using Microsoft.Extensions.Options;
using OngakuVault.Helpers;
using OngakuVault.Models;
using SubtitlesParserV2;
using SubtitlesParserV2.Models;
using System.Diagnostics;
using System.Text;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace OngakuVault.Services
{
	public interface IMediaDownloaderService
	{
		/// <summary>
		/// This method download a audio in the best possible quality before converting it to the choose format is selected.
		/// </summary>
		/// <param name="mediaUrl">The url of the page with the audio to download</param>
		/// <param name="audioConversionFormat">If changed from best, will convert the best audio to the selected format</param>
		/// <param name="cancellationToken">Token for cancellation</param>
		/// <param name="progressReport">A IProgress to get updates on the download progress</param>
		/// <returns>The <see cref="FileInfo"/> of the downloaded audio, or null if the audio file was not created.</returns>
		/// <exception cref="ScraperErrorOutputHelper.ProcessedScraperErrorOutputException">Related to the error output of the scraper (yt-dlp)</exception>
		public Task<FileInfo?> DownloadAudio(string mediaUrl, AudioConversionFormat audioConversionFormat = AudioConversionFormat.Best, CancellationToken? cancellationToken = null, IProgress<DownloadProgress>? progressReport = null);

		/// <summary>
		/// Get informations about a media
		/// </summary>
		/// <param name="url">The url of the media</param>
		/// <param name="flatPlaylist">If set to true, does not extract information for others video in a playlist</param>
		/// <param name="fetchComments">If set to true, fetch comments on a media</param>
		/// <param name="cancellationToken">If defined, allow to cancel the task</param>
		/// <returns>Return a <see cref="MediaInfoAdvancedModel"/>. Contains advanced info about the media</returns>
		/// <exception cref="NotSupportedException">Related to the data returned by yt-dlp about the current media</exception>
		/// <exception cref="ScraperErrorOutputHelper.ProcessedScraperErrorOutputException">Related to the error output of the scraper (yt-dlp)</exception>
		public Task<MediaInfoAdvancedModel> GetMediaInformations(string url, bool flatPlaylist = true, bool fetchComments = false, CancellationToken? cancellationToken = null);
	}

	/// <summary>
	/// This class implements the <see cref="IMediaDownloaderService"/> interface and provides functionality
	/// to interact with the yt-dlp scrapper.
	/// </summary>
	public class MediaDownloaderService : IMediaDownloaderService, IDisposable
	{
		private bool _isDisposed = false;
		private readonly ILogger<MediaDownloaderService> _logger;
		private readonly AppSettingsModel _appSettings;
		/// <summary>
		/// The scrapper, YoutubeDLSharp (yt-dlp wrapper)
		/// </summary>
		private readonly YoutubeDL MediaDownloader;
		/// <summary>
		/// The directory in which the OngakuVault executable is located
		/// </summary>
		private readonly string ExecutableDirectory = AppContext.BaseDirectory;

		/// <summary>
		/// The path of the directory where yt-dlp files will first be saved.
		/// NOTE: This directory is deleted at the closure of the app.
		/// </summary>
		private readonly string TMPOutputPath;

		/// <summary>
		/// Hard-coded settings that are used for every audio download request
		/// </summary>
		private readonly OptionSet DownloaderOverwriteOptions = new OptionSet()
		{
			// Keep the best file quality possible
			AudioQuality = 0,
			// Prefer bestaudio, fallback to best (may be a video)
			Format = "bestaudio/best",
			// If media is a video, convert it to a audio only
			ExtractAudio = true,
			// Embed the cover / thumbnail as cover art on the file if existing
			EmbedThumbnail = true,
			// Preserve file metadata
			EmbedMetadata = true,
			// Do not embed the infojson and subtitles in the metadata
			NoEmbedInfoJson = true,
			NoEmbedSubs = true,
			// Ensure to download only the current media if the url is media + playlist of media
			NoPlaylist = true,
			// Do not load informations about other media if url contains a playlist
			FlatPlaylist = true,
		};

		/// <summary>
		/// Hard-coded settings that are used for every fetching of information by the scraper
		/// </summary>
		private readonly OptionSet InformationOverwriteOptions = new OptionSet() { };

		/// <summary>
		/// List of lossless codecs
		/// </summary>
		private readonly string[] LosslessCodecs = new string[]
		{
			// FLAC - Free Lossless Audio Codec
			"flac",
			// ALAC - Apple Lossless Audio Codec
			"alac",
			// WAV pcm_s16le (16-bit little-endian PCM) and pcm_s24le (24-bit little-endian PCM)
			"pcm_s16le",
			"pcm_s24le",
			// AIFF pcm_s16be (16-bit big-endian PCM) and pcm_s24be (24-bit big-endian PCM)
			"pcm_s16be",
			"pcm_s24be",
			// Wavpack
			"wavpack",
			// TTA - True Audio
			"tta",
			// MPEG-4 ALS - MPEG-4 Audio Lossless Coding
			"als",
		};

		public MediaDownloaderService(ILogger<MediaDownloaderService> logger, IOptions<AppSettingsModel> appSettings)
		{
			// Init readonly fields
			_logger = logger;
			_appSettings = appSettings.Value;
			TMPOutputPath = _appSettings.TMP_OUTPUT_DIRECTORY ?? Directory.CreateTempSubdirectory("ongakuvault_downloads_").FullName;
			_appSettings.TMP_OUTPUT_DIRECTORY = TMPOutputPath; // Overwrite setting value so that if value was null, it is now the new TempSub 
			// Ensure the given TMP output path exist
			Directory.CreateDirectory(TMPOutputPath);
			// Create a instance of the yt-dlp wrapper that can run up to "PARALLEL_SCRAPPER_PROC" value of processes simultaneously
			MediaDownloader = new YoutubeDL((byte)_appSettings.PARALLEL_SCRAPER_PROC) 
			{
				// Ensure each download have a unique name by adding the epoch time
				OutputFileTemplate = "%(title)s [%(id)s]_%(epoch)s.%(ext)s",
				// Set the paths for yt-dlp and FFmpeg executables for linux by default
				YoutubeDLPath = Path.Combine(ExecutableDirectory, "yt-dlp"),
				FFmpegPath = Path.Combine(ExecutableDirectory, "ffmpeg"),
				// Set the download path
				OutputFolder = TMPOutputPath,
				// Restrict files name to ASCII
				//RestrictFilenames = true,
			};

			// If OS is Windows, append ".exe" to the executables name
			if (OperatingSystem.IsWindows())
			{
				MediaDownloader.YoutubeDLPath += ".exe";
				MediaDownloader.FFmpegPath += ".exe";
				// Update scraper download settings
				DownloaderOverwriteOptions.WindowsFilenames = true;
			}
			_logger.LogInformation("Current temporary download output path is set to : {TMPOutputPath}", TMPOutputPath);

			// Ensure that yt-dlp and ffmpeg binary are existing when initialising
			if (File.Exists(MediaDownloader.YoutubeDLPath) && File.Exists(MediaDownloader.FFmpegPath))
			{
				_logger.LogInformation("MediaDownloaderService configured yt-dlp wrapper external binaries. yt-dlp to '{YoutubeDLPath}' and FFmpeg to '{FFmpegPath}'.", MediaDownloader.YoutubeDLPath, MediaDownloader.FFmpegPath);
				/* MediaDownloader.Version seems to only work on Windows due to the way it get executable version (FileVersionInfo.GetVersionInfo)
				 * work around is starting our own process with --version argument and getting the output.
				*/
				// Create a temporary yt-dlp proc
				YoutubeDLProcess temporaryYTDLPProc = new YoutubeDLProcess(MediaDownloader.YoutubeDLPath);
				
				// Create a event handler to listen to the proc outputs
				EventHandler<DataReceivedEventArgs>? outputHandler = null;
				outputHandler = (o, e) =>
				{
					// e.Data is a proc output, when running --version arg, the first and only output should be in yyyy.MM.dd format.
					bool isDateValid = DateTime.TryParseExact(e.Data, "yyyy.MM.dd", null, System.Globalization.DateTimeStyles.None, out _);
					if (isDateValid)
					{
						_logger.LogInformation("Current yt-dlp version is : {date}", e.Data);
					}
					else _logger.LogWarning("Could not detect yt-dlp version. Output is : {output}", e.Data);
					// Detach the event handler to allow the GC to free memory
					temporaryYTDLPProc.OutputReceived -= outputHandler;
				};
				// Attach the event handler to the proc
				temporaryYTDLPProc.OutputReceived += outputHandler;
				// Run the proc with the --version argument in another thread to prevent delaying the initialisation of MediaDownloaderService.
				_ = temporaryYTDLPProc.RunAsync(null, new OptionSet { Version = true, Simulate = true });
			}
			else
			{
				_logger.LogWarning("MediaDownloaderService could not locate some external binaries. yt-dlp should be in '{YoutubeDLPath}' and FFmpeg in '{FFmpegPath}'.\r\nEnsure both binaries are under their respective paths.", MediaDownloader.YoutubeDLPath, MediaDownloader.FFmpegPath);
			}


			// Overwrite the yt-dlp configuration depending on the defined appSettings
			if (!string.IsNullOrEmpty(_appSettings.SCRAPER_USERAGENT)) // Apply custom user-agent
			{
				DownloaderOverwriteOptions.AddHeaders = new string[] { $"User-Agent:{_appSettings.SCRAPER_USERAGENT}" };
				InformationOverwriteOptions.AddHeaders = new string[] { $"User-Agent:{_appSettings.SCRAPER_USERAGENT}" };
			}

			string[]? scraperPluginDirs = _appSettings.SCRAPER_PLUGIN_DIRS_ARRAY;
			if (scraperPluginDirs != null)
			{
				DownloaderOverwriteOptions.PluginDirs = scraperPluginDirs;
				InformationOverwriteOptions.PluginDirs = scraperPluginDirs;
			}

			string[]? informationCustomOptions = _appSettings.SCRAPER_INFORMATION_CUSTOM_OPTIONS_ARRAY;
			if (informationCustomOptions != null) ApplySettingsCustomOptionToScraper(informationCustomOptions, InformationOverwriteOptions);
			string[]? downloadCustomOptions = _appSettings.SCRAPER_DOWNLOAD_CUSTOM_OPTIONS_ARRAY;
			if (downloadCustomOptions != null) ApplySettingsCustomOptionToScraper(downloadCustomOptions, DownloaderOverwriteOptions);
		}

		/// <summary>
		/// This method will apply the custom options from <see cref="AppSettingsModel"/> to the given scraper OptionSet.
		/// Allowing user to add custom arguments to the scraper.
		/// </summary>
		/// <param name="customOptions">A array of custom options that follows <see cref="AppSettingsModel.SCRAPER_DOWNLOAD_CUSTOM_OPTIONS"/> format.</param>
		/// <param name="configToOverwrite">The hard-coded scraper settings to overwrite with the additionals arguments</param>
		private void ApplySettingsCustomOptionToScraper(string[] customOptions, OptionSet configToOverwrite)
		{
			foreach (string currCustomOption in customOptions)
			{
				string[] currParameters = currCustomOption.Split(';');
				if (currParameters.Length != 3)
				{
					_logger.LogWarning("The custom option '{currCustomOption}' has {paramsLength} parameters but need 3. Ignored custom option.", currCustomOption, currParameters.Length);
					return; // Next custom option
				}

				// Follow the format described in AppSettingsModel, Argument name (including the two "--") ; the type of the value ; the value
				string customOptionKey = currParameters[0];
				string customOptionValueType = currParameters[1];
				string customOptionValue = currParameters[2];

				if (customOptionValueType.Equals("string", StringComparison.CurrentCultureIgnoreCase))
				{
					configToOverwrite.AddCustomOption(customOptionKey, customOptionValue);
				}
				else if (customOptionValueType.Equals("int", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!int.TryParse(customOptionValue, out int customOptionValueInt))
					{
						_logger.LogWarning("Could not parse '{customOptionValue}' as a value type '{customOptionValueType}' in custom option '{currCustomOption}'. Ignored custom option.", customOptionValue, customOptionValueType, currCustomOption);
					}
					configToOverwrite.AddCustomOption(customOptionKey, customOptionValueInt);
				}
				else if (customOptionValueType.Equals("boolean", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!bool.TryParse(customOptionValue, out bool customOptionValueBool))
					{
						_logger.LogWarning("Could not parse '{customOptionValue}' as a value type '{customOptionValueType}' in custom option '{currCustomOption}'. Ignored custom option.", customOptionValue, customOptionValueType, currCustomOption);
					}
					configToOverwrite.AddCustomOption(customOptionKey, customOptionValueBool);
				}
				else _logger.LogWarning("The value type '{customOptionValueType}' in custom option '{currCustomOption}' is not supported. Ignored custom option.", customOptionValueType, currCustomOption);
			}
		}

		/// <summary>
		/// Called when Disposing of MediaDownaloderService (should only happen at application closure)
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed) return; // Prevent redundant calls
			_logger.LogInformation("Removing temporary download directory before process closure...");
			if (Directory.Exists(TMPOutputPath))
			{
				Directory.Delete(TMPOutputPath, true);
			}
			GC.SuppressFinalize(this);
			_isDisposed = true;
		}


		public async Task<FileInfo?> DownloadAudio(string mediaUrl, AudioConversionFormat audioConversionFormat = AudioConversionFormat.Best, CancellationToken? cancellationToken = null, IProgress<DownloadProgress>? progressReport = null)
		{
			// Sanitize the url to prevent possible command injections on windows & linux
			string sanitizedMediaUrl = UrlHelper.SanitizeUrl(mediaUrl) ?? throw new FormatException($"Tried to sanitize the given mediaUrl '{mediaUrl}' to a safe URL Encoded url but failed.");

			// If no cancellation token was given, generate a "None" token
			cancellationToken = cancellationToken ?? CancellationToken.None;
			// Download the media audio
			RunResult<string> audioDownloadResult = await MediaDownloader.RunAudioDownload(sanitizedMediaUrl, audioConversionFormat, cancellationToken.Value, progressReport, default, DownloaderOverwriteOptions);
			// If succes is false, throw a ScraperErrorOutputException using ProcessScraperErrorOutput
			if (!audioDownloadResult.Success) ScraperErrorOutputHelper.ProcessScraperErrorOutput(audioDownloadResult.ErrorOutput, _appSettings.CLIENT_SAFE_ERROR);
			// Ensure file exists, else return null
			// Some website/url can cause a success result, but without information / formats available, thus, no file are created.
			if (File.Exists(audioDownloadResult.Data)) return new FileInfo(audioDownloadResult.Data);
			return null;
		}


		public async Task<MediaInfoAdvancedModel> GetMediaInformations(string url, bool flatPlaylist = true, bool fetchComments = false, CancellationToken? cancellationToken = null)
		{
			// Sanitize the url to prevent possible command injections on windows & linux
			string sanitizedUrl = UrlHelper.SanitizeUrl(url) ?? throw new FormatException($"Tried to sanitize the given url '{url}' to a safe URL Encoded url but failed.");
			
			// If no cancellation token was given, generate a "None" token
			cancellationToken = cancellationToken ?? CancellationToken.None;
			// Fetch media information
			// NOTE: flatplaylist is enabled so that if a given url contains a specific media ALONG with a playlist, the playlist part get ignored by the scraper
			RunResult<VideoData> mediaData = await MediaDownloader.RunVideoDataFetch(sanitizedUrl, cancellationToken.Value, flatPlaylist, fetchComments, InformationOverwriteOptions);
			if (!mediaData.Success)
			{
				// Failed to fetch / get media info from a webpage
				// Throw a error message related to the scraper issue
				ScraperErrorOutputHelper.ProcessScraperErrorOutput(mediaData.ErrorOutput, _appSettings.CLIENT_SAFE_ERROR);
			}

			// Ensure the scraper results isn't a playlist
			if (mediaData.Data.ResultType == MetadataType.Playlist) throw new NotSupportedException("The scraper returned a playlist (list) of different media. You cannot get data about a specific media by giving a playlist url.");
			
			MediaInfoAdvancedModel returnedMediaInformations = new MediaInfoAdvancedModel()
			{
				Name = string.IsNullOrEmpty(mediaData.Data.Track) ? mediaData.Data.Title : mediaData.Data.Track, // Fallback to Title
				ArtistName = string.IsNullOrEmpty(mediaData.Data.Artist) ? mediaData.Data.Uploader : mediaData.Data.Artist, // Fallback to Uploader name
				AlbumName = mediaData.Data.Album,
				MediaUrl = mediaData.Data.WebpageUrl,
				ReleaseYear = int.TryParse(mediaData.Data.ReleaseYear, out int releaseYearParsed) ? releaseYearParsed : mediaData.Data.UploadDate?.Year, // Fallback to UploadDate year
				Genre = mediaData.Data.Genre,
				TrackNumber = mediaData.Data.TrackNumber,
				Description = mediaData.Data.Description,
			};

			// Ensure formats where found for the current media
			// NOTE: While formats in the library are set as not nullable, it can be null when no formats are found
			if (mediaData.Data?.Formats?.Length >= 1)
			{
				// Get the best format that has a audio codec by ordering bitrate and sampling rate
				FormatData? bestFormatData = mediaData.Data.Formats.Where(item => item.AudioCodec != null)
					.OrderByDescending(item => item.Bitrate ?? 0)
					.ThenByDescending(item => item.AudioSamplingRate ?? 0)
					.FirstOrDefault();

				if (bestFormatData != null)
				{
					// Loop trought format to verify if one of them is using a lossless encoding
					foreach (FormatData item in mediaData.Data.Formats)
					{
						// Ensure the format has a audio codec
						if (item.AudioCodec != null)
						{
							// Verify if a codec in our list matches the item codec
							bool isAudioLossless = LosslessCodecs.Any(codec => item.AudioCodec.Contains(codec));
							if (isAudioLossless)
							{
								// If some values from the item are missing, we force the best value possible (normally impossible values)
								// Since a lossless file was found on the webpage, we assume it is better quality as somes webpage don't
								// return lossless files bitrate / sampling.
								double itemBitrate = item?.Bitrate ?? double.MaxValue;
								double itemSamplingRate = item?.AudioSamplingRate ?? double.MaxValue;

								// Ensure the lossless audio is better or the same quality as the best format found.
								// This prevent recommending lossless when the best audio quality is in a lossy audio.
								if (itemBitrate >= (bestFormatData?.AudioBitrate ?? 0.0) && itemSamplingRate >= (bestFormatData?.AudioSamplingRate ?? 0.0))
								{
									returnedMediaInformations.IsLosslessRecommended = true;
									break;
								}

							}
						}
					}
				}
			}
			else throw new NotSupportedException("Scraper did not find any video or audio formats available on the webpage.");

			// Handle lyrics / subtitle fetching
			string[]? lyricsLanguagePriority = _appSettings.LYRICS_LANGUAGE_PRIORITY_ARRAY;
			if (lyricsLanguagePriority?.Length >= 1) // Ensure the setting is configured, else it's disabled
			{
				List<MediaLyric>? mediaLyrics = await GetLyricsAsync(mediaData, lyricsLanguagePriority);
				if (mediaLyrics != null) returnedMediaInformations.Lyrics = mediaLyrics;
			}

			return returnedMediaInformations;
		}


		/// <summary>
		/// Search for lyrics inside the results returned by the scrapper and try to parse them according to a language
		/// priority.
		/// </summary>
		/// <param name="mediaData">The data (results) returned by the scrapper</param>
		/// <param name="languagePriority">The language priority in IETF language tag</param>
		/// <returns>A list of <see cref="MediaLyric"/> or null</returns>
		/// <exception cref="ArgumentException"></exception>
		private async Task<List<MediaLyric>?> GetLyricsAsync(RunResult<VideoData> mediaData, string[] languagePriority)
		{
			if (languagePriority.Length <= 0) throw new ArgumentException("Language priority need to have >= 1 element but has <= 0");
			List<MediaLyric> lyrics = new List<MediaLyric>();

			// Loop every subtitles found, where Key is the IETF language tag and value a array of all
			// available subtitle formats for that language.
			foreach (KeyValuePair<string, SubtitleData[]> currSubtitle in mediaData.Data.Subtitles)
			{
				if (lyrics.Count >= 1) break;
				// Verify if one of the given language tag is similar to the current subtitle language tag
				if (languagePriority.Any(languageTag => LanguageTagHelper.IsIETFLanguageTagSimilar(languageTag, currSubtitle.Key)))
				{
					// Loop trought all file format available for the current language
					foreach (SubtitleData subtitleData in currSubtitle.Value)
					{
						// Verify if the file format is supported by our parser
						SubtitleFormatType? subtitleFormatType = SubtitleFormat.GetFormatTypeByFileExtensionName(subtitleData.Ext);
						if (subtitleFormatType.HasValue)
						{
							try
							{
								using HttpResponseMessage requestResponse = await WebRequestHelper.GetContentFromWebsiteAsync(new Uri(subtitleData.Url), _appSettings.WEB_REQUEST_USERAGENT);
								using Stream responseStream = await requestResponse.Content.ReadAsStreamAsync();
								SubtitleParserResultModel? result = SubtitleParser.ParseStream(responseStream, Encoding.UTF8, subtitleFormatType.Value, false);
								if (result != null)
								{
									// Loop trought all of the parsed subtitles/lyrics
									foreach (SubtitleModel subtitleContent in result.Subtitles)
									{
										lyrics.Add(new MediaLyric()
										{
											Content = string.Join(' ', subtitleContent.Lines),
											Time = subtitleContent.StartTime >= 0 ? subtitleContent.StartTime : null
										});
									}
									break; // We found the lyrics, stop execution
								}
								else _logger.LogWarning("Fetched lyrics from the source '{url}' as extension '{extensionFormat}' but failed to parsing using '{parser}' parser.",subtitleData.Url , subtitleData.Ext, Enum.GetName(typeof(SubtitleFormatType), subtitleFormatType));
							}
							catch (HttpRequestException requestException)
							{
								_logger.LogWarning(requestException, "Failed to fetch lyrics on '{url}. Got HTTP response '{httpCode}. Exception: {ex}", subtitleData.Url, requestException.StatusCode, requestException.Message);
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "Unexpected error happened while fetching lyrics. Exception: {ex}", ex.Message);
							}
						}
					}
				}
			}

			// Return results
			if (lyrics.Count <= 0) return null;
			return lyrics;
		}

	}
}

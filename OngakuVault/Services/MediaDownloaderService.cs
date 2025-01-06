using Microsoft.Extensions.Options;
using OngakuVault.Helpers;
using OngakuVault.Models;
using System.Diagnostics;
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
		/// <exception cref="ScraperErrorOutputException">Related to the error output of the scraper (yt-dlp)</exception>
		public Task<FileInfo?> DownloadAudio(string mediaUrl, AudioConversionFormat audioConversionFormat = AudioConversionFormat.Best,  CancellationToken? cancellationToken = null, IProgress<DownloadProgress>? progressReport = null);

		/// <summary>
		/// Get informations about a media
		/// </summary>
		/// <param name="url">The url of the media</param>
		/// <param name="flatPlaylist">If set to true, does not extract information for others video in a playlist</param>
		/// <param name="fetchComments">If set to true, fetch comments on a media</param>
		/// <returns>Return a <see cref="MediaInfoAdvancedModel"/>. Contains advanced info about the media</returns>
		/// <exception cref="NotSupportedException">Related to the data returned by yt-dlp about the current media</exception>
		/// <exception cref="ScraperErrorOutputException">Related to the error output of the scraper (yt-dlp)</exception>
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
		/// Hard-coded settings that are used for every Audio download request
		/// </summary>
		private readonly OptionSet AudioDownloaderOverrideOptions = new OptionSet()
		{
			// Keep the best file quality possible
			AudioQuality = 0,
			// Prefer bestaudio, fallback to best (may be video, but must include audio)
			Format = "bestaudio/best[acodec!=none]",
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
		};

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
			TMPOutputPath = _appSettings.TMP_OUTPUT_DIRECTORY  ?? Directory.CreateTempSubdirectory("ongakuvault_downloads_").FullName;
			_appSettings.TMP_OUTPUT_DIRECTORY = TMPOutputPath; // Overwrite setting value so that if value was null, it is now the new TempSub 
			// Create a instance of the yt-dlp wrapper that can run up to "PARALLEL_SCRAPPER_PROC" value of processes simultaneously
			MediaDownloader = new YoutubeDL((byte)_appSettings.PARALLEL_SCRAPPER_PROC);
			// Set the paths for yt-dlp and FFmpeg executables for linux by default
			MediaDownloader.YoutubeDLPath = Path.Combine(ExecutableDirectory, "yt-dlp");
			MediaDownloader.FFmpegPath = Path.Combine(ExecutableDirectory, "ffmpeg");

			// If OS is Windows, append ".exe" to the executables name
			if (OperatingSystem.IsWindows())
			{
				MediaDownloader.YoutubeDLPath += ".exe";
				MediaDownloader.FFmpegPath += ".exe";
			}
			// Set the download path
			Directory.CreateDirectory(TMPOutputPath); // Ensure the given output path exist
			MediaDownloader.OutputFolder = TMPOutputPath;
			_logger.LogInformation("Current temporary download output path is set to : {TMPOutputPath}", TMPOutputPath);
			MediaDownloader.RestrictFilenames = true; // Only allow ASCII

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
				EventHandler<DataReceivedEventArgs> outputHandler = (o, e) =>
				{
					// e.Data is a proc output, when running --version arg, the first and only output should be in yyyy.MM.dd format.
					bool isDateValid = DateTime.TryParseExact(e.Data, "yyyy.MM.dd", null, System.Globalization.DateTimeStyles.None, out _);
					if (isDateValid) 
					{
						_logger.LogInformation("Current yt-dlp version is : {date}", e.Data);
					} else _logger.LogWarning("Could not detect yt-dlp version. Output is : {output}", e.Data);
				};
				// Attach the event handler to the proc
				temporaryYTDLPProc.OutputReceived += outputHandler;
				// Run the proc with the --version argument and wait for proc exit
				temporaryYTDLPProc.RunAsync(null, new OptionSet { Version = true, Simulate = true }).Wait();
				// Detach the event handler to allow the GC to free memory
				temporaryYTDLPProc.OutputReceived -= outputHandler;
			}
			else 
			{
				_logger.LogWarning("MediaDownloaderService could not locate some external binaries. yt-dlp should be in '{YoutubeDLPath}' and FFmpeg in '{FFmpegPath}'.\r\nEnsure both binaries are under their respective paths.", MediaDownloader.YoutubeDLPath, MediaDownloader.FFmpegPath);
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
			// If no cancellation token was given, generate a "None" token
			cancellationToken = cancellationToken ?? CancellationToken.None;
			// Download the media audio
			RunResult<string> audioDownloadResult = await MediaDownloader.RunAudioDownload(mediaUrl, audioConversionFormat, cancellationToken.Value, progressReport, default, AudioDownloaderOverrideOptions);
			// If succes is false, throw a ScraperErrorOutputException using ProcessScraperErrorOutput
			if (!audioDownloadResult.Success) ScraperErrorOutputHelper.ProcessScraperErrorOutput(audioDownloadResult.ErrorOutput);
			// Ensure file exists, else return null
			// Some website/url can cause a success result, but without information / formats available, thus, no file are created.
			if (File.Exists(audioDownloadResult.Data)) return new FileInfo(audioDownloadResult.Data);
			return null;
		}


		public async Task<MediaInfoAdvancedModel> GetMediaInformations(string url, bool flatPlaylist = true, bool fetchComments = false, CancellationToken? cancellationToken = null)
		{
			// If no cancellation token was given, generate a "None" token
			cancellationToken = cancellationToken ?? CancellationToken.None;
			// Fetch media information
			RunResult<VideoData> mediaData = await MediaDownloader.RunVideoDataFetch(url, cancellationToken.Value, flatPlaylist, fetchComments);
			if (!mediaData.Success)
			{
				// Failed to fetch / get media info from a webpage
				// Throw a error message related to the scraper issue
				ScraperErrorOutputHelper.ProcessScraperErrorOutput(mediaData.ErrorOutput);
			}

			if (mediaData.Data.ResultType == MetadataType.Playlist) throw new NotSupportedException("Playlist media are not currently supported.");
			MediaInfoAdvancedModel mediaInformations = new MediaInfoAdvancedModel()
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
			// NOTE: While formats was set as not nullable, it can be null when no formats are found
			if (mediaData.Data?.Formats?.Length >= 1)
			{
				// Get the best format that has a audio codec by ordering bitrate and sampling rate
				FormatData? bestFormatData = mediaData.Data.Formats.Where(item => item.AudioCodec != null)
					.OrderByDescending(item => item.Bitrate ?? 0)
					.ThenByDescending(item => item.AudioSamplingRate ?? 0)
					.FirstOrDefault();

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
							if (bestFormatData != null)
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
									mediaInformations.IsLosslessRecommended = true;
									break;
								}
							}
						}
					}
				}
			}
			else throw new NotSupportedException("Scraper did not find any video or audio formats available on the webpage.");

			return mediaInformations;
		}
	}
}

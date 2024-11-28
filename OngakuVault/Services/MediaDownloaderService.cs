using OngakuVault.Models;
using System.Runtime.InteropServices;
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
		/// <returns>The <see cref="FileInfo"/> of the downloaded audio.</returns>
		public Task<FileInfo> DownloadAudio(string mediaUrl, AudioConversionFormat audioConversionFormat = AudioConversionFormat.Best,  CancellationToken? cancellationToken = null);

		/// <summary>
		/// Get informations about a media
		/// </summary>
		/// <param name="url">The url of the media</param>
		/// <param name="flatPlaylist">If set to true, does not extract information for others video in a playlist</param>
		/// <param name="fetchComments">If set to true, fetch comments on a media</param>
		/// <returns><see cref="MediaInfoModel"/> if success, else null</returns>
		public Task<MediaInfoModel?> GetMediaInformations(string url, bool flatPlaylist = true, bool fetchComments = false);
	}
	public class MediaDownloaderService : IMediaDownloaderService
	{
		private readonly ILogger<MediaDownloaderService> _logger;
		/// <summary>
		/// YoutubeDownloadSharp (yt-dlp wrapper). 8 parallel yt-dlp process allowed to run
		/// </summary>
		private readonly YoutubeDL MediaDownloader = new YoutubeDL(8);
		/// <summary>
		/// The directory in which the OngakuVault executable is located
		/// </summary>
		private readonly string ExecutableDirectory = AppContext.BaseDirectory;

		/// <summary>
		/// Hard-coded settings that are used for every Audio download request
		/// </summary>
		private readonly OptionSet AudioDownloaderOverrideOptions = new OptionSet()
		{
			// Keep the best file quality possible
			AudioQuality = 0,
			// Prefer best found audio, fallback to best video if no audio found
			Format = "bestaudio/best",
			// If media is a video, convert it to a audio only
			ExtractAudio = true,
		};

		public MediaDownloaderService(ILogger<MediaDownloaderService> logger)
        {
			_logger = logger;

			// Set the paths for yt-dlp and FFmpeg executables for linux by default
			MediaDownloader.YoutubeDLPath = Path.Combine(ExecutableDirectory, "yt-dlp");
			MediaDownloader.FFmpegPath = Path.Combine(ExecutableDirectory, "ffmpeg");

			// If OS is Windows, append ".exe" to the executables
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				MediaDownloader.YoutubeDLPath += ".exe";
				MediaDownloader.FFmpegPath += ".exe";
			}

			// Set the download path
			MediaDownloader.OutputFolder = Path.Combine(ExecutableDirectory, "tmp_downloads");
			Directory.CreateDirectory(MediaDownloader.OutputFolder); // Ensure the tmp download directory exist
			MediaDownloader.RestrictFilenames = true; // Only allow ASCII
			_logger.LogInformation("MediaDownloaderService configured yt-dlp wrapper external binaries. yt-dlp to '{YoutubeDLPath}' and FFmpeg to '{FFmpegPath}'.", MediaDownloader.YoutubeDLPath, MediaDownloader.FFmpegPath);
			_logger.LogInformation("Current yt-dlp version is {version}.", MediaDownloader.Version);
		}

		public async Task<FileInfo> DownloadAudio(string mediaUrl, AudioConversionFormat audioConversionFormat = AudioConversionFormat.Best, CancellationToken? cancellationToken = null)
		{
			// If no cancellation token was given, generate a "None" token
			cancellationToken = cancellationToken ?? CancellationToken.None;
			// Download the media audio
			RunResult<string> audioDownloadResult = await MediaDownloader.RunAudioDownload(mediaUrl, audioConversionFormat, cancellationToken.Value, default, default, AudioDownloaderOverrideOptions);
			// If succes is false, throw exception with all error output
			audioDownloadResult.EnsureSuccess();
			return new FileInfo(audioDownloadResult.Data);
		}

		/// <summary>
		/// Get informations about a media
		/// </summary>
		/// <param name="url">The url of the media</param>
		/// <param name="flatPlaylist">If set to true, does not extract information for others video in a playlist</param>
		/// <param name="fetchComments">If set to true, fetch comments on a media</param>
		/// <returns><see cref="MediaInfoModel"/> if success, else null</returns>
		public async Task<MediaInfoModel?> GetMediaInformations(string url, bool flatPlaylist = true, bool fetchComments = false)
		{
			// Fetch media information
			RunResult<VideoData> mediaData = await MediaDownloader.RunVideoDataFetch(url, default, flatPlaylist, fetchComments);
			if (mediaData.Success)
			{
				MediaInfoModel mediaInformations = new MediaInfoModel()
				{
					Name = string.IsNullOrEmpty(mediaData.Data.Track) ? mediaData.Data.Title : mediaData.Data.Track, // Fallback to Title
					ArtistName = string.IsNullOrEmpty(mediaData.Data.Artist) ? mediaData.Data.Uploader : mediaData.Data.Artist, // Fallback to Uploader name
					AlbumName = mediaData.Data.Album,
					MediaUrl = mediaData.Data.WebpageUrl,
					ReleaseYear = mediaData.Data.ReleaseYear ?? mediaData.Data.UploadDate?.Year.ToString(), // Fallback to UploadDate year
					Genre = mediaData.Data.Genre,
					TrackNumber = mediaData.Data.TrackNumber,
				};

				// Loop trought format to verify if one of them is using a lossless encoding
				foreach (FormatData item in mediaData.Data.Formats)
                {
					if (item.AudioCodec != null)
					{
						// FLAC - Free Lossless Audio Codec
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("flac") ? true : mediaInformations.IsLosslessAvailable;
						// ALAC - Apple Lossless Audio Codec
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("alac") ? true : mediaInformations.IsLosslessAvailable;
						// WAV pcm_s16le (16-bit little-endian PCM) and pcm_s24le (24-bit little-endian PCM)
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("pcm_s16le") ? true : mediaInformations.IsLosslessAvailable;
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("pcm_s24le") ? true : mediaInformations.IsLosslessAvailable;
						// AIFF pcm_s16be (16-bit big-endian PCM) and pcm_s24be (24-bit big-endian PCM)
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("pcm_s16be") ? true : mediaInformations.IsLosslessAvailable;
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("pcm_s24be") ? true : mediaInformations.IsLosslessAvailable;
						// Wavpack
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("wavpack") ? true : mediaInformations.IsLosslessAvailable;
						// TTA - True Audio
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("tta") ? true : mediaInformations.IsLosslessAvailable;
						// MPEG-4 ALS - MPEG-4 Audio Lossless Coding
						mediaInformations.IsLosslessAvailable = item.AudioCodec.Contains("als") ? true : mediaInformations.IsLosslessAvailable;
						if (mediaInformations.IsLosslessAvailable) break;
					}
				}
				return mediaInformations;
			}
			// Failed to fetch
			_logger.LogWarning("MediaDownaloderService failed to fetch data about mediaUrl : {url}. Errors : {ErrorOutput}", url , mediaData.ErrorOutput.Append(Environment.NewLine));
			return null;
		}
	}
}

using OngakuVault.Models;
using System.Runtime.InteropServices;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace OngakuVault.Services
{
	public interface IMediaDownloaderService 
	{
		public Task DownloadAudio(MediaInfoModel mediaInfo, CancellationToken? cancellationToken = null);

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
			_logger.LogInformation("MediaDownloaderService configured yt-dlp wrapper external binaries. yt-dlp to '{YoutubeDLPath}' and FFmpeg to '{FFmpegPath}'.", MediaDownloader.YoutubeDLPath, MediaDownloader.FFmpegPath);
			_logger.LogInformation("Current yt-dlp version is {version}.", MediaDownloader.Version);
		}
        public async Task DownloadAudio(MediaInfoModel mediaInfo, CancellationToken? cancellationToken = null) 
		{
			// If no cancellation token was given, generate a "None" token
			cancellationToken = cancellationToken ?? CancellationToken.None;
			for (int i = 0; i < 20; i++)
			{
				if (cancellationToken.Value.IsCancellationRequested) break;
				Console.WriteLine("Inside semaphore " + i.ToString());
				await Task.Delay(4000);
			}
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
				return new MediaInfoModel()
				{
					Name = string.IsNullOrEmpty(mediaData.Data.Track) ? mediaData.Data.Title : mediaData.Data.Track, // Fallback to Title
					ArtistName = string.IsNullOrEmpty(mediaData.Data.Artist) ? mediaData.Data.Uploader : mediaData.Data.Artist, // Fallback to Uploader name
					AlbumName = mediaData.Data.Album,
					MediaUrl = mediaData.Data.WebpageUrl,
					ReleaseYear = mediaData.Data.ReleaseYear ?? mediaData.Data.UploadDate?.Year.ToString(), // Fallback to UploadDate year
					Genre = mediaData.Data.Genre,
					TrackNumber = mediaData.Data.TrackNumber,
				};
			}
			// Failed to fetch
			_logger.LogWarning("MediaDownaloderService failed to fetch data about mediaUrl : {url}. Errors : {ErrorOutput}", url , mediaData.ErrorOutput.Append(Environment.NewLine));
			return null;
		}
	}
}

using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Xml.Linq;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace OngakuVault.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MediaController : ControllerBase
	{
		readonly ILogger<MediaController> _logger;
		/// <summary>
		/// YoutubeDownloadSharp (yt-dlp wrapper)
		/// </summary>
		private readonly YoutubeDL _mediaDownloader;
		public MediaController(ILogger<MediaController> logger, YoutubeDL mediaDownloader)
        {
            _logger = logger;
			_mediaDownloader = mediaDownloader;
        }

		[HttpGet("info")]
		[EndpointDescription("Get basic informations about a media from a url")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MediaInfoModel))]
		[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		public async Task<ActionResult> GetMediaInfo([Url]string mediaUrl)
		{
			_ = Uri.TryCreate(mediaUrl, UriKind.Absolute, out Uri? mediaUrlUri);
			if (mediaUrlUri?.Scheme != Uri.UriSchemeHttp && mediaUrlUri?.Scheme != Uri.UriSchemeHttps && mediaUrlUri != null)
			{
				return BadRequest("mediaUrl scheme can only be http or https.");
			}
			// Fetch video data (with flat playlist set to true)
			RunResult<VideoData> mediaData = await _mediaDownloader.RunVideoDataFetch(mediaUrl, default, true, false);
			if (mediaData.Success) 
			{
				MediaInfoModel mediaInfoModel = new MediaInfoModel() 
				{
					Name = string.IsNullOrEmpty(mediaData.Data.Track) ? mediaData.Data.Title : mediaData.Data.Track, // Fallback to Title
					ArtistName = string.IsNullOrEmpty(mediaData.Data.Artist) ? mediaData.Data.Uploader : mediaData.Data.Artist, // Fallback to Uploader name
					AlbumName = mediaData.Data.Album,
					MediaUrl = mediaData.Data.WebpageUrl,
					ReleaseYear = mediaData.Data.ReleaseYear ?? mediaData.Data.UploadDate?.Year.ToString(), // Fallback to UploadDate year
					Genre = mediaData.Data.Genre,
					TrackNumber = mediaData.Data.TrackNumber,
				};
				return Ok(mediaInfoModel);
			}
            else
            {
				_logger.LogWarning("MediaDownaloder failed to fetch data about a mediaUrl. Errors : {ErrorOutput}", mediaData.ErrorOutput.Append(Environment.NewLine));
				return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch data about your mediaUrl. More information was printed in the server logs.");
			}
		}
	}
}

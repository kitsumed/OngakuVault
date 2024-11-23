using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using OngakuVault.Services;
using System.ComponentModel.DataAnnotations;

namespace OngakuVault.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MediaController : ControllerBase
	{
		readonly ILogger<MediaController> _logger;

		private readonly IMediaDownloaderService _mediaDownloaderService;

		public MediaController(ILogger<MediaController> logger, IMediaDownloaderService mediaDownloaderService)
        {
            _logger = logger;
			_mediaDownloaderService = mediaDownloaderService;
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
			// Fetch video data
			MediaInfoModel? mediaInfoModel = await _mediaDownloaderService.GetMediaInformations(mediaUrl);
			if (mediaInfoModel != null) return Ok(mediaInfoModel);
			// Failed to fetch
			return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch data about your mediaUrl. More information was printed in the server logs.");
		}
	}
}

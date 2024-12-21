using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using OngakuVault.Services;

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
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MediaInfoAdvancedModel))]
		[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status415UnsupportedMediaType, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		public async Task<ActionResult> GetMediaInfo(string mediaUrl)
		{
			if (!Helpers.UrlHelper.IsUrlValid(mediaUrl))
			{
				return BadRequest("mediaUrl scheme can only be http or https.");
			}
			// Fetch video data
			try
			{
				MediaInfoAdvancedModel mediaInfoModel = await _mediaDownloaderService.GetMediaInformations(mediaUrl);
				if (mediaInfoModel != null) return Ok(mediaInfoModel);
			}
			// If it's a NotSupportedException, the error is related to the media information and can be send to client
			catch (NotSupportedException ex)
			{
				_logger.LogWarning("Error happened while processing fetched information about mediaUrl : '{mediaUrl}'. Error: {message}", mediaUrl, ex.Message);
				return StatusCode(StatusCodes.Status415UnsupportedMediaType, ex.Message);
			}
			// If it's a know error (a "normal" error) returned often by the scrapper, can be send to the client
			catch (Exception ex) when (ex.Message.StartsWith("[ONGAKU-SAFE]"))
			{
				_logger.LogWarning("Known error was returned by the scrapper when using mediaUrl : '{mediaUrl}'. Error: {message}", mediaUrl, ex.Message);
				return StatusCode(StatusCodes.Status500InternalServerError, ex.Message.Replace("[ONGAKU-SAFE]", string.Empty));
			}
			catch(Exception ex) 
			{
				// We print other error as Error since they are not planned
				_logger.LogError(ex, "An unexpected error occurred while fetching media information. Error: {message}", ex.Message);
			}
			// If request failed, return a vague error message to client
			return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch data about your mediaUrl. More information was printed in the server logs.");
		}
	}
}

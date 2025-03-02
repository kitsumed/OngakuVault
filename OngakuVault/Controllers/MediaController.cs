using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using OngakuVault.Services;
using static OngakuVault.Helpers.ScraperErrorOutputHelper;

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
		[EndpointDescription("Use the scraper to get advanced informations about a media")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MediaInfoAdvancedModel))]
		[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status415UnsupportedMediaType, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		public async Task<ActionResult> GetMediaInfo(string mediaUrl, CancellationToken cancellationToken)
		{
			if (!Helpers.UrlHelper.IsUrlUsingHttpScheme(mediaUrl))
			{
				return BadRequest("mediaUrl scheme can only be http or https.");
			}
			// Fetch video data
			try
			{
				MediaInfoAdvancedModel mediaInfoModel = await _mediaDownloaderService.GetMediaInformations(mediaUrl, true, false, cancellationToken);
				if (mediaInfoModel != null) return Ok(mediaInfoModel);
			}
			// Ignore canceledException when it's thrown due to the cancel signal on our cancellationToken
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {}
			// If it's a NotSupportedException, the error is related to the media information and can be send to client
			catch (NotSupportedException ex)
			{
				_logger.LogWarning("Error happened while processing fetched information about mediaUrl : '{mediaUrl}'. Error: {message}", mediaUrl, ex.Message);
				return StatusCode(StatusCodes.Status415UnsupportedMediaType, ex.Message);
			}
			catch (ProcessedScraperErrorOutputException ex)
			{
				// If it's a know error (a "normal" error) returned often by the scraper, can be send to the client
				if (ex.IsKnownError)
				{
					_logger.LogWarning("Known scraper error was returned by the scraper. Using mediaUrl : '{mediaUrl}'. Error: {message}. Original Scraper Error: {originalError}", mediaUrl, ex.Message, ex.OriginalError);
					return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
				}
				else _logger.LogError("An unexpected scraper error occurred while fetching media information on : '{mediaUrl}'. Error: {message}", mediaUrl, ex.Message);
			}
			catch (Exception ex) // Handle every other errors
			{
				_logger.LogError(ex, "An unexpected error occurred while fetching media information. Error: {message}", ex.Message);
			}
			// If request failed, return a vague error message to client
			return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch data about your mediaUrl. More information was printed in the server logs.");
		}
	}
}

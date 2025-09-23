using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OngakuVault.Models;
using OngakuVault.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace OngakuVault.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class DirectoryController : ControllerBase
	{
		readonly ILogger<DirectoryController> _logger;
		readonly IDirectoryScanService _directoryScanService;

		public DirectoryController(ILogger<DirectoryController> logger, IDirectoryScanService directoryScanService)
		{
			_logger = logger;
			_directoryScanService = directoryScanService;
		}

		[HttpGet("suggestions")]
		[EndpointDescription("Get directory suggestions for artist and album names based on existing folder structure")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DirectorySuggestionsModel))]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[Produces("application/json")]
		public ActionResult<DirectorySuggestionsModel> GetDirectorySuggestions([FromQuery] string? artistFilter = null, [FromQuery] string? albumFilter = null)
		{
			try
			{
				var suggestions = _directoryScanService.GetDirectorySuggestions(artistFilter, albumFilter);
				
				if (suggestions == null || (!suggestions.Artists.Any() && !suggestions.Albums.Any()))
				{
					return NoContent();
				}

				return Ok(suggestions);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting directory suggestions");
				return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve directory suggestions");
			}
		}

		[HttpGet("enabled")]
		[EndpointDescription("Check if directory suggestions feature is enabled based on OUTPUT_SUB_DIRECTORY_FORMAT configuration")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
		[Produces("application/json")]
		public ActionResult<bool> IsDirectorySuggestionsEnabled()
		{
			return Ok(_directoryScanService.IsDirectorySuggestionsEnabled());
		}
	}
}
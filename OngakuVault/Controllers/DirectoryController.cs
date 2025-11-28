using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using OngakuVault.Services;

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

		/// <response code="200">Return a list of suggestions nodes</response>
		/// <response code="204">No suggestions found</response>
		/// <response code="400">Return the BadRequest reason as a string</response>
		[HttpPost("suggestions")]
		[EndpointDescription("Get directory name suggestions based on OUTPUT_SUB_DIRECTORY_FORMAT schema and existing folder structure, allowing easier typos prevention when filling out inputs to create a audio download job. AKA : autocomplete feature.")]
		[EndpointSummary("Get directory name suggestions based on the folder structure schema")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DirectorySuggestionNode>))]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
		[Produces("application/json")]
		public ActionResult<List<DirectorySuggestionNode>> GetDirectorySuggestions([FromBody] DirectorySuggestionRequest request)
		{
			try
			{
				if (request.Depth < 0)
				{
					return BadRequest("Depth cannot be negative");
				}

				List<DirectorySuggestionNode>? suggestions = _directoryScanService.GetDirectorySuggestions(request);
				
				if (suggestions == null || !suggestions.Any())
				{
					return NoContent();
				}

				return Ok(suggestions);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting directory suggestions");
				return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve directory suggestions. More informations in server logs.");
			}
		}

		/// <response code="200">Return a string containing the OUTPUT_SUB_DIRECTORY_FORMAT schema</response>
		[HttpGet("schema")]
		[EndpointDescription("Get the parsed directory schema from OUTPUT_SUB_DIRECTORY_FORMAT")]
		[EndpointSummary("Get the directory schema")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
		[Produces("application/json")]
		public ActionResult<List<string>> GetDirectorySchema()
		{
			try
			{
				List<string> schema = _directoryScanService.GetDirectorySchema();
				return Ok(schema);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting directory schema");
				return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve directory schema. More informations in server logs.");
			}
		}


		/// <response code="200">Return a boolean</response>
		[HttpGet("enabled")]
		[EndpointDescription("Check if directory suggestions feature is enabled based on OUTPUT_SUB_DIRECTORY_FORMAT and DISABLE_DIRECTORY_SUGGESTIONS configuration")]
		[EndpointSummary("Check if directory suggestions are available")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
		[Produces("application/json")]
		public ActionResult<bool> IsDirectorySuggestionsEnabled()
		{
			return Ok(_directoryScanService.IsDirectorySuggestionsEnabled());
		}
	}
}
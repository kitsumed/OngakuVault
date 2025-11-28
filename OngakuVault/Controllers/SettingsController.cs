using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OngakuVault.Models;

namespace OngakuVault.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SettingsController : ControllerBase
	{
		readonly AppSettingsModel _appSettings;

		public SettingsController(IOptions<AppSettingsModel> appSettings)
		{
			_appSettings = appSettings.Value;
		}

		/// <response code="200">The character used as separator for multiple metadata values</response>
		[HttpGet("metadata-separator")]
		[EndpointDescription("Get the character used as separator for multiple values in metadata fields like Artist or Genre. This is configured via METADATA_VALUE_SEPARATOR setting.")]
		[EndpointSummary("Get the metadata value separator character")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(char))]
		[Produces("application/json")]
		public ActionResult<char> GetMetadataValueSeparator()
		{
			return Ok(_appSettings.METADATA_VALUE_SEPARATOR);
		}
	}
}

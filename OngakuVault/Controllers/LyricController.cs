using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using SubtitlesParserV2;
using SubtitlesParserV2.Models;
using System.Text;

namespace OngakuVault.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class LyricController : ControllerBase
	{
		readonly ILogger<LyricController> _logger;
		public LyricController(ILogger<LyricController> logger)
        {
            _logger = logger;
        }

		[HttpPost("getLyricsFromFile")]
		[EndpointDescription("Parse a lyrics file encoded in UTF8 into a valid list of MediaLyric items. Size limit of 4MB.")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MediaLyric>))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status415UnsupportedMediaType, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		[RequestSizeLimit(4194304)] // 4MB in bytes
		public ActionResult GetLyricsFromFile(IFormFile File)
		{
			string fileExtension = Path.GetExtension(File.FileName).Remove(0, 1); // File extension without the dot
			SubtitleFormatType? fileFormat = SubtitleFormat.GetFormatTypeByFileExtensionName(fileExtension);
			// All of the parsers that will be used to try to parse the file
			List<SubtitleFormatType> supportedFormats = SubtitleFormat.Formats.Keys.ToList();

			if (fileFormat.HasValue) 
			{
				// Ensure the detect file format is the first in the list of supported formats.
				supportedFormats.Remove(fileFormat.Value);
				supportedFormats.Insert(0, fileFormat.Value);
			}

			using Stream fileStream = File.OpenReadStream();
			// Try parsing with different parsers type in the order of the list
			SubtitleParserResultModel? parsedFileResults = SubtitleParser.ParseStream(fileStream, Encoding.UTF8, supportedFormats);
			if (parsedFileResults != null)
			{
				List<MediaLyric> lyrics = new List<MediaLyric>();
				// Loop trought all of the parsed subtitles
				foreach (SubtitleModel subtitleContent in parsedFileResults.Subtitles)
				{
					lyrics.Add(new MediaLyric()
					{
						Content = string.Join(' ', subtitleContent.Lines),
						Time = subtitleContent.StartTime >= 0 ? subtitleContent.StartTime : null
					});
				}
				return Ok(lyrics);
			}
			return StatusCode(StatusCodes.Status422UnprocessableEntity, "The parser was not able to parse your file successfully. This could be because your file is not supported, is malformed, or the parser had a unexpected exception.");
		}
	}
}

using Microsoft.AspNetCore.Mvc;

namespace OngakuVault.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MediaController : ControllerBase
	{
		readonly ILogger<MediaController> _logger;
		public MediaController(ILogger<MediaController> logger)
        {
            _logger = logger;
        }
    }
}

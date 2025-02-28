using Microsoft.AspNetCore.Mvc;
using OngakuVault.Services;
using System.Net.WebSockets;

namespace OngakuVault.Controllers
{
	/// <summary>
	/// Handle the connection of clients to the WebSocketService
	/// </summary>
	[ApiController]
	public class WebSocketController : ControllerBase
	{
		private ILogger<WebSocketController> _logger;
		private IWebSocketManagerService _webSocketManagerService;

		public WebSocketController(ILogger<WebSocketController> logger, IWebSocketManagerService webSocketManagerService)
		{
			_logger = logger;
			_webSocketManagerService = webSocketManagerService;
		}

		/*[HttpGet("test")]
		[EndpointDescription("test")]
		[Produces("application/json")]
		public ActionResult test()
		{
			_webSocketManagerService.BroadcastAsync<string>("update", "test");
			return Ok();
		}*/

		[Route("/ws")] // See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-8.0
		[ApiExplorerSettings(IgnoreApi = true)] // Swagger does not support websocket (as it require a mvc.HttpGetAttribute, who cause websocket to stop working)
		public async Task ConnectToWebSocket()
		{
			// Ensure the request is a websocket
			if (!HttpContext.WebSockets.IsWebSocketRequest)
			{
				HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
				return;
			}

			using WebSocket webSocketConnection = await HttpContext.WebSockets.AcceptWebSocketAsync();
			// Add the websocket connection to the websocket manager list
			bool wasWebSocketClientAdded = _webSocketManagerService.TryAddClient(webSocketConnection, out Guid webSocketClientId);
			if (!wasWebSocketClientAdded)
			{
				_logger.LogWarning("Failed to add websocket connection to the WebSocketManagerService list.");
				HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
				return;
			};

			try
			{
				// Keep the WebSocket open until the client disconnect
				byte[] buffer = new byte[1024 * 4]; // 4kb buffer
				while (webSocketConnection.State == WebSocketState.Open)
				{
					WebSocketReceiveResult websocketResult = await webSocketConnection.ReceiveAsync(buffer, CancellationToken.None);
					// Check if websocket connection is closing
					if (websocketResult.CloseStatus.HasValue)
					{
						// Send a close handshake to the client to handle closure
						await webSocketConnection.CloseAsync(websocketResult.CloseStatus.Value, websocketResult.CloseStatusDescription, CancellationToken.None);
						break;
					}

					// We don't do anything the the client received data (websocketResult)
				}
			}
			catch (WebSocketException ex) 
			{
				// Do not log if the error is related to Prematurely closed websocket (Can happen at server closure with clients connected for example)
				if (ex.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely) 
				{
					_logger.LogError(ex, "Unexpected WebSocket error occured with connection client ID : '{clientId}'. Error: {message}", webSocketClientId, ex.Message);
				}
			}
			finally
			{
				// Remove and dispose of the client connection
				_ = _webSocketManagerService.TryRemoveClient(webSocketClientId);
			}
		}
	}
}

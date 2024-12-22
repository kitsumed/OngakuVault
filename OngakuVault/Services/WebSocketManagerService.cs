using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace OngakuVault.Services
{
	public interface IWebSocketManagerService
	{
		/// <summary>
		/// Try to add a websocket connection to the connections list of the WebSockerManager.
		/// </summary>
		/// <param name="webSocket">The websocket connection</param>
		/// <param name="clientId">The unique clientId created for that websocket connection</param>
		/// <returns>True if it was added, false if it failed</returns>
		public bool TryAddClient(WebSocket webSocket, out Guid clientId);

		/// <summary>
		/// Try to remove and dispose of the websocket connection of a specific client inside the
		/// WebSocketManager list.
		/// </summary>
		/// <param name="clientId">The unique clientId of the websocket connection</param>
		/// <returns>True if it was removed and disposed, else false</returns>
		public bool TryRemoveClient(Guid clientId);

		public Task BroadcastAsync(string message);
	}

	/// <summary>
	/// This class implement the <see cref="IWebSocketManagerService"></see> interface,
	/// allowing to manages websocket connections
	/// </summary>
	public class WebSocketManagerService : IWebSocketManagerService
	{
		/// <summary>
		/// List of clients connection to the websocket
		/// </summary>
		private readonly ConcurrentDictionary<Guid, WebSocket> ClientsConnection = new ConcurrentDictionary<Guid, WebSocket>();

		public bool TryAddClient(WebSocket webSocket, out Guid clientId)
		{
			clientId = Guid.NewGuid();
			return ClientsConnection.TryAdd(clientId, webSocket);
		}

		public bool TryRemoveClient(Guid clientId)
		{
			bool sucess = ClientsConnection.TryRemove(clientId, out WebSocket? webSocket);
			if (sucess)
			{
				webSocket?.Dispose();
			}
			return sucess;
		}

		public async Task BroadcastAsync(string message)
		{
			// Convert the message to UTF8 bytes
			byte[] buffer = Encoding.UTF8.GetBytes(message);
			// Run multiple async thread for every client connection
			IEnumerable<Task> allWebSocketTaks = ClientsConnection.Values.Select(async webSocket =>
			{
				if (webSocket.State == WebSocketState.Open)
				{
					await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
				}
			});
			await Task.WhenAll(allWebSocketTaks);
		}
	}
}

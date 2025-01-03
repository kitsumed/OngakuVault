using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

		/// <summary>
		/// Send a <see cref="WebSocketBroadcastDataModel{T}"/> to every clients (websocket connection) in json
		/// format
		/// </summary>
		/// <typeparam name="T">The type of the data</typeparam>
		/// <param name="key">A custom name to allow the client to differentiate how they should handle the DATA value</param>
		/// <param name="data">The data send to the client</param>
		/// <returns></returns>
		public Task BroadcastAsync<T>(string key, T data);
	}

	/// <summary>
	/// This class implement the <see cref="IWebSocketManagerService"></see> interface,
	/// allowing to manage and interact with websocket connections
	/// </summary>
	public class WebSocketManagerService : IWebSocketManagerService
	{
		/// <summary>
		/// List of clients connection to the websocket
		/// </summary>
		private readonly ConcurrentDictionary<Guid, WebSocket> ClientsConnection = new ConcurrentDictionary<Guid, WebSocket>();

		/// <summary>
		/// Create a json serialisation config one time and re-use it across all method call
		/// </summary>
		private readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
		{
			// Since the WebApplicationBuilder has its json serialiser configured to use "camelCase",
			// we ensure consistency here by using it
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			// Add the StringEnumConverter to convert all enums to their string name insead of int value
			// again, ensuring consistency with WebApplicationBuilder
			Converters = { new JsonStringEnumConverter() },
		};

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

		public async Task BroadcastAsync<T>(string key, T data)
		{
			// Create a broadcastDataModel of the same type of the data we want to send to all clients
			WebSocketBroadcastDataModel<T> broadcastData = new WebSocketBroadcastDataModel<T>
			{
				Key = key,
				Data = data
			};
			string broadcastDataJson = JsonSerializer.Serialize<WebSocketBroadcastDataModel<T>>(broadcastData, JsonSerializerOptions);
			// Convert the json string to UTF8 bytes
			byte[] buffer = Encoding.UTF8.GetBytes(broadcastDataJson);
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

	/// <summary>
	/// This class is a model used specifically by <see cref="WebSocketManagerService"/> to
	/// communicate with clients (websocket connections)
	/// </summary>
	/// <typeparam name="T">The type of the data value</typeparam>
	public class WebSocketBroadcastDataModel<T>
	{
		/// <summary>
		/// This is the name of the update. Allowing the client to
		/// differentiate how they should handle the DATA value
		/// </summary>
		public required string Key { get; set; }

		/// <summary>
		/// Contains the data you want to send to the client
		/// </summary>
		public required T Data { get; set; }
	}
}

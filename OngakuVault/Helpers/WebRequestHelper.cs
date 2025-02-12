using System.Net;

namespace OngakuVault.Helpers
{
	/// <summary>
	/// Contains static methods for making web requests and getting the results
	/// </summary>
	public static class WebRequestHelper
	{
		private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
		{
			DefaultRequestVersion = HttpVersion.Version20, // Use HTTP2
			DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower, // Fall back to HTTP1/1.1 if server does not support
		};

		private static readonly string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

		/// <summary>
		/// Make a request to a website and get the response message
		/// </summary>
		/// <param name="requestUrl">The url of the website you want to get the content</param>
		/// <param name="userAgent">Optional, overwrite the user-agent used for the request</param>
		/// <exception cref="HttpRequestException">If the response http code is not in the 200-299 range</exception>
		/// <remarks>
		/// Don't forget to dispose of the response message.
		/// </remarks>
		/// <returns>The response message</returns>
		public static async Task<HttpResponseMessage> GetContentFromWebsiteAsync(Uri requestUrl, string? userAgent = null)
		{
			// Create the get request & Add headers
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			request.Headers.Add("Host", requestUrl.Host);
			// Set the referer path before the current "/"
			request.Headers.Add("Referer", requestUrl.AbsoluteUri.Substring(0, requestUrl.AbsoluteUri.LastIndexOf('/')));
			// Theses values are taken from https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers
			request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			request.Headers.Add("Accept-Language", "en-US,en;q=0.5,*");
			// All compression method supported by System.Net.DecompressionMethods
			request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
			request.Headers.Add("User-Agent", userAgent ?? DefaultUserAgent);

			// Do not dispose yet, this would dispose the response before we can use it
			HttpResponseMessage response = await Client.SendAsync(request);
			// Throw a error if the response is invalid.
			response.EnsureSuccessStatusCode();
			// Return the page content (DOM)
			return response;
		}
	}
}

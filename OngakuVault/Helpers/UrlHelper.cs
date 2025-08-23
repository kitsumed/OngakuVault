namespace OngakuVault.Helpers
{
	/// <summary>
	/// Contains static methods with simple actions regarding urls.
	/// </summary>
	public static class UrlHelper
	{
		/// <summary>
		/// Verify if the string can be converted to a Uri and is of scheme type http or https.
		/// </summary>
		/// <param name="url">The url as a string</param>
		/// <returns>True or false</returns>
		public static bool IsUrlUsingHttpScheme(string url)
		{
			bool isUriValid = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult);
			bool isUriUsingHttpScheme = uriResult?.Scheme == Uri.UriSchemeHttp || uriResult?.Scheme == Uri.UriSchemeHttps;
			return isUriValid && isUriUsingHttpScheme;
		}

		/// <summary>
		/// This will encode your url into a URL escaped string. Turning illegals characters into percents number. 
		/// </summary>
		/// <remarks>
		/// <strong>You should sanitize your url before doing anything OS related, like opening a command prompt that contains
		/// a user given url.</strong>
		/// </remarks>
		/// <param name="url">The url to encode</param>
		/// <returns>The url escaped url OR null if the url could not be parsed</returns>
		public static string? SanitizeUrl(string url)
		{
			// Parse the URL using Uri
			if (Uri.TryCreate(url, UriKind.Absolute, out Uri? urlUri))
			{
				// According to the microsoft docs getting the absolute url append
				// all of the others fields, theses fields get URI escaped when you GET them
				// (https://learn.microsoft.com/en-us/dotnet/api/system.uri.query?view=net-8.0#remarks) 
				return urlUri.AbsoluteUri;
			}
			// Invalid url format
			return null;
		}
	}
}

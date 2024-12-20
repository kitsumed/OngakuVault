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
		public static bool IsUrlValid(string url)
		{
			bool isUriValid = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult);
			bool isUrlValid = uriResult?.Scheme == Uri.UriSchemeHttp || uriResult?.Scheme == Uri.UriSchemeHttps;
			return isUriValid && isUrlValid;
		}
	}
}

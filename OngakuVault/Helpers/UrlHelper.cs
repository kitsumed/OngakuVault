using System.Linq;

namespace OngakuVault.Helpers
{
	/// <summary>
	/// Contains static methods with simple actions regarding urls and file paths.
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

		/// <summary>
		/// Sanitizes a file name by removing or replacing characters that are illegal on Windows and Linux file systems.
		/// </summary>
		/// <remarks>
		/// <strong>This method handles cross-platform file name sanitization for both Windows and Linux systems.
		/// It replaces illegal characters with underscores and handles Windows reserved names.</strong>
		/// </remarks>
		/// <param name="fileName">The file name to sanitize</param>
		/// <returns>A sanitized file name safe for use on both Windows and Linux</returns>
		public static string SanitizeFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return "unnamed_file";

			// Characters illegal on Windows: < > : " | ? * \ and control characters (0-31)
			// Characters illegal on Linux: / and null character
			// We'll be conservative and handle both platforms
			char[] illegalChars = { '<', '>', ':', '"', '|', '?', '*', '\\', '/' };
			
			string sanitized = fileName;
			
			// Replace illegal characters with underscores
			foreach (char c in illegalChars)
			{
				sanitized = sanitized.Replace(c, '_');
			}
			
			// Remove control characters (0-31) and DEL (127)
			sanitized = new string(sanitized.Where(c => c >= 32 && c != 127).ToArray());
			
			// Separate name and extension for proper handling
			string nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
			string extension = Path.GetExtension(sanitized);
			
			// Trim leading and trailing spaces and dots from the name part (problematic on Windows)
			nameWithoutExtension = nameWithoutExtension.Trim(' ', '.');
			
			// Handle the case where trimming resulted in an empty name
			if (string.IsNullOrWhiteSpace(nameWithoutExtension))
				nameWithoutExtension = "unnamed_file";
			
			// Handle Windows reserved names
			string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
			
			if (reservedNames.Contains(nameWithoutExtension.ToUpperInvariant()))
			{
				nameWithoutExtension = nameWithoutExtension + "_file";
			}
			
			// Reconstruct the file name
			return nameWithoutExtension + extension;
		}

		/// <summary>
		/// Sanitizes a directory path by sanitizing each path component individually.
		/// </summary>
		/// <remarks>
		/// <strong>This method handles cross-platform directory path sanitization for both Windows and Linux systems.
		/// It processes each directory component separately to ensure path safety.</strong>
		/// </remarks>
		/// <param name="directoryPath">The directory path to sanitize</param>
		/// <returns>A sanitized directory path safe for use on both Windows and Linux</returns>
		public static string SanitizeDirectoryPath(string directoryPath)
		{
			if (string.IsNullOrWhiteSpace(directoryPath))
				return string.Empty;

			// Split the path into components and sanitize each one
			string[] pathComponents = directoryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
			
			for (int i = 0; i < pathComponents.Length; i++)
			{
				pathComponents[i] = SanitizeFileName(pathComponents[i]);
			}
			
			return string.Join(Path.DirectorySeparatorChar.ToString(), pathComponents);
		}
	}
}

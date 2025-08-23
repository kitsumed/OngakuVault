using System.Linq;
using System.Text;

namespace OngakuVault.Helpers
{
	/// <summary>
	/// Contains static methods for file system operations including file name sanitization.
	/// </summary>
	public static class FileSystemHelper
	{
		// Windows reserved names - defined outside method for performance
		private static readonly string[] WindowsReservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

		/// <summary>
		/// Sanitizes a file name by removing or replacing characters that are illegal on Windows and Linux file systems.
		/// </summary>
		/// <remarks>
		/// <strong>This method handles cross-platform file name sanitization for both Windows and Linux systems.
		/// It replaces illegal characters with underscores and handles Windows reserved names when running on Windows.</strong>
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
			
			var sb = new StringBuilder(fileName.Length);
			
			// Replace illegal characters with underscores and remove control characters in one pass
			foreach (char c in fileName)
			{
				if (c < 32 || c == 127)
				{
					// Skip control characters (0-31) and DEL (127)
					continue;
				}
				else if (Array.IndexOf(illegalChars, c) >= 0)
				{
					// Replace illegal characters with underscores
					sb.Append('_');
				}
				else
				{
					// Keep valid characters
					sb.Append(c);
				}
			}
			
			string sanitized = sb.ToString();
			
			// Separate name and extension for proper handling
			string nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
			string extension = Path.GetExtension(sanitized);
			
			// Trim leading and trailing spaces from the name part
			nameWithoutExtension = nameWithoutExtension.Trim(' ');
			
			// Handle the case where trimming resulted in an empty name
			if (string.IsNullOrWhiteSpace(nameWithoutExtension))
				nameWithoutExtension = "unnamed_file";
			
			// Handle Windows reserved names only when running on Windows
			if (OperatingSystem.IsWindows() && WindowsReservedNames.Contains(nameWithoutExtension.ToUpperInvariant()))
			{
				nameWithoutExtension = nameWithoutExtension + "_file";
			}
			
			// Reconstruct the file name using StringBuilder
			sb.Clear();
			sb.Append(nameWithoutExtension);
			sb.Append(extension);
			return sb.ToString();
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
			
			// Use StringBuilder for efficient path joining
			var sb = new StringBuilder();
			for (int i = 0; i < pathComponents.Length; i++)
			{
				if (i > 0)
					sb.Append(Path.DirectorySeparatorChar);
				sb.Append(pathComponents[i]);
			}
			
			return sb.ToString();
		}
	}
}
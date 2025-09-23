using OngakuVault.Models;

namespace OngakuVault.Services
{
	/// <summary>
	/// Service interface for scanning directories and providing suggestions based on OUTPUT_SUB_DIRECTORY_FORMAT schema
	/// </summary>
	public interface IDirectoryScanService
	{
		/// <summary>
		/// Get directory suggestions based on existing folder structure and schema
		/// </summary>
		/// <param name="request">Request parameters including depth, parent context, and filter</param>
		/// <returns>Directory suggestions model</returns>
		DirectorySuggestionsModel? GetDirectorySuggestions(DirectorySuggestionRequest request);

		/// <summary>
		/// Get the parsed schema from OUTPUT_SUB_DIRECTORY_FORMAT
		/// </summary>
		/// <returns>List of token types in order</returns>
		List<string> GetDirectorySchema();

		/// <summary>
		/// Check if directory suggestions feature is enabled based on configuration
		/// </summary>
		/// <returns>True if directory suggestions are enabled</returns>
		bool IsDirectorySuggestionsEnabled();
	}
}
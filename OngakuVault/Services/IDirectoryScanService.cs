using OngakuVault.Models;

namespace OngakuVault.Services
{
	/// <summary>
	/// Service interface for scanning directories and providing suggestions for artist/album names
	/// </summary>
	public interface IDirectoryScanService
	{
		/// <summary>
		/// Get directory suggestions based on existing folder structure
		/// </summary>
		/// <param name="artistFilter">Optional filter for artist names</param>
		/// <param name="albumFilter">Optional filter for album names</param>
		/// <returns>Directory suggestions model containing artists and albums</returns>
		DirectorySuggestionsModel? GetDirectorySuggestions(string? artistFilter = null, string? albumFilter = null);

		/// <summary>
		/// Check if directory suggestions feature is enabled based on configuration
		/// </summary>
		/// <returns>True if directory suggestions are enabled</returns>
		bool IsDirectorySuggestionsEnabled();

		/// <summary>
		/// Refresh the cached directory structure
		/// </summary>
		void RefreshCache();
	}
}
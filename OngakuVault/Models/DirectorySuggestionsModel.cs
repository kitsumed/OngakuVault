using Swashbuckle.AspNetCore.Annotations;

namespace OngakuVault.Models
{
	/// <summary>
	/// Model containing directory suggestions for artist and album names
	/// </summary>
	[SwaggerSchema(Description = "Contains directory suggestions based on existing folder structure")]
	public class DirectorySuggestionsModel
	{
		/// <summary>
		/// List of existing artist names found in the directory structure
		/// </summary>
		[SwaggerSchema(Description = "List of existing artist names")]
		public List<string> Artists { get; set; } = new List<string>();

		/// <summary>
		/// Dictionary mapping artist names to their albums
		/// </summary>
		[SwaggerSchema(Description = "Dictionary mapping artist names to their associated album names")]
		public Dictionary<string, List<string>> Albums { get; set; } = new Dictionary<string, List<string>>();
	}

	/// <summary>
	/// Model for individual directory suggestion item
	/// </summary>
	[SwaggerSchema(Description = "Individual directory suggestion with name and path")]
	public class DirectorySuggestionItem
	{
		/// <summary>
		/// The display name of the directory
		/// </summary>
		[SwaggerSchema(Description = "The display name of the directory")]
		public required string Name { get; set; }

		/// <summary>
		/// The full path of the directory
		/// </summary>
		[SwaggerSchema(Description = "The full path of the directory")]
		public required string Path { get; set; }
	}
}
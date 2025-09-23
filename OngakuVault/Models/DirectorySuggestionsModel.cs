using Swashbuckle.AspNetCore.Annotations;

namespace OngakuVault.Models
{
	/// <summary>
	/// Model containing directory suggestions based on the OUTPUT_SUB_DIRECTORY_FORMAT schema
	/// </summary>
	[SwaggerSchema(Description = "Contains directory suggestions based on existing folder structure and schema")]
	public class DirectorySuggestionsModel
	{
		/// <summary>
		/// The parsed directory structure schema from OUTPUT_SUB_DIRECTORY_FORMAT
		/// </summary>
		[SwaggerSchema(Description = "Ordered list of directory tokens from the format schema")]
		public List<string> Schema { get; set; } = new List<string>();

		/// <summary>
		/// Hierarchical directory suggestions organized by depth level
		/// </summary>
		[SwaggerSchema(Description = "Directory suggestions organized by schema depth")]
		public Dictionary<int, List<DirectorySuggestionNode>> Suggestions { get; set; } = new Dictionary<int, List<DirectorySuggestionNode>>();
	}

	/// <summary>
	/// Model representing a single directory suggestion node in the hierarchy
	/// </summary>
	[SwaggerSchema(Description = "Single directory suggestion with hierarchical information")]
	public class DirectorySuggestionNode
	{
		/// <summary>
		/// The name/value of this directory level
		/// </summary>
		[SwaggerSchema(Description = "The directory name")]
		public required string Name { get; set; }

		/// <summary>
		/// The token type this represents (e.g., AUDIO_ARTIST, AUDIO_ALBUM, etc.)
		/// </summary>
		[SwaggerSchema(Description = "The audio token type")]
		public required string TokenType { get; set; }

		/// <summary>
		/// The full path to this directory
		/// </summary>
		[SwaggerSchema(Description = "Full path to the directory")]
		public required string Path { get; set; }

		/// <summary>
		/// Child directories under this node
		/// </summary>
		[SwaggerSchema(Description = "Child directory suggestions")]
		public List<DirectorySuggestionNode> Children { get; set; } = new List<DirectorySuggestionNode>();
	}

	/// <summary>
	/// Model for directory suggestion requests with filters
	/// </summary>
	[SwaggerSchema(Description = "Request model for directory suggestions with filtering")]
	public class DirectorySuggestionRequest
	{
		/// <summary>
		/// The depth level to get suggestions for (0-based)
		/// </summary>
		[SwaggerSchema(Description = "Schema depth level (0 = first token, 1 = second token, etc.)")]
		public int Depth { get; set; } = 0;

		/// <summary>
		/// Parent path context for hierarchical filtering
		/// </summary>
		[SwaggerSchema(Description = "Parent directory path for contextual filtering")]
		public string? ParentPath { get; set; }

		/// <summary>
		/// Filter text for matching suggestions
		/// </summary>
		[SwaggerSchema(Description = "Text filter for matching suggestions")]
		public string? Filter { get; set; }
	}
}
using Microsoft.Extensions.Options;
using OngakuVault.Models;
using System.Text.RegularExpressions;

namespace OngakuVault.Services
{
	/// <summary>
	/// Service for scanning directories and providing suggestions based on OUTPUT_SUB_DIRECTORY_FORMAT schema
	/// </summary>
	public class DirectoryScanService : IDirectoryScanService
	{
		readonly ILogger<DirectoryScanService> _logger;
		readonly AppSettingsModel _appSettings;

		// All supported audio tokens from the ValueReplacingHelper
		private static readonly HashSet<string> SupportedAudioTokens = new HashSet<string>
		{
			"AUDIO_TITLE", "AUDIO_ARTIST", "AUDIO_ALBUM", "AUDIO_YEAR", "AUDIO_TRACK_NUMBER",
			"AUDIO_DISC_NUMBER", "AUDIO_LANGUAGE", "AUDIO_GENRE", "AUDIO_COMPOSER",
			"AUDIO_DURATION", "AUDIO_DURATION_MS", "AUDIO_ISRC", "AUDIO_CATALOG_NUMBER"
		};

		public DirectoryScanService(ILogger<DirectoryScanService> logger, IOptions<AppSettingsModel> appSettings)
		{
			_logger = logger;
			_appSettings = appSettings.Value;
		}

		public bool IsDirectorySuggestionsEnabled()
		{
			var schema = GetDirectorySchema();
			return schema.Count > 0;
		}

		public List<string> GetDirectorySchema()
		{
			if (string.IsNullOrEmpty(_appSettings.OUTPUT_SUB_DIRECTORY_FORMAT))
			{
				return new List<string>();
			}

			// Parse the schema to extract audio tokens in order
			var schema = new List<string>();
			var format = _appSettings.OUTPUT_SUB_DIRECTORY_FORMAT;

			// Find all tokens in the format string using regex
			var tokenRegex = new Regex(@"\|([A-Z_]+)\|");
			var matches = tokenRegex.Matches(format);

			foreach (Match match in matches)
			{
				var token = match.Groups[1].Value;
				if (SupportedAudioTokens.Contains(token))
				{
					schema.Add(token);
				}
			}

			return schema;
		}

		public DirectorySuggestionsModel? GetDirectorySuggestions(DirectorySuggestionRequest request)
		{
			var schema = GetDirectorySchema();
			if (schema.Count == 0)
			{
				return null;
			}

			if (request.Depth >= schema.Count || request.Depth < 0)
			{
				return null;
			}

			try
			{
				var suggestions = ScanDirectoryStructure(schema, request);
				return suggestions;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting directory suggestions for depth {Depth}", request.Depth);
				return null;
			}
		}

		private DirectorySuggestionsModel? ScanDirectoryStructure(List<string> schema, DirectorySuggestionRequest request)
		{
			if (!Directory.Exists(_appSettings.OUTPUT_DIRECTORY))
			{
				_logger.LogInformation("Output directory does not exist: {OutputDirectory}", _appSettings.OUTPUT_DIRECTORY);
				return null;
			}

			var result = new DirectorySuggestionsModel
			{
				Schema = schema
			};

			// Get the base directory to scan from
			var basePath = _appSettings.OUTPUT_DIRECTORY;
			var currentDepth = 0;

			// If we have a parent path context, navigate to that level first
			if (!string.IsNullOrEmpty(request.ParentPath))
			{
				var parentParts = request.ParentPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, 
					StringSplitOptions.RemoveEmptyEntries);
				
				if (parentParts.Length <= schema.Count)
				{
					basePath = Path.Combine(_appSettings.OUTPUT_DIRECTORY, request.ParentPath);
					currentDepth = parentParts.Length;
				}
			}

			// If the requested depth is not the current depth, we can't provide suggestions
			if (request.Depth != currentDepth)
			{
				return result;
			}

			// Get suggestions at the requested depth level
			if (Directory.Exists(basePath))
			{
				var directories = Directory.GetDirectories(basePath);
				var suggestions = new List<DirectorySuggestionNode>();

				foreach (var dir in directories)
				{
					var dirName = Path.GetFileName(dir);
					
					// Apply filter if provided
					if (!string.IsNullOrEmpty(request.Filter) && 
						!dirName.Contains(request.Filter, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					var suggestion = new DirectorySuggestionNode
					{
						Name = dirName,
						TokenType = schema[currentDepth],
						Path = Path.GetRelativePath(_appSettings.OUTPUT_DIRECTORY, dir)
					};

					// If there are more levels in the schema, check for children
					if (currentDepth + 1 < schema.Count)
					{
						var childDirs = Directory.GetDirectories(dir);
						foreach (var childDir in childDirs)
						{
							var childName = Path.GetFileName(childDir);
							var childSuggestion = new DirectorySuggestionNode
							{
								Name = childName,
								TokenType = schema[currentDepth + 1],
								Path = Path.GetRelativePath(_appSettings.OUTPUT_DIRECTORY, childDir)
							};
							suggestion.Children.Add(childSuggestion);
						}
					}

					suggestions.Add(suggestion);
				}

				// Sort suggestions by name
				suggestions.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
				result.Suggestions[currentDepth] = suggestions;
			}

			_logger.LogInformation("Found {Count} suggestions for depth {Depth} with token {Token}", 
				result.Suggestions.ContainsKey(currentDepth) ? result.Suggestions[currentDepth].Count : 0,
				currentDepth, 
				currentDepth < schema.Count ? schema[currentDepth] : "Unknown");

			return result;
		}
	}
}
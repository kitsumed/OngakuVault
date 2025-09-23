using Microsoft.Extensions.Options;
using OngakuVault.Models;
using System.Text.RegularExpressions;

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

		/// <summary>
		/// Manually refresh the directory hierarchy cache
		/// </summary>
		void RefreshCache();
	}

	/// <summary>
	/// Service for scanning directories and providing suggestions based on OUTPUT_SUB_DIRECTORY_FORMAT schema
	/// </summary>
	public class DirectoryScanService : IDirectoryScanService
	{
		readonly ILogger<DirectoryScanService> _logger;
		readonly AppSettingsModel _appSettings;
		
		// Server-side caching
		private DirectorySuggestionsModel? _cachedHierarchy;
		private DateTime _cacheTimestamp = DateTime.MinValue;
		private readonly object _cacheLock = new object();

		// All supported audio tokens from the ValueReplacingHelper
		private static readonly HashSet<string> SupportedAudioTokens = new HashSet<string>
		{
			"AUDIO_TITLE", "AUDIO_ARTIST", "AUDIO_ALBUM", "AUDIO_YEAR", "AUDIO_TRACK_NUMBER",
			"AUDIO_DISC_NUMBER", "AUDIO_LANGUAGE", "AUDIO_GENRE", "AUDIO_COMPOSER",
			"AUDIO_DURATION", "AUDIO_DURATION_MS", "AUDIO_ISRC", "AUDIO_CATALOG_NUMBER"
		};

		// Regex that uses the PIPE separation used by the ValueReplacingHelper to detect tokens
		private static readonly Regex TokenRegex = new Regex(@"\|([A-Z_]+)\|");

		public DirectoryScanService(ILogger<DirectoryScanService> logger, IOptions<AppSettingsModel> appSettings)
		{
			_logger = logger;
			_appSettings = appSettings.Value;
		}

		public bool IsDirectorySuggestionsEnabled()
		{
			List<string> schema = GetDirectorySchema();
			return schema.Count > 0;
		}

		public List<string> GetDirectorySchema()
		{
			if (string.IsNullOrEmpty(_appSettings.OUTPUT_SUB_DIRECTORY_FORMAT))
			{
				return new List<string>();
			}

			// Parse the schema to extract audio tokens in order
			List<string> schema = new List<string>();
			string? format = _appSettings.OUTPUT_SUB_DIRECTORY_FORMAT;

			// Find all tokens in the format string using regex
			MatchCollection matches = TokenRegex.Matches(format);

			foreach (Match match in matches)
			{
				string token = match.Groups[1].Value;
				if (SupportedAudioTokens.Contains(token))
				{
					schema.Add(token);
				}
			}

			return schema;
		}

		public DirectorySuggestionsModel? GetDirectorySuggestions(DirectorySuggestionRequest request)
		{
			List<string> schema = GetDirectorySchema();
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
				// Use cached hierarchy if caching is enabled and cache is valid
				DirectorySuggestionsModel? fullHierarchy = null;

				if (_appSettings.DIRECTORY_SUGGESTIONS_CACHE_ENABLED)
				{
					lock (_cacheLock)
					{
						var cacheExpiry = TimeSpan.FromMinutes(_appSettings.DIRECTORY_SUGGESTIONS_CACHE_REFRESH_MINUTES);
						var now = DateTime.Now;

						if (_cachedHierarchy == null || now - _cacheTimestamp > cacheExpiry)
						{
							_logger.LogDebug("Refreshing directory hierarchy cache");
							_cachedHierarchy = ScanDirectoryStructure(schema);
							_cacheTimestamp = now;
						}

						fullHierarchy = _cachedHierarchy;
					}
				}
				else
				{
					// No caching - scan directly
					fullHierarchy = ScanDirectoryStructure(schema);
				}

				if (fullHierarchy == null)
				{
					return null;
				}

				// Filter the full hierarchy based on the request
				return FilterHierarchyForRequest(fullHierarchy, request);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting directory suggestions for depth {Depth}", request.Depth);
				return null;
			}
		}

		public void RefreshCache()
		{
			if (!_appSettings.DIRECTORY_SUGGESTIONS_CACHE_ENABLED)
			{
				return;
			}

			var schema = GetDirectorySchema();
			if (schema.Count == 0)
			{
				return;
			}

			lock (_cacheLock)
			{
				_logger.LogInformation("Manually refreshing directory hierarchy cache");
				_cachedHierarchy = ScanDirectoryStructure(schema);
				_cacheTimestamp = DateTime.Now;
			}
		}

		private DirectorySuggestionsModel FilterHierarchyForRequest(DirectorySuggestionsModel fullHierarchy, DirectorySuggestionRequest request)
		{
			var result = new DirectorySuggestionsModel
			{
				Schema = fullHierarchy.Schema
			};

			// Get suggestions at the requested depth level
			if (!fullHierarchy.Suggestions.ContainsKey(request.Depth))
			{
				return result;
			}

			var allSuggestionsAtDepth = fullHierarchy.Suggestions[request.Depth];
			var filteredSuggestions = new List<DirectorySuggestionNode>();

			foreach (var suggestion in allSuggestionsAtDepth)
			{
				// Apply parent path filter
				if (!string.IsNullOrEmpty(request.ParentPath))
				{
					var parentPathParts = request.ParentPath.Split('/', '\\');
					var suggestionPathParts = suggestion.Path.Split('/', '\\');

					// Check if this suggestion matches the parent path context
					var matches = true;
					for (int i = 0; i < parentPathParts.Length && i < suggestionPathParts.Length - 1; i++)
					{
						if (!parentPathParts[i].Equals(suggestionPathParts[i], StringComparison.OrdinalIgnoreCase))
						{
							matches = false;
							break;
						}
					}

					if (!matches)
					{
						continue;
					}
				}

				// Apply text filter
				if (!string.IsNullOrEmpty(request.Filter) && 
					!suggestion.Name.Contains(request.Filter, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				filteredSuggestions.Add(suggestion);
			}

			if (filteredSuggestions.Count > 0)
			{
				result.Suggestions[request.Depth] = filteredSuggestions;
			}

			return result;
		}

		// Overloaded method to scan without request context (for caching full hierarchy)
		private DirectorySuggestionsModel? ScanDirectoryStructure(List<string> schema)
		{
			if (!Directory.Exists(_appSettings.OUTPUT_DIRECTORY))
			{
				_logger.LogWarning("Output directory does not exist: {OutputDirectory}. Cannot scan directory structure.", _appSettings.OUTPUT_DIRECTORY);
				return null;
			}

			var result = new DirectorySuggestionsModel
			{
				Schema = schema
			};

			try
			{
				// Build the complete directory hierarchy
				var allSuggestionsByDepth = new Dictionary<int, List<DirectorySuggestionNode>>();

				// Get all subdirectories recursively
				var allDirectories = Directory.GetDirectories(_appSettings.OUTPUT_DIRECTORY, "*", SearchOption.AllDirectories);

				foreach (var dirPath in allDirectories)
				{
					var relativePath = Path.GetRelativePath(_appSettings.OUTPUT_DIRECTORY, dirPath);
					var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

					// Create suggestions for each depth level that corresponds to schema
					for (int depth = 0; depth < Math.Min(pathParts.Length, schema.Count); depth++)
					{
						if (!allSuggestionsByDepth.ContainsKey(depth))
						{
							allSuggestionsByDepth[depth] = new List<DirectorySuggestionNode>();
						}

						var pathUpToDepth = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Take(depth + 1));
						var suggestion = new DirectorySuggestionNode
						{
							Name = pathParts[depth],
							TokenType = schema[depth],
							Path = pathUpToDepth,
							Children = new List<DirectorySuggestionNode>()
						};

						// Check if this suggestion already exists
						var existingSuggestion = allSuggestionsByDepth[depth].FirstOrDefault(s => 
							s.Name.Equals(suggestion.Name, StringComparison.OrdinalIgnoreCase) &&
							s.Path.Equals(suggestion.Path, StringComparison.OrdinalIgnoreCase));

						if (existingSuggestion == null)
						{
							allSuggestionsByDepth[depth].Add(suggestion);
						}
					}
				}

				// Sort all suggestions by name
				foreach (var kvp in allSuggestionsByDepth)
				{
					kvp.Value.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
				}

				result.Suggestions = allSuggestionsByDepth;

				_logger.LogDebug("Scanned complete directory hierarchy: {DepthCount} levels with total {SuggestionCount} suggestions", 
					allSuggestionsByDepth.Count, allSuggestionsByDepth.Values.Sum(list => list.Count));

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error scanning directory structure");
				return null;
			}
		}

		private DirectorySuggestionsModel? ScanDirectoryStructure(List<string> schema, DirectorySuggestionRequest request)
		{
			if (!Directory.Exists(_appSettings.OUTPUT_DIRECTORY))
			{
				_logger.LogWarning("Output directory does not exist: {OutputDirectory}. Cannot scan directory structure.", _appSettings.OUTPUT_DIRECTORY);
				return null;
			}

			DirectorySuggestionsModel result = new DirectorySuggestionsModel
			{
				Schema = schema
			};

			// Get the base directory to scan from
			string basePath = _appSettings.OUTPUT_DIRECTORY;
			int currentDepth = 0;

			// If we have a parent path context, navigate to that level first
			if (!string.IsNullOrEmpty(request.ParentPath))
			{
				string[] parentParts = request.ParentPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
				
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
				string[] directories = Directory.GetDirectories(basePath);
				List<DirectorySuggestionNode> suggestions = new List<DirectorySuggestionNode>();

				foreach (string dir in directories)
				{
					string dirName = Path.GetFileName(dir);
					
					// Apply filter if provided
					if (!string.IsNullOrEmpty(request.Filter) && !dirName.Contains(request.Filter, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					DirectorySuggestionNode suggestion = new DirectorySuggestionNode
					{
						Name = dirName,
						TokenType = schema[currentDepth],
						Path = Path.GetRelativePath(_appSettings.OUTPUT_DIRECTORY, dir)
					};

					// If there are more levels in the schema, check for children
					if ((currentDepth + 1) < schema.Count)
					{
						string[] childDirs = Directory.GetDirectories(dir);
						foreach (var childDir in childDirs)
						{
							string childName = Path.GetFileName(childDir);
							DirectorySuggestionNode childSuggestion = new DirectorySuggestionNode
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

			_logger.LogDebug("Found {Count} suggestions for depth {Depth} with token {Token}", 
				result.Suggestions.ContainsKey(currentDepth) ? result.Suggestions[currentDepth].Count : 0,
				currentDepth, 
				currentDepth < schema.Count ? schema[currentDepth] : "Unknown");

			return result;
		}
	}
}
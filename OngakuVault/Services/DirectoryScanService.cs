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
		/// <returns>List of directory suggestions</returns>
		List<DirectorySuggestionNode>? GetDirectorySuggestions(DirectorySuggestionRequest request);

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
		/// Manually refresh the directory hierarchy cache.
		/// Get ignored if caching is disabled or directory name autofill feature is disabled.
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
		private DirectoryHierarchyCache? _cachedHierarchy;
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
			return schema.Count > 0 && !_appSettings.DISABLE_DIRECTORY_SUGGESTIONS;
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

		public List<DirectorySuggestionNode>? GetDirectorySuggestions(DirectorySuggestionRequest request)
		{
			// If feature is disabled, return null
			if (!IsDirectorySuggestionsEnabled()) return null;

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
				DirectoryHierarchyCache? hierarchyCache = null;

				if (_appSettings.DIRECTORY_SUGGESTIONS_CACHE_ENABLED)
				{
					// Prevent concurrent access to the cache, other access requests will wait until the lock is released
					lock (_cacheLock)
					{
						TimeSpan cacheExpiry = TimeSpan.FromMinutes(_appSettings.DIRECTORY_SUGGESTIONS_CACHE_REFRESH_MINUTES);
						DateTime now = DateTime.Now;

						if (_cachedHierarchy == null || now - _cacheTimestamp > cacheExpiry)
						{
							_logger.LogDebug("Refreshing directory hierarchy cache");
							_cachedHierarchy = BuildDirectoryHierarchy(schema);
							_cacheTimestamp = now;
						}

						hierarchyCache = _cachedHierarchy;
					}
				}
				else
				{
					// No caching, scan directly
					hierarchyCache = BuildDirectoryHierarchy(schema);
				}

				if (hierarchyCache == null)
				{
					return null;
				}

				// Filter the hierarchy and return suggestions directly
				return FilterHierarchyForRequest(hierarchyCache, request);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting directory suggestions for depth {Depth}", request.Depth);
				return null;
			}
		}

		public void RefreshCache()
		{
			if (!_appSettings.DIRECTORY_SUGGESTIONS_CACHE_ENABLED || _appSettings.DISABLE_DIRECTORY_SUGGESTIONS)
			{
				return;
			}

			List<string> schema = GetDirectorySchema();
			if (schema.Count == 0)
			{
				return;
			}

			// Prevent concurrent access to the cache, other access requests will wait until the lock is released
			lock (_cacheLock)
			{
				_logger.LogInformation("Manually refreshing directory hierarchy cache");
				_cachedHierarchy = BuildDirectoryHierarchy(schema);
				_cacheTimestamp = DateTime.Now;
			}
		}

		private List<DirectorySuggestionNode> FilterHierarchyForRequest(DirectoryHierarchyCache hierarchyCache, DirectorySuggestionRequest request)
		{
			List<DirectorySuggestionNode> filteredSuggestions = new List<DirectorySuggestionNode>();

			// Get suggestions at the requested depth level
			if (!hierarchyCache.SuggestionsByDepth.ContainsKey(request.Depth))
			{
				return filteredSuggestions;
			}

			List<DirectorySuggestionNode> allSuggestionsAtDepth = hierarchyCache.SuggestionsByDepth[request.Depth];

			foreach (DirectorySuggestionNode? suggestion in allSuggestionsAtDepth)
			{
				// Apply parent path filter
				if (!string.IsNullOrEmpty(request.ParentPath))
				{
					string[] parentPathParts = request.ParentPath.Split('/', '\\');
					string[] suggestionPathParts = suggestion.Path.Split('/', '\\');

					// Check if this suggestion matches the parent path context
					bool matches = true;
					for (int i = 0; i < parentPathParts.Length && i < suggestionPathParts.Length - 1; i++)
					{
						if (!parentPathParts[i].Equals(suggestionPathParts[i], StringComparison.OrdinalIgnoreCase))
						{
							// Stop checking this suggestions as it does not matches, go to next one
							matches = false;
							break;
						}
					}

					if (!matches)
					{
						continue; // Verify next one
					}
				}

				// Apply text filter
				if (!string.IsNullOrEmpty(request.Filter) && !suggestion.Name.Contains(request.Filter, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				filteredSuggestions.Add(suggestion);
			}

			return filteredSuggestions;
		}

		// Method to build complete directory hierarchy for caching
		private DirectoryHierarchyCache? BuildDirectoryHierarchy(List<string> schema)
		{
			if (!Directory.Exists(_appSettings.OUTPUT_DIRECTORY))
			{
				_logger.LogWarning("Output directory does not exist: {OutputDirectory}. Cannot scan directory structure.", _appSettings.OUTPUT_DIRECTORY);
				return null;
			}

			DirectoryHierarchyCache result = new DirectoryHierarchyCache
			{
				Schema = schema
			};

			try
			{
				// Build the complete directory hierarchy
				Dictionary<int, List<DirectorySuggestionNode>> allSuggestionsByDepth = new Dictionary<int, List<DirectorySuggestionNode>>();

				// Get all subdirectories recursively
				string[] allDirectories = Directory.GetDirectories(_appSettings.OUTPUT_DIRECTORY, "*", SearchOption.AllDirectories);

				foreach (string dirPath in allDirectories)
				{
					// Get relative part after output directory
					string relativePath = Path.GetRelativePath(_appSettings.OUTPUT_DIRECTORY, dirPath);
					// Separate each directory level by "parts"
					string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

					// Create suggestions for each depth level that corresponds to schema
					for (int depth = 0; depth < Math.Min(pathParts.Length, schema.Count); depth++)
					{
						// Initialise each depths list (0,1,2,etc) if not already
						if (!allSuggestionsByDepth.ContainsKey(depth))
						{
							allSuggestionsByDepth[depth] = new List<DirectorySuggestionNode>();
						}

						
						string pathUpToDepth = string.Join(Path.AltDirectorySeparatorChar.ToString(), pathParts.Take(depth + 1));
						DirectorySuggestionNode suggestion = new DirectorySuggestionNode
						{
							Name = pathParts[depth],
							TokenType = schema[depth],
							Path = pathUpToDepth
						};

						// Check if this suggestion already exists
						DirectorySuggestionNode? existingSuggestion = allSuggestionsByDepth[depth].FirstOrDefault(s => 
							s.Name.Equals(suggestion.Name, StringComparison.OrdinalIgnoreCase) &&
							s.Path.Equals(suggestion.Path, StringComparison.OrdinalIgnoreCase));

						if (existingSuggestion == null)
						{
							allSuggestionsByDepth[depth].Add(suggestion);
						}
					}
				}

				// Sort all suggestions by name using natural sorting (handles numbers properly)
				foreach (KeyValuePair<int, List<DirectorySuggestionNode>> kvp in allSuggestionsByDepth)
				{
					kvp.Value.Sort((a, b) => NaturalSort(a.Name, b.Name));
				}

				result.SuggestionsByDepth = allSuggestionsByDepth;

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

		/// <summary>
		/// Natural sorting that handles numeric portions correctly
		/// This ensures "File (2)" comes before "File (10)" instead of after it (this is the behavior of .Order / .OrderBy)
		/// </summary>
		/// <param name="x">First string to compare</param>
		/// <param name="y">Second string to compare</param>
		/// <returns>Comparison result</returns>
		private static int NaturalSort(string x, string y)
		{
			if (x == null && y == null) return 0;
			if (x == null) return -1;
			if (y == null) return 1;

			int xReadingPos = 0, yReadingPos = 0;
			
			while (xReadingPos < x.Length && yReadingPos < y.Length)
			{
				// Check if both characters are digits
				if (char.IsDigit(x[xReadingPos]) && char.IsDigit(y[yReadingPos]))
				{
					// Extract numeric portions
					var numX = ExtractNumber(x, ref xReadingPos);
					var numY = ExtractNumber(y, ref yReadingPos);
					
					// Compare numbers numerically
					int numComparison = numX.CompareTo(numY);
					if (numComparison != 0) return numComparison;
				}
				else
				{
					// Compare characters case-insensitively
					int charComparison = char.ToUpperInvariant(x[xReadingPos]).CompareTo(char.ToUpperInvariant(y[yReadingPos]));
					if (charComparison != 0) return charComparison;
					
					xReadingPos++;
					yReadingPos++;
				}
			}
			
			// Handle remaining characters
			return x.Length.CompareTo(y.Length);
		}

		/// <summary>
		/// Extract a number from string starting at the given index and moves that index forward
		/// </summary>
		/// <param name="str">Source string</param>
		/// <param name="index">Starting index, will be updated to point after the number</param>
		/// <returns>Extracted number</returns>
		private static long ExtractNumber(string str, ref int index)
		{
			long number = 0;
			while (index < str.Length && char.IsDigit(str[index]))
			{
				int charToInt = str[index] - '0';
				// This shift the new number to the left, we do this since we parse the whole number character by character and store it inside one "long" integer.
				number = number * 10 + charToInt;
				index++;
			}
			return number;
		}
	}
}
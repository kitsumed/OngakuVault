using Microsoft.Extensions.Options;
using OngakuVault.Models;
using System.Text;
using ATL;

namespace OngakuVault.Services
{
	/// <summary>
	/// Service for scanning directories and providing suggestions for artist/album names
	/// </summary>
	public class DirectoryScanService : IDirectoryScanService
	{
		readonly ILogger<DirectoryScanService> _logger;
		readonly AppSettingsModel _appSettings;
		private DirectorySuggestionsModel? _cachedSuggestions;
		private DateTime _cacheTimestamp = DateTime.MinValue;
		private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

		public DirectoryScanService(ILogger<DirectoryScanService> logger, IOptions<AppSettingsModel> appSettings)
		{
			_logger = logger;
			_appSettings = appSettings.Value;
		}

		public bool IsDirectorySuggestionsEnabled()
		{
			return !string.IsNullOrEmpty(_appSettings.OUTPUT_SUB_DIRECTORY_FORMAT) &&
				   (_appSettings.OUTPUT_SUB_DIRECTORY_FORMAT.Contains("|AUDIO_ARTIST|") ||
				    _appSettings.OUTPUT_SUB_DIRECTORY_FORMAT.Contains("|AUDIO_ALBUM|"));
		}

		public DirectorySuggestionsModel? GetDirectorySuggestions(string? artistFilter = null, string? albumFilter = null)
		{
			if (!IsDirectorySuggestionsEnabled())
			{
				return null;
			}

			// Check if we need to refresh cache
			if (_cachedSuggestions == null || DateTime.Now - _cacheTimestamp > _cacheExpiry)
			{
				RefreshCache();
			}

			if (_cachedSuggestions == null)
			{
				return null;
			}

			// Apply filters if provided
			var filteredSuggestions = new DirectorySuggestionsModel();

			// Filter artists
			if (!string.IsNullOrEmpty(artistFilter))
			{
				filteredSuggestions.Artists = _cachedSuggestions.Artists
					.Where(a => a.Contains(artistFilter, StringComparison.OrdinalIgnoreCase))
					.ToList();
			}
			else
			{
				filteredSuggestions.Artists = _cachedSuggestions.Artists.ToList();
			}

			// Filter albums
			foreach (var artistAlbumPair in _cachedSuggestions.Albums)
			{
				var artistName = artistAlbumPair.Key;
				var albums = artistAlbumPair.Value;

				// If artist filter is provided, only include matching artists
				if (!string.IsNullOrEmpty(artistFilter) && 
					!artistName.Contains(artistFilter, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				// Filter albums for this artist
				var filteredAlbums = albums;
				if (!string.IsNullOrEmpty(albumFilter))
				{
					filteredAlbums = albums.Where(album => 
						album.Contains(albumFilter, StringComparison.OrdinalIgnoreCase)).ToList();
				}

				if (filteredAlbums.Any())
				{
					filteredSuggestions.Albums[artistName] = filteredAlbums;
				}
			}

			return filteredSuggestions;
		}

		public void RefreshCache()
		{
			try
			{
				_cachedSuggestions = ScanDirectoryStructure();
				_cacheTimestamp = DateTime.Now;
				_logger.LogInformation("Directory suggestions cache refreshed");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error refreshing directory suggestions cache");
				_cachedSuggestions = null;
			}
		}

		private DirectorySuggestionsModel? ScanDirectoryStructure()
		{
			if (!Directory.Exists(_appSettings.OUTPUT_DIRECTORY))
			{
				_logger.LogInformation("Output directory does not exist: {OutputDirectory}", _appSettings.OUTPUT_DIRECTORY);
				return null;
			}

			var suggestions = new DirectorySuggestionsModel();
			var format = _appSettings.OUTPUT_SUB_DIRECTORY_FORMAT!;

			// Determine the directory structure pattern
			var hasArtist = format.Contains("|AUDIO_ARTIST|");
			var hasAlbum = format.Contains("|AUDIO_ALBUM|");

			if (!hasArtist && !hasAlbum)
			{
				return null;
			}

			try
			{
				// Get all subdirectories in the output directory
				var subdirectories = Directory.GetDirectories(_appSettings.OUTPUT_DIRECTORY, "*", SearchOption.AllDirectories);

				foreach (var dirPath in subdirectories)
				{
					var relativePath = Path.GetRelativePath(_appSettings.OUTPUT_DIRECTORY, dirPath);
					var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

					// Parse based on the format structure
					if (hasArtist && hasAlbum)
					{
						// Format likely contains both artist and album
						// Try to match the pattern |AUDIO_ARTIST|\|AUDIO_ALBUM|
						if (pathParts.Length >= 2)
						{
							var artist = pathParts[0];
							var album = pathParts[1];

							if (!suggestions.Artists.Contains(artist))
							{
								suggestions.Artists.Add(artist);
							}

							if (!suggestions.Albums.ContainsKey(artist))
							{
								suggestions.Albums[artist] = new List<string>();
							}

							if (!suggestions.Albums[artist].Contains(album))
							{
								suggestions.Albums[artist].Add(album);
							}
						}
					}
					else if (hasArtist)
					{
						// Only artist in format
						if (pathParts.Length >= 1)
						{
							var artist = pathParts[0];
							if (!suggestions.Artists.Contains(artist))
							{
								suggestions.Artists.Add(artist);
							}
						}
					}
					else if (hasAlbum)
					{
						// Only album in format
						if (pathParts.Length >= 1)
						{
							var album = pathParts[0];
							if (!suggestions.Albums.ContainsKey("Unknown"))
							{
								suggestions.Albums["Unknown"] = new List<string>();
							}
							if (!suggestions.Albums["Unknown"].Contains(album))
							{
								suggestions.Albums["Unknown"].Add(album);
							}
						}
					}
				}

				// Sort the results
				suggestions.Artists.Sort();
				foreach (var albumList in suggestions.Albums.Values)
				{
					albumList.Sort();
				}

				_logger.LogInformation("Scanned directory structure: found {ArtistCount} artists and {AlbumCount} album entries", 
					suggestions.Artists.Count, suggestions.Albums.Count);

				return suggestions;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error scanning directory structure");
				return null;
			}
		}
	}
}
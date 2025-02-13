namespace OngakuVault.Models
{
	/// <summary>
	/// This class contains the application settings. It is intended to be injected into the DI services using <see cref="IConfiguration"/>.
	/// </summary>
	public class AppSettingsModel
	{
		/// <summary>
		/// The path of the directory where the final audio files will be saved.
		/// Defaults to EXECUTION_DIRECTORY\archived-audios\
		/// </summary>
		public string OUTPUT_DIRECTORY { get; set; } = Path.Combine(AppContext.BaseDirectory, "archived-audios");

		/// <summary>
		/// The path of the directory where files will be downloaded. If null, a temporary folder will to be created by 
		/// <see cref="Services.MediaDownloaderService"/> when required.
		/// WARNING: This directory get removed at the application closure.
		/// </summary>
		public string? TMP_OUTPUT_DIRECTORY { get; set; }

		/// <summary>
		/// The partial path of the directory where the final audio files will be saved. (Sub directory)
		/// Defaults to null, if a value is set, the final output path will be <see cref="OUTPUT_DIRECTORY"/> + <see cref="OUTPUT_SUB_DIRECTORY_FORMAT"/>.
		/// </summary>
		/// <remarks>
		/// This string support values replacing, if the text is placed in between two PIPE (|).
		/// Currently the following values are available: NOW_YEAR, NOW_MONTH, NOW_DAY, AUDIO_ARTIST, AUDIO_ALBUM, AUDIO_YEAR
		/// </remarks>
		public string? OUTPUT_SUB_DIRECTORY_FORMAT { get; set; } = null;

		/// <summary>
		/// Enable or disable swagger api documentation on the website.
		/// Disabled by default.
		/// </summary>
		public bool ENABLE_SWAGGER_DOC { get; set; } = false;

		/// <summary>
		/// Disable the website (static files serving) at website root / of the wwwroot directory
		/// Disabled by default. (Website is enabled)
		/// </summary>
		public bool DISABLE_WEBSITE { get; set; } = false;

		/// <summary>
		/// If http connections should be redirected to the configured https port
		/// Disabled by default.
		/// </summary>
		public bool ENFORCE_HTTPS { get; set; } = false;

		/// <summary>
		/// If enabled, all existing non-standard fields will be removed from the audio file metadata.
		/// We uses the ATL library for metadata, for more informations on what is a non-standard field
		/// refer to https://github.com/Zeugma440/atldotnet/wiki/Focus-on-non-standard-fields
		/// </summary>
		public bool CLEAR_METADATA_NONSTANDARD_FIELDS { get; set; } = false;

		/// <summary>
		/// Number of allowed scrapper processes (yt-dlp) that can run at the same time.
		/// 8 by default, minimum 1, limited to 100.
		/// </summary>
		public int PARALLEL_SCRAPER_PROC
		{
			get => _parallelScraperProc;
			set => _parallelScraperProc = (value < 1) ? 1 : (value > 100) ? 100 : value;
		}
		private int _parallelScraperProc = 8;

		/// <summary>
		/// Number of allowed download jobs that can run at the same time.
		/// 4 by default, minimum 1, limited to PARALLEL_SCRAPER_PROC value
		/// </summary>
		public int PARALLEL_JOBS
		{
			get => _parallelJobs;
			set => _parallelJobs = (value < 1) ? 1 : (value > PARALLEL_SCRAPER_PROC) ? PARALLEL_SCRAPER_PROC : value;
		}
		private int _parallelJobs = 4;

		/// <summary>
		/// List of allowed origins for REST and Websocket connections.
		/// Origins are separated by PIPE (|). Ex: (https://example.com|https://example2.com)
		/// Empty by default, allowing all origins.
		/// </summary>
		public string? OVERWRITE_CORS_ORIGIN { get; set; }
		/// <remarks>
		/// Array version of <see cref="OVERWRITE_CORS_ORIGIN"/>
		/// </remarks>
		public string[]? OVERWRITE_CORS_ORIGIN_ARRAY => OVERWRITE_CORS_ORIGIN?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? null;


		/// <summary>
		/// List, in descending priority order, of allowed lyrics/subtitles
		/// that can be fetched and returned by the server in a additional media info
		/// request (by <see cref="Controllers.MediaController"/>).
		/// All values are separated by PIPE (|). Ex: (https://example.com|https://example2.com)
		/// Empty by default, not fetching any lyrics / subtitles.
		/// </summary>
		/// <remarks>
		/// Uses IETF language tags since the scraper (yt-dlp) uses them.
		/// A value of "en" would match "en" and everything starting by "en-".
		/// A value of "en-US" would match "en-US" and everything starting by "en-US-".
		/// </remarks>
		public string? LYRICS_LANGUAGE_PRIORITY { get; set; }
		/// <remarks>
		/// Array version of <see cref="LYRICS_LANGUAGE_PRIORITY"/>
		/// </remarks>
		public string[]? LYRICS_LANGUAGE_PRIORITY_ARRAY => LYRICS_LANGUAGE_PRIORITY?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? null;
	}
}

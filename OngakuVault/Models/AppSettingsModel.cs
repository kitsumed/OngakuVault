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
		/// Number of allowed scrapper processes (yt-dlp) that can run at the same time.
		/// 8 by default, minimum 1, limited to 100.
		/// </summary>
		public int PARALLEL_SCRAPPER_PROC
		{
			get => _parallelScrapperProc;
			set => _parallelScrapperProc = (value < 1) ? 1 : (value > 100) ? 100 : value;
		}
		private int _parallelScrapperProc = 8;

		/// <summary>
		/// Number of allowed download jobs that can run at the same time.
		/// 4 by default, minimum 1, limited to PARALLEL_SCRAPPER_PROC value
		/// </summary>
		public int PARALLEL_JOBS
		{
			get => _parallelJobs;
			set => _parallelJobs = (value < 1) ? 1 : (value > PARALLEL_SCRAPPER_PROC) ? PARALLEL_SCRAPPER_PROC : value;
		}
		private int _parallelJobs = 4;

		/// <summary>
		/// List of allowed origins for REST & Websocket connections.
		/// Origins are separated by PIPE (|). Ex: (https://example.com|https://example2.com)
		/// Empty by default, allowing all origins.
		/// </summary>
		public string? OVERWRITE_CORS_ORIGIN { get; set; }


		/// <summary>
		/// Get <see cref="OVERWRITE_CORS_ORIGIN"/> values as a array (PIPE (|) used for separation)
		/// </summary>
		/// <returns>A array or null if empty</returns>
		public string[]? Get_OVERWRITE_CORS_ORIGIN_AsArray()
		{
			return OVERWRITE_CORS_ORIGIN?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? null;
		}
	}
}

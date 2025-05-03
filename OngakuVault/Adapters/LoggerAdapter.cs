using ATL.Logging;

namespace OngakuVault.Adapters
{
	/// <summary>
	/// This class manages the redirection of logging from third-party assembly to this application logging system.
	/// </summary>
	public class LoggerAdapter : ILogDevice
    {
        /// <summary>
        /// ASP.NET Logger Factory
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

		// THIRD-PARTY TO ASP.NET LOGGER
		/// <summary>
		/// ATL to ASP.NET Logging
		/// </summary>
		private readonly ILogger<ATL.Logging.Log> _atlLogger;
		private const string _atlLoggerMessage = "'{message}'. LOCATION : '{location}'";

		public LoggerAdapter(ILoggerFactory loggerFactory)
        {
            /// Define values
            _loggerFactory = loggerFactory;
            /// Create third-party to ASP.NET loggers
			_atlLogger = _loggerFactory.CreateLogger<ATL.Logging.Log>();
			/// Register third-party loggers
			// Register SubtitlesParserV2
			SubtitlesParserV2.Logger.LoggerManager.LoggerFactory = _loggerFactory;
			// Register ATL loggers
			Log atlExternalLogger = new ATL.Logging.Log();
			LogDelegator.SetLog(ref atlExternalLogger);
			atlExternalLogger.Register(this);
		}

		// Redirect ATL Logs to ASP.NET Logger
		public void DoLog(Log.LogItem anItem)
		{
			switch (anItem.Level)
			{
				case Log.LV_INFO:
					_atlLogger.LogInformation(_atlLoggerMessage, anItem.Message, anItem.Location);
					break;
				case Log.LV_WARNING:
					_atlLogger.LogWarning(_atlLoggerMessage, anItem.Message, anItem.Location);
					break;
				case Log.LV_DEBUG:
					_atlLogger.LogDebug(_atlLoggerMessage, anItem.Message, anItem.Location);
					break;
				case Log.LV_ERROR:
					_atlLogger.LogError(_atlLoggerMessage, anItem.Message, anItem.Location);
					break;
			}
		}
	}
}

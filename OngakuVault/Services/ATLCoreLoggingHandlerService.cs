using ATL.Logging;

namespace OngakuVault.Services
{
	/// <summary>
	/// This class implement the <see cref="ILogDevice"></see> interface,
	/// It manages the logging system of the <see cref="ATL"/> library by redirecting them to our application logging system 
	/// (<see cref="ILogger"/>).
	/// </summary>
	/// <remarks>
	/// Interaction by others class is possible due to public voids required by ATL interface, but <strong>NOT RECOMMENDED</strong>.
	/// </remarks>
	public class ATLCoreLoggingHandlerService : ILogDevice
	{
		/// <summary>
		/// ASP.NET Logging
		/// </summary>
		private readonly ILogger<ATLCoreLoggingHandlerService> _logger;

		/// <summary>
		/// ATL Logging
		/// </summary>
		private readonly Log _atlCoreLogger = new Log();

		/// <summary>
		/// Message Template
		/// </summary>
		private readonly string logMessageFormat = "ATL-CORE LOG MESSAGE: '{message}'. LOCATION : '{location}'.";
        public ATLCoreLoggingHandlerService(ILogger<ATLCoreLoggingHandlerService> logger)
        {
			// Init ASP.NET Logging
            _logger = logger;
			// Register our LOG object inside ATL to allow sending logs using atl logging
			LogDelegator.SetLog(ref _atlCoreLogger);
			// Register the current class (object) to be called when ATL sends logs
			_atlCoreLogger.Register(this);
			//_atlCoreLogger.Info("Log sended from ATL logging system")
		}

		// Called by ATL when logs are received, we redirect them to our logging system
		public void DoLog(Log.LogItem logItem)
		{
			switch (logItem.Level) 
			{
				case Log.LV_INFO:
					_logger.LogInformation(logMessageFormat, logItem.Message, logItem.Location);
					break;
				case Log.LV_WARNING:
					_logger.LogWarning(logMessageFormat, logItem.Message, logItem.Location);
					break;
				case Log.LV_DEBUG:
					_logger.LogDebug(logMessageFormat, logItem.Message, logItem.Location);
					break;
				case Log.LV_ERROR:
					_logger.LogError(logMessageFormat, logItem.Message, logItem.Location);
					break;
			}
		}
	}
}

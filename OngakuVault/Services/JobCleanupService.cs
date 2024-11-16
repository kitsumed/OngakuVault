namespace OngakuVault.Services
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Hosting;
	using Microsoft.Extensions.Logging;


	public class JobCleanupService : BackgroundService
	{
		private readonly ILogger<JobCleanupService> _logger;
		private readonly IJobService _jobService;
		private Timer _timer;
		private readonly TimeSpan _dueTime = TimeSpan.Zero;  // Start immediately
		private readonly TimeSpan _everyTime = TimeSpan.FromMinutes(30); // Run every 30 minutes

		public JobCleanupService(ILogger<JobCleanupService> logger, IJobService jobService)
		{
			_logger = logger;
			_jobService = jobService;
		}

		// Override ExecuteAsync to start the timer
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_timer = new Timer(StartCleanup, null, _dueTime, _everyTime);

			// Return a completed task, the timer run in another thread
			return Task.CompletedTask;
		}

		// This method will be called every 30 minutes
		private void StartCleanup(object state)
		{
			// Remove jobs older than 30 minute (the _everyTime)
			_jobService.OldJobsCleanup(_everyTime.Minutes);
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("JobCleanupService is stopping...");
			// Stop the timer and any pending operations
			_timer?.Change(Timeout.Infinite, 0); // Stop the timer
			return base.StopAsync(cancellationToken);
		}
	}
}

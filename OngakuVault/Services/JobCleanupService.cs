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

		private readonly TimeSpan _runAtEvery = TimeSpan.FromMinutes(30); // Run every 30 minutes

		public JobCleanupService(ILogger<JobCleanupService> logger, IJobService jobService)
		{
			_logger = logger;
			_jobService = jobService;
		}

		/// <summary>
		/// Called when the application start and load this service
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			using PeriodicTimer periodicTimer = new PeriodicTimer(_runAtEvery);
			while (!stoppingToken.IsCancellationRequested && await periodicTimer.WaitForNextTickAsync(stoppingToken))
			{
				// Clear eligible jobs that are older than _runAtEvery
				_jobService.OldJobsCleanup(_runAtEvery.TotalMinutes);
			}
		}
	}
}

﻿using OngakuVault.Models;
using System.Collections.Concurrent;
using YoutubeDLSharp;
using static OngakuVault.Helpers.ScraperErrorOutputHelper;

namespace OngakuVault.Services
{
	public interface IJobService
	{
		/// <summary>
		/// Create and configure a job that can then be added to the execution queue
		/// </summary>
		/// <param name="jobRESTCreationDataModel">A <see cref="JobRESTCreationModel"/> containing the job additional info used during execution</param>
		/// <returns>A JobModel</returns>
		JobModel CreateJob(JobRESTCreationModel jobRESTCreationDataModel);

		/// <summary>
		/// Add a new job inside the list and add it to the execution queue
		/// </summary>
		/// <param name="job">The current job informations</param>
		/// <returns>True if the job was added, false if a job with the same ID is already in the list (probably the same job)</returns>
		bool TryAddJobToQueue(JobModel jobModel);

		/// <summary>
		/// Get a job (JobModel) from it's ID
		/// </summary>
		/// <param name="ID">The Job ID</param>
		/// <param name="job">The job data</param>
		/// <returns>True if found, else false</returns>
		bool TryGetJobByID(string ID, out JobModel? job);

		/// <summary>
		/// Get all Jobs in the list
		/// </summary>
		/// <returns>A ICollection of all JobModels</returns>
		ICollection<JobModel> GetJobs();
	}

	/// <summary>
	/// This class implements the <see cref="IJobService"/> interface and provides functionality
	/// to manage jobs. It allows executing up to 4 async methods in parallel as "jobs".
	/// </summary>
	public class JobService : IJobService
	{
		private readonly ILogger<JobService> _logger;
		private readonly IMediaDownloaderService _mediaDownloaderService;
		private readonly IWebSocketManagerService _websocketManagerService;

		/// <summary>
		/// List of Jobs managed by the JobService (waiting for execution, running, completed, etc)
		/// </summary>
		private readonly ConcurrentDictionary<string, JobModel> Jobs = new ConcurrentDictionary<string, JobModel>();

		/// <summary>
		/// JobsSemaphore to allow 4 async thread (jobs) at the same time
		/// </summary>
		private readonly SemaphoreSlim JobsSemaphore = new SemaphoreSlim(4, 4);

		/// <summary>
		/// Run jobs cleanup timer at every 30 minutes
		/// </summary>
		private readonly TimeSpan _runCleanupAtEvery = TimeSpan.FromMinutes(30);

		/// <summary>
		/// The path of the directory where the final files will be saved.
		/// Defaults to EXECUTION_DIRECTORY\archived-audios\ if no environment variable are set.
		/// </summary>
		private readonly string OutputPath = Environment.GetEnvironmentVariable("OUTPUT_DIRECTORY") ?? Path.Combine(AppContext.BaseDirectory, "archived-audios");


		public JobService(ILogger<JobService> logger, IMediaDownloaderService mediaDownloaderService, IWebSocketManagerService webSocketManagerService)
        {
            _logger = logger;
			_mediaDownloaderService = mediaDownloaderService;
			_websocketManagerService = webSocketManagerService;
			// Init jobs cleanup async task
			Task.Run(async () =>
			{
				using PeriodicTimer periodicTimer = new PeriodicTimer(_runCleanupAtEvery);
				while (await periodicTimer.WaitForNextTickAsync())
				{
					// Clear eligible jobs that are older than _runCleanupAtEvery
					OldJobsCleanup(_runCleanupAtEvery.TotalMinutes);
				}
			});
		}

		public JobModel CreateJob(JobRESTCreationModel jobRESTCreationDataModel)
		{
			return new JobModel(_websocketManagerService, jobRESTCreationDataModel);
		}

		public bool TryAddJobToQueue(JobModel jobModel)
		{
			bool result = Jobs.TryAdd(jobModel.ID, jobModel);
			// If the Job was added to the list, start a async thread with the JobModel
			if (result) 
			{
				_logger.LogInformation("Job ID: '{ID}' has been added to the execution queue. (Queued)", jobModel.ID);
				jobModel.ReportStatus(JobStatus.Queued); // Report to all websocket connection that the job is now in the execution queue
				AddJobToExecutionQueueAsync(jobModel.ID); // Plan the job for future execution
			} 
			return result;
		}

		public bool TryGetJobByID(string ID, out JobModel? job)
		{
			// Try to get the job, return success boolean, & out JobModel
			return Jobs.TryGetValue(ID, out job);
		}

		public ICollection<JobModel> GetJobs()
		{
			return Jobs.Values;
		}

		// [PRIVATE METHODS]

		/// <summary>
		/// Dispose and remove jobs in the jobs list if they completed/failed execution and are older than a specific duration.
		/// </summary>
		/// <param name="totalMinutes">The minimum number of minutes</param>
		private void OldJobsCleanup(double totalMinutes)
		{
			DateTime dateTimeNow = DateTime.Now;
            foreach (JobModel jobModel in Jobs.Values)
            {
				// Ensure that the job is not currently running or waiting to be ran
				if (jobModel.Status != JobStatus.Running && jobModel.Status != JobStatus.Queued) 
				{
					// If the numbers of minutes between the job creation and NOW is bigger than totalMinutes
					if (dateTimeNow.Subtract(jobModel.CreationDate).TotalMinutes >= totalMinutes) 
					{
						// Free the old job from the list
						if (Jobs.TryRemove(jobModel.ID, out _))
						{
							jobModel?.Dispose();
						}
					};
				}
			}
        }

		/// <summary>
		/// Start a new thread for a Job and wait for JobsSemaphore before processing
		/// </summary>
		/// <param name="jobID">The job ID in the <see cref="Jobs"/> list.</param>
		private async void AddJobToExecutionQueueAsync(string jobID)
		{
			// Allow job failed checks to detect if a the job was set to failed state manually or because of a thrown exception.
			bool didJobFailedDueToThrownException = false;
			try
			{
				// Wait for a new place in the execution queue
				await JobsSemaphore.WaitAsync(Jobs[jobID].CancellationTokenSource.Token);
				Jobs[jobID].ReportStatus(JobStatus.Running, "Starting job...", 0); // Update job status to Running
				_logger.LogInformation("Job ID: '{ID}' is now running.", jobID);
				try
				{
					// Create a progress update for the yt-dlp scraper. Represent 80% of the Job progress
					IProgress<DownloadProgress> downloadProgress = new Progress<DownloadProgress>(progress =>
					{
						// Progress % contributions in total (0-80)
						const int preProcessingPercentage = 5; // Pre-processing is 5%
						const int downloadingPercentage = 70; // Downloading is 70%
						const int postProcessingPercentage = 5; // Post-processing is 5%
						int? newProgress = null;
						string? newProgressTaskName = null;

						if (progress.State == DownloadState.PreProcessing)
						{
							newProgressTaskName = $"Scraper is preprocessing...";
							// 0 to 5 when scraper return pre-processing at 100%.
							newProgress = (int)(preProcessingPercentage * progress.Progress);
						}
						else if (progress.State == DownloadState.Downloading)
						{
							newProgressTaskName = $"Scraper is downloading media... ETA: {progress.ETA ?? "Unknown"}";
							// 5 to 75 when scraper return downloading at 100%.
							newProgress = preProcessingPercentage + (int)(downloadingPercentage * progress.Progress);
						}
						else if (progress.State == DownloadState.PostProcessing)
						{
							newProgressTaskName = $"Scraper is postprocessing...";
							newProgress = preProcessingPercentage + downloadingPercentage + (int)(postProcessingPercentage * progress.Progress);
						}

						if (newProgress != null)
						{
							// Ensure newProgress is bigger than current job progress before updating
							if (newProgress > Jobs[jobID].Progress)
							{
								Jobs[jobID].ReportStatus(JobStatus.Running, newProgressTaskName, newProgress);
							}
						}
					});

					FileInfo? downloadedFileInfo = await _mediaDownloaderService.DownloadAudio(Jobs[jobID].Data.MediaUrl, Jobs[jobID].Configuration.FinalAudioFormat, Jobs[jobID].CancellationTokenSource.Token, downloadProgress);
					if (downloadedFileInfo != null)
					{
						// Ensure the final output directory exists
						Directory.CreateDirectory(OutputPath);
						// Set file permission for linux based systems
						if (OperatingSystem.IsLinux()) downloadedFileInfo.UnixFileMode = (UnixFileMode.OtherRead | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.UserRead | UnixFileMode.UserWrite);
						downloadedFileInfo.MoveTo(Path.Combine(OutputPath, downloadedFileInfo.Name));
					}
					else 
					{
						_logger.LogWarning("Job ID: '{ID}'. The scraper did not return the downloaded file path. Error could be due to the scraper not finding any formats/media on the target webpage. (the formats key is empty?)", jobID);
						Jobs[jobID].ReportStatus(JobStatus.Failed, $"Could not locate downloaded file. Did the scraper found any formats? Is webpage supported?", 100);
					}
				}
				// Ignore canceledException when it's thrown due to the cancel signal on our cancellationToken
				catch (OperationCanceledException) when (Jobs[jobID].CancellationTokenSource.IsCancellationRequested) { }
				catch (Exception ex)
				{
					didJobFailedDueToThrownException = true;
					// If it's a error related to the scraper (yt-dlp)
					if (ex is ProcessedScraperErrorOutputException)
					{
						ProcessedScraperErrorOutputException processedEx = (ProcessedScraperErrorOutputException)ex;
						if (processedEx.IsKnownError)
						{
							_logger.LogWarning("Known scraper error occurred during during execution of Job ID : '{ID}'. Error: {message}", jobID, ex.Message);
							Jobs[jobID].ReportStatus(JobStatus.Failed, $"Known scraper error occurred: {ex.Message}", 100);
						}
						else 
						{
							_logger.LogError("An unexpected scraper error occurred during the execution of Job ID : '{ID}'. Error: {message}", jobID, ex.Message);
							Jobs[jobID].ReportStatus(JobStatus.Failed, "An unexpected scraper error occurred", 100);
						}
					}
					else
					{
						_logger.LogError(ex, "An unexpected error occurred during the execution of Job ID : '{ID}'. Error: {message}'", jobID, ex.Message);
						Jobs[jobID].ReportStatus(JobStatus.Failed, "An unexpected error occurred", 100);
					}
				}
				finally
				{
					// Handle final status when the jobs exit it's execution
					if (Jobs[jobID].CancellationTokenSource.IsCancellationRequested) // Cancellation token triggered, job cancelled
					{
						_logger.LogInformation("Job ID: '{ID}' was cancelled during execution.", jobID);
						Jobs[jobID].ReportStatus(JobStatus.Cancelled, "Cancellation was requested", 100);
					}
					else if (Jobs[jobID].Status == JobStatus.Failed) 
					{
						if (didJobFailedDueToThrownException) 
						{
							_logger.LogInformation("Job ID: '{ID}' failed during execution due to a exception.", jobID);
						} else _logger.LogInformation("Job ID: '{ID}' was manually set to failed state during execution.", jobID);

					}
					else if (Jobs[jobID].Status == JobStatus.Running && !Jobs[jobID].CancellationTokenSource.IsCancellationRequested)
					{
						_logger.LogInformation("Job ID: '{ID}' finished execution.", Jobs[jobID].ID);
						TimeSpan jobDuration = DateTime.Now - Jobs[jobID].CreationDate;
						Jobs[jobID].ReportStatus(JobStatus.Completed, $"Completed in {jobDuration.Hours}h:{jobDuration.Minutes}m:{jobDuration.Seconds}s", 100);
					}
					// Release a place inside the semaphore to allow a new job to start
					JobsSemaphore.Release();
				}
			}
			catch (OperationCanceledException)
			{
				// If the CancellationToken is triggered before execution, change the job status to cancelled.
				Jobs[jobID].ReportStatus(JobStatus.Cancelled, "Cancellation was requested", 100);
				_logger.LogInformation("Job ID: '{ID}' was cancelled before execution.", jobID);
			}
		}
	}
}

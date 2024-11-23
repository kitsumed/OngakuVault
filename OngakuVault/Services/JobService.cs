using OngakuVault.Models;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using YoutubeDLSharp;

namespace OngakuVault.Services
{
	public interface IJobService
	{
		/// <summary>
		/// Add a new job inside the list & add it to the execution queue
		/// </summary>
		/// <param name="job">The JobModel data</param>
		/// <returns>True if the job was added, false if a job with the same ID already exist (should never happen)</returns>
		bool TryAddJobToQueue(JobModel jobModel);
		/// <summary>
		/// Get a JobModel data from it's ID
		/// </summary>
		/// <param name="ID">The Job ID</param>
		JobModel? TryGetJob(string ID);
		/// <summary>
		/// Get all Jobs in the list
		/// </summary>
		/// <returns>A ICollection of all JobModels</returns>
		ICollection<JobModel> GetJobs();
		/// <summary>
		/// Update a JobModel to a newer JobModel
		/// </summary>
		/// <param name="ID">The job ID</param>
		/// <param name="updatedJobModel">The new JobModel</param>
		/// <returns>True if updated, false if JobID not found</returns>
		bool TryUpdateJob(string ID, JobModel updatedJobModel);
		/// <summary>
		/// Cleanup jobs that have finished, failed or been cancelled. 
		/// This method removes jobs by verifying the creation date for a specified duration in minutes.
		/// </summary>
		/// <param name="totalMinutes">The maximum number of minutes before removing a job</param>
		void OldJobsCleanup(double totalMinutes);
	}
	public class JobService : IJobService
	{
		private readonly ILogger<JobService> _logger;

		/// <summary>
		/// YoutubeDownloadSharp (yt-dlp wrapper). Allow 4 parallel download.
		/// </summary>
		private readonly YoutubeDL _mediaDownloader;

		/// <summary>
		/// List of created Jobs
		/// </summary>
		private readonly ConcurrentDictionary<string, JobModel> Jobs = new ConcurrentDictionary<string, JobModel>();

		/// <summary>
		/// JobsSemaphore to allow 4 async thread (jobs) at the same time
		/// </summary>
		private readonly SemaphoreSlim JobsSemaphore = new SemaphoreSlim(4, 4);

		/// <summary>
		/// The directory in which the OngakuVault executable is located
		/// </summary>
		private readonly string ExecutableDirectory = AppContext.BaseDirectory;

		public JobService(ILogger<JobService> logger, YoutubeDL mediaDownloader)
        {
            _logger = logger;
			_mediaDownloader = mediaDownloader;

			// Set the paths for yt-dlp and FFmpeg executables for linux by default
			_mediaDownloader.YoutubeDLPath = Path.Combine(ExecutableDirectory, "yt-dlp");
			_mediaDownloader.FFmpegPath = Path.Combine(ExecutableDirectory, "ffmpeg");

			// If OS is Windows, append ".exe" to the executables
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_mediaDownloader.YoutubeDLPath += ".exe";
				_mediaDownloader.FFmpegPath += ".exe";
			}

			// Set the download path
			_mediaDownloader.OutputFolder = Path.Combine(ExecutableDirectory, "tmp_downloads");
			_logger.LogInformation("JobService configured MediaDownloader external binaries. yt-dlp to '{YoutubeDLPath}' and FFmpeg to '{FFmpegPath}'.", _mediaDownloader.YoutubeDLPath, _mediaDownloader.FFmpegPath);
			_logger.LogInformation("Current yt-dlp version is {version}.", _mediaDownloader.Version);
        }

		public bool TryAddJobToQueue(JobModel jobModel)
		{
			bool result = Jobs.TryAdd(jobModel.ID, jobModel);
			// If the Job was added to the list, start a async thread with the JobModel
			if (result) 
			{
				_logger.LogInformation("Job ID: {ID} has been added to the execution queue.", jobModel.ID);
				Jobs[jobModel.ID].Status = JobStatus.Queued; // Update the job status to Queued
				StartJobAsync(Jobs[jobModel.ID]);
			} 
			return result;
		}

		// Retrieves a job by its ID
		public JobModel? TryGetJob(string ID)
		{
			// Try to get the job, return null if not found
			Jobs.TryGetValue(ID, out JobModel? job);
			return job;
		}

		public ICollection<JobModel> GetJobs()
		{
			return Jobs.Values;
		}

		// Updates a job if it exists; otherwise, returns false
		public bool TryUpdateJob(string ID, JobModel updatedJob)
		{
			if (Jobs.ContainsKey(ID))
			{
				// Update the job (this replaces the existing job with the updated one)
				Jobs[ID] = updatedJob;
				return true;
			}
			return false; // Job not found
		}

		// Cleans up old jobs
		public void OldJobsCleanup(double totalMinutes)
		{
			DateTime dateTimeNow = DateTime.Now;
			int cleanedJobs = 0;
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
							cleanedJobs++;
						}
					};
				}

			}
			_logger.LogDebug("Removed {cleanedJobs} ended jobs from the jobs list. Minutes threshold: {totalMinutes}", cleanedJobs, totalMinutes);
        }

		/// <summary>
		/// Start a new Job thread and wait for JobsSemaphore before processing
		/// </summary>
		/// <param name="jobModel">The data of the job</param>
		private async void StartJobAsync(JobModel jobModel)
		{
			try
			{
				await JobsSemaphore.WaitAsync(jobModel.CancellationTokenSource.Token);
				Jobs[jobModel.ID].Status = JobStatus.Running; // Update to job status to Running
				_logger.LogDebug("Job ID: {ID} changed status from 'Queuing' to 'Running'.", jobModel.ID);
				try
				{
					for (int i = 0; i < 20; i++)
					{
						if (jobModel.CancellationTokenSource.IsCancellationRequested) break;
						Console.WriteLine("Inside semaphore " + i.ToString());
						await Task.Delay(4000);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, ex.Message);
					Jobs[jobModel.ID].Status = JobStatus.Failed;
				}
				finally
				{
					// Handle final status when the jobs exit it's execution
					if (jobModel.CancellationTokenSource.IsCancellationRequested) // Cancellation token triggered, job cancelled
					{
						_logger.LogInformation("Job ID: {ID} was cancelled during execution.", jobModel.ID);
						Jobs[jobModel.ID].Status = JobStatus.Cancelled;
					}
					else if (Jobs[jobModel.ID].Status == JobStatus.Failed) 
					{
						_logger.LogInformation("Job ID: {ID} failed during execution.", jobModel.ID);
					}
					else if (Jobs[jobModel.ID].Status == JobStatus.Running && !jobModel.CancellationTokenSource.IsCancellationRequested)
					{
						_logger.LogInformation("Job ID: {ID} finished execution.", jobModel.ID);
						Jobs[jobModel.ID].Status = JobStatus.Completed;
					}
					// Release a place inside the semaphore to allow a new job to start
					JobsSemaphore.Release();
				}
			}
			catch (OperationCanceledException)
			{
				// If the CancellationToken is triggered before execution, change the job status to cancelled.
				Jobs[jobModel.ID].Status = JobStatus.Cancelled;
				_logger.LogInformation("Job ID: {ID} was cancelled before execution.", jobModel.ID);
			}
		}
	}
}

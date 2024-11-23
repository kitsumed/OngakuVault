using OngakuVault.Models;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using YoutubeDLSharp;

namespace OngakuVault.Services
{
	public interface IJobService<T>
	{
		/// <summary>
		/// Add a new job inside the list & add it to the execution queue
		/// </summary>
		/// <param name="job">The current job informations</param>
		/// <returns>True if the job was added, false if a job with the same ID already exist (should never happen)</returns>
		bool TryAddJobToQueue(JobModel<T> jobModel);
		/// <summary>
		/// Get a JobModel data from it's ID
		/// </summary>
		/// <param name="ID">The Job ID</param>
		JobModel<T>? TryGetJob(string ID);
		/// <summary>
		/// Get all Jobs in the list
		/// </summary>
		/// <returns>A ICollection of all JobModels</returns>
		ICollection<JobModel<T>> GetJobs();
		/// <summary>
		/// This delegate method is called when the Job start running.
		/// The method called need the same arguments as this delegate.
		/// </summary>
		/// <param name="jobMethodAdditionalInfo">Additional informations stocked inside the job <see cref="JobModel{T}.Data"/></param>
		/// <param name="cancellationToken">The cancellation token of the job</param>
		/// <returns></returns>
		public delegate Task ExecuteJob(T jobMethodAdditionalInfo, CancellationToken? jobCancellationToken);
		/// <summary>
		/// Cleanup jobs that have finished, failed or been cancelled. 
		/// This method removes jobs by verifying the creation date for a specified duration in minutes.
		/// </summary>
		/// <param name="totalMinutes">The maximum number of minutes before removing a job</param>
		void OldJobsCleanup(double totalMinutes);
	}

	/// <summary>
	/// This class implements the <see cref="IJobService{T}"/> interface and provides functionality
	/// to manage jobs. It allows executing up to 4 async methods in parallel as "jobs".
	/// </summary>
	/// <typeparam name="T">The value type of the additional data stocked inside <see cref="JobModel{T}.Data"/></typeparam>
	public class JobService<T> : IJobService<T>
	{
		private readonly ILogger<JobService<T>> _logger;

		/// <summary>
		/// List of created Jobs
		/// </summary>
		private readonly ConcurrentDictionary<string, JobModel<T>> Jobs = new ConcurrentDictionary<string, JobModel<T>>();

		/// <summary>
		/// JobsSemaphore to allow 4 async thread (jobs) at the same time
		/// </summary>
		private readonly SemaphoreSlim JobsSemaphore = new SemaphoreSlim(4, 4);

		/// <summary>
		/// Run jobs cleanup timer at every 30 minutes
		/// </summary>
		private readonly TimeSpan _runCleanupAtEvery = TimeSpan.FromMinutes(30);

		public JobService(ILogger<JobService<T>> logger)
        {
            _logger = logger;
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

		public bool TryAddJobToQueue(JobModel<T> jobModel)
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
		public JobModel<T>? TryGetJob(string ID)
		{
			// Try to get the job, return null if not found
			Jobs.TryGetValue(ID, out JobModel<T>? job);
			return job;
		}

		public ICollection<JobModel<T>> GetJobs()
		{
			return Jobs.Values;
		}

		// Cleans up old jobs
		public void OldJobsCleanup(double totalMinutes)
		{
			DateTime dateTimeNow = DateTime.Now;
			int cleanedJobs = 0;
            foreach (JobModel<T> jobModel in Jobs.Values)
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
		/// <param name="jobModel">The informations of the job</param>
		private async void StartJobAsync(JobModel<T> jobModel)
		{
			try
			{
				await JobsSemaphore.WaitAsync(jobModel.CancellationTokenSource.Token);
				Jobs[jobModel.ID].Status = JobStatus.Running; // Update to job status to Running
				_logger.LogDebug("Job ID: {ID} changed status from 'Queuing' to 'Running'.", jobModel.ID);
				try
				{
					// Execute the method selectionned at the creation of the jobModel & send job additional informations
					await jobModel.ExecuteJobMethod(Jobs[jobModel.ID].Data, Jobs[jobModel.ID].CancellationTokenSource.Token);
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

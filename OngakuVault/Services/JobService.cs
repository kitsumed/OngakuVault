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
		/// Add a new job inside the list and add it to the execution queue
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
		/// This delegate method is the format of supported methods that can be called when the Job start running.
		/// The method called need to accept the same arguments as this delegate.
		/// </summary>
		/// <param name="jobMethodAdditionalInfo">Additional informations stocked inside the job <see cref="JobModel{T}.Data"/></param>
		/// <param name="cancellationToken">The cancellation token of the job</param>
		/// <returns></returns>
		public delegate Task ExecuteJob(T jobMethodAdditionalInfo, CancellationToken? jobCancellationToken);
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
				AddJobToExecutionQueue(jobModel.ID);
			} 
			return result;
		}

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


		/// <summary>
		/// Dispose and remove from the jobs list ended jobs older than a specific duration.
		/// </summary>
		/// <param name="totalMinutes">The minimum number of minutes</param>
		private void OldJobsCleanup(double totalMinutes)
		{
			DateTime dateTimeNow = DateTime.Now;
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
						}
					};
				}
			}
        }

		/// <summary>
		/// Start a new Job thread and wait for JobsSemaphore before processing
		/// </summary>
		/// <param name="jobID">The job ID in the <see cref="Jobs"/> list.</param>
		private async void AddJobToExecutionQueue(string jobID)
		{
			try
			{
				await JobsSemaphore.WaitAsync(Jobs[jobID].CancellationTokenSource.Token);
				Jobs[jobID].Status = JobStatus.Running; // Update to job status to Running
				_logger.LogDebug("Job ID: {ID} changed status from 'Queuing' to 'Running'.", jobID);
				try
				{
					// Execute the method selectionned at the creation of the jobModel & send job additional informations
					await Jobs[jobID].ExecuteJob(Jobs[jobID].Data, Jobs[jobID].CancellationTokenSource.Token);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, ex.Message);
					Jobs[jobID].Status = JobStatus.Failed;
				}
				finally
				{
					// Handle final status when the jobs exit it's execution
					if (Jobs[jobID].CancellationTokenSource.IsCancellationRequested) // Cancellation token triggered, job cancelled
					{
						_logger.LogInformation("Job ID: {ID} was cancelled during execution.", jobID);
						Jobs[jobID].Status = JobStatus.Cancelled;
					}
					else if (Jobs[jobID].Status == JobStatus.Failed) 
					{
						_logger.LogInformation("Job ID: {ID} failed during execution.", jobID);
					}
					else if (Jobs[jobID].Status == JobStatus.Running && !Jobs[jobID].CancellationTokenSource.IsCancellationRequested)
					{
						_logger.LogInformation("Job ID: {ID} finished execution.", Jobs[jobID].ID);
						Jobs[jobID].Status = JobStatus.Completed;
					}
					// Release a place inside the semaphore to allow a new job to start
					JobsSemaphore.Release();
				}
			}
			catch (OperationCanceledException)
			{
				// If the CancellationToken is triggered before execution, change the job status to cancelled.
				Jobs[jobID].Status = JobStatus.Cancelled;
				_logger.LogInformation("Job ID: {ID} was cancelled before execution.", jobID);
			}
		}
	}
}

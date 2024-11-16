using OngakuVault.Controllers;
using OngakuVault.Models;
using System.Collections.Concurrent;

namespace OngakuVault.Services
{
	public interface IJobService
	{
		/// <summary>
		/// Add a new job inside the list & add it to the execution queue
		/// </summary>
		/// <param name="job">The JobModel data</param>
		/// <returns>True if the job was added, false if a job with the same ID already exist</returns>
		bool TryAddJob(JobModel jobModel);
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
		/// Remove a Job from the list
		/// </summary>
		/// <param name="ID">The Job ID</param>
		/// <returns>True if it was removed, else false</returns>
		bool TryRemoveJob(string ID);
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
		/// List of created Jobs
		/// </summary>
		private readonly ConcurrentDictionary<string, JobModel> Jobs = new ConcurrentDictionary<string, JobModel>();

		/// <summary>
		/// JobsSemaphore to allow 4 async thread (jobs) at the same time
		/// </summary>
		private readonly SemaphoreSlim JobsSemaphore = new SemaphoreSlim(4, 4);


        public JobService(ILogger<JobService> logger)
        {
            _logger = logger;
        }

		public bool TryAddJob(JobModel jobModel)
		{
			bool result = Jobs.TryAdd(jobModel.ID, jobModel);
			// If the Job was added to the list, start a async thread with the JobModel
			if (result) 
			{
				_logger.LogInformation("Job ID: {ID} has been created & added to the queue.", jobModel.ID);
				StartJobAsync(jobModel);
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

		// Removes a job by its ID
		public bool TryRemoveJob(string ID)
		{
			return Jobs.TryRemove(ID, out _);
		}

		// Cleans up old jobs
		public void OldJobsCleanup(double totalMinutes)
		{
			DateTime dateTimeNow = DateTime.Now;
			int cleanedJobs = 0;
            foreach (JobModel jobModel in Jobs.Values)
            {
				// Ensure that the job is not currently running or waiting to be ran
				if (jobModel.Status != "Running" && jobModel.Status != "Queued") 
				{
					// If the numbers of minutes between the job creation and NOW is biggan than totalMinutes
					if (dateTimeNow.Subtract(jobModel.CreationDate).TotalMinutes >= totalMinutes) 
					{
						// Free the old job from the list
						jobModel.Dispose();
						_ = TryRemoveJob(jobModel.ID);
						cleanedJobs++;
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
				Jobs[jobModel.ID].Status = "Running";
				_logger.LogDebug("Job ID: {ID} changed status from 'Queuing' to 'Running'.", jobModel.ID);
				try
				{
					for (int i = 0; i < 20; i++)
					{
						if (jobModel.CancellationTokenSource.IsCancellationRequested)
						{
							_logger.LogInformation("Job ID: {ID} has been cancelled during it's execution.", jobModel.ID);
							Jobs[jobModel.ID].Status = "Cancelled";
							break;
						}
						Console.WriteLine("Inside semaphore " + i.ToString());
						await Task.Delay(4000);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, ex.Message);
					Jobs[jobModel.ID].Status = "Failed";
				}
				finally
				{
					// Ignore this part if status is Failed or Cancelled
					if (Jobs[jobModel.ID].Status == "Running")
					{
						_logger.LogInformation("Job ID: {ID} finished it's execution.", jobModel.ID);
						Jobs[jobModel.ID].Status = "Finished";
					}
					JobsSemaphore.Release();
				}
			}
			catch (OperationCanceledException)
			{
				// If the CancellationToken is triggered, change the job status to cancelled.
				Jobs[jobModel.ID].Status = "Cancelled";
				_logger.LogInformation("Job ID: {ID} has been cancelled before it's execution.", jobModel.ID);
			}
		}
	}
}

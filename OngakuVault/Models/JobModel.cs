using OngakuVault.Services;
using System.Text.Json.Serialization;

namespace OngakuVault.Models
{
	/// <summary>
	/// The JobModel class used by <see cref="JobService"/> to contains informations about a Job.
	/// </summary>
	public class JobModel : IDisposable
	{
		/// <summary>
		/// The constructor is executed during the initialization of a <see cref="JobModel"/>.
		/// </summary>
		/// <param name="jobData">The additional data stocked inside the <see cref="JobModel.Data"/>.</param>
		public JobModel(MediaInfoModel jobData)
		{
			Data = jobData;
		}

		/// <summary>
		/// Unique Job ID
		/// </summary>
		public string ID { get; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Additional Data for the current job.
		/// </summary>
		public MediaInfoModel Data { get; set; }
		/// <summary>
		/// Job creation date
		/// </summary>
		public DateTime CreationDate { get; } = DateTime.Now;
		/// <summary>
		/// The current progress of the job
		/// </summary>
		public int Progress { get; set; } = 0;
		/// <summary>
		/// The current status of the job
		/// </summary>
		public JobStatus Status { get; set; } = JobStatus.WaitingForQueue;
		/// <summary>
		/// Cancellation token to cancel the job execution
		/// </summary>
		[JsonIgnore] // Ignore Json since we don't want to return the cencellationtoken to users (API response)
		public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

		public void Dispose()
		{
			CancellationTokenSource?.Dispose();
		}
	}

	/// <summary>
	/// A list of all possible states of a Job
	/// </summary>
	public enum JobStatus 
	{
		WaitingForQueue,
		Queued,
		Running,
		Completed,
		Cancelled,
		Failed
	}
}

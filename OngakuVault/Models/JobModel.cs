using OngakuVault.Services;
using System.Text.Json.Serialization;

namespace OngakuVault.Models
{
	/// <summary>
	/// The JobModel class used by <see cref="JobService{T}"/> to contains informations about a Job.
	/// </summary>
	/// <typeparam name="T">The value type of the additional data stocked inside <see cref="JobModel{T}.Data"/></typeparam>
	public class JobModel<T> : IDisposable
	{

		/// <summary>
		/// The constructor is executed during the initialization of a <see cref="JobModel{T}"/>.
		/// </summary>
		/// <param name="jobData">The additional data stocked inside the <see cref="JobModel{T}.Data"/>. Data type was defined by T</param>
		/// <param name="executeJob">The method called when the job start running.</param>
		public JobModel(T jobData, IJobService<T>.ExecuteJob executeJob)
		{
			Data = jobData;
			ExecuteJob = executeJob;
		}

		/// <summary>
		/// Unique Job ID
		/// </summary>
		public string ID { get; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Additional Data for the current job. The type of the data is 
		/// defined by the T on <see cref="JobModel{T}"/>
		/// </summary>
		public T Data { get; set; }
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
		/// <summary>
		/// Store a async method matching <see cref="IJobService{T}.ExecuteJob"/> inside a delegate that will be
		/// called when the Job will change to it's running state.
		/// </summary>
		[JsonIgnore]
		public IJobService<T>.ExecuteJob ExecuteJob { get; }

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

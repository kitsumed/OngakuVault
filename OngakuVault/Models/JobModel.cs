using OngakuVault.Services;
using System.Text.Json.Serialization;

namespace OngakuVault.Models
{
	/// <summary>
	/// The JobModel class used by <see cref="JobService"/> to contains informations about a Job.
	/// </summary>
	public class JobModel : IDisposable
	{
		// We store the webSockerManager service to broadcast our status update to all clients
		private readonly IWebSocketManagerService _webSocketManagerService;

		/// <summary>
		/// The constructor is executed during the initialization of a <see cref="JobModel"/>.
		/// </summary>
		/// <param name="webSocketManagerService">Dependency injection of webSocketManagerService to broadcast job status update</param>
		/// <param name="jobRestCreationData">A <see cref="JobRESTCreationModel"/> containing the job additional info and execution configuration</param>
		public JobModel(IWebSocketManagerService webSocketManagerService, JobRESTCreationModel jobRestCreationData)
		{
			// Configure dependency injection
			_webSocketManagerService = webSocketManagerService;
			// Define JobModel values
			Data = jobRestCreationData.mediaInfo;
			Configuration = jobRestCreationData.jobConfiguration;
		}

		/// <summary>
		/// Unique Job ID
		/// </summary>
		public string ID { get; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Additional Data for the current job
		/// </summary>
		public MediaInfoModel Data { get; set; }
		/// <summary>
		/// Configuration for the current job
		/// </summary>
		public JobConfigurationModel Configuration { get; set; }
		/// <summary>
		/// Job creation date
		/// </summary>
		public DateTime CreationDate { get; } = DateTime.Now;
		/// <summary>
		/// The current progress of the whole job execution. 0 to 100
		/// </summary>
		public int Progress { get; set; } = 0;

		/// <summary>
		/// The name or description of the task currently being done during the job execution
		/// </summary>
		public string? ProgressTaskName { get; set; }

		/// <summary>
		/// The current status of the job
		/// </summary>
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public JobStatus Status { get; set; } = JobStatus.WaitingForQueue;
		/// <summary>
		/// Cancellation token to cancel the job execution
		/// </summary>
		[JsonIgnore] // Ignore Json since we don't want to return the cencellationtoken to users (API response)
		public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

		/// <summary>
		/// Update status related key in the current JobModel and broadcast a "JobReportedStatusUpdate" key on the
		/// webSockerManager service with the whole JobModel as data. If you leave every arguments on default, only
		/// the broadcast will be made.
		/// </summary>
		/// <param name="jobStatus">The new jobStatus of the current job. Default to current status</param>
		/// <param name="progressTaskName">The new progressTaskName of the current job. Default to current task name</param>
		/// <param name="progress">The new progress precent (0-100) of the current job. Default to current progress value</param>
		public void ReportStatus(JobStatus? jobStatus = null, string? progressTaskName = null, int? progress = null)
		{
			if (jobStatus != null) Status = (JobStatus)jobStatus;
			if (!string.IsNullOrEmpty(progressTaskName)) ProgressTaskName = progressTaskName;
			if (progress != null) Progress = progress.Value;
			// Broadcast to all connected client a key "JobReportedStatusUpdate" containing the current JobModel converted to json
			_webSocketManagerService.BroadcastAsync("JobReportedStatusUpdate", this);
		}

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

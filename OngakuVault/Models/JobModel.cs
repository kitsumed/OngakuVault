using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OngakuVault.Models
{
	/// <summary>
	/// The JobModel class contains informations about a Job
	/// </summary>
	public class JobModel : IDisposable
	{

		public JobModel(JobCreateRequestModel jobCreationData)
		{
			Data = new MediaInfoModel()
			{
				Name = jobCreationData.Name,
				Album = jobCreationData.Album,
				Artist = jobCreationData.Artist,
				MediaUrl = jobCreationData.OriginalMediaUrl,
			};
		}

		/// <summary>
		/// Unique Job ID
		/// </summary>
		public string ID { get; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Media informations for the current job
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

	/// <summary>
	/// The JobCreateRequestModel class are the fields used by RESTAPI to create a new job <see cref="JobModel"/>.
	/// In this application case, theses informations will be used by <see cref="JobModel"/> constructor
	/// to create a <see cref="MediaInfoModel"/>.
	/// </summary>
	public class JobCreateRequestModel()
	{
		public required string Name { get; set; }
		public string Album { get; set; } = string.Empty;
		public string Artist { get; set; } = string.Empty;
		[Url]
		public required string OriginalMediaUrl { get; set; }
	}
}

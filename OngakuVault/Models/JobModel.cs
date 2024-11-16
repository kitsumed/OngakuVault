using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OngakuVault.Models
{
	public class JobModel : IDisposable
	{

		public JobModel(JobModelCreate jobModelDTO)
		{
			Name = jobModelDTO.Name;
			Album = jobModelDTO.Album;
			Artist = jobModelDTO.Artist;
			originalUrl = jobModelDTO.originalUrl;
		}

		/// <summary>
		/// Unique Job ID
		/// </summary>
		public string ID { get; } = Guid.NewGuid().ToString();
		/// <summary>
		/// Song name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Album name
		/// </summary>
		public string Album { get; set; }
		/// <summary>
		/// Artist name
		/// </summary>
		public string Artist { get; set; }
		/// <summary>
		/// Job creation date
		/// </summary>
		public DateTime CreationDate { get; } = DateTime.Now;
		/// <summary>
		/// The song url send by the user
		/// </summary>
		public string originalUrl { get; set; }
		/// <summary>
		/// The current progress of the job
		/// </summary>
		public int Progress { get; set; } = 0;
		/// <summary>
		/// The current status of the job
		/// </summary>
		public string Status { get; set; } = "Queued";
		/// <summary>
		/// Cancellation token to cancel the job execution
		/// </summary>
		[JsonIgnore] // Ignore Json since we don't want to return the cencellationtoken to users
		public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

		public void Dispose()
		{
			CancellationTokenSource?.Dispose();
		}
	}

	/// <summary>
	/// This class is used by RESTAPI to allow the creation of a JobModel later by calling <see cref="JobModel"/>
	/// </summary>
	public class JobModelCreate()
	{
		public required string Name { get; set; }
		public string Album { get; set; } = "Unknown";
		public string Artist { get; set; } = "Unknown";
		[Url]
		public required string originalUrl { get; set; }
	}
}

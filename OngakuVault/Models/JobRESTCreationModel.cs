using Swashbuckle.AspNetCore.Annotations;

namespace OngakuVault.Models
{
	/// <summary>
	/// The JobRESTCreationModel is a class designed to be used by the REST api to allow users to create a <see cref="JobModel"/> on the server side.
	/// It include the job configuration and additional data.
	/// </summary>
	public class JobRESTCreationModel
	{
		/// <summary>
		/// Contains the media info (additional data inside the job)
		/// </summary>
		[SwaggerSchema(Description = "The additional information inside the job, in this case, the basic info about the media.")]
		public required MediaInfoModel MediaInfo { get; set; }

		/// <summary>
		/// Contains the job configuration
		/// </summary>
		[SwaggerSchema(Description = "Configuration for the job, mostly related to actions done during job execution.")]
		public required JobConfigurationModel JobConfiguration { get; set; }

	}
}

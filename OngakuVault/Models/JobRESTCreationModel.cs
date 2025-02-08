using Swashbuckle.AspNetCore.Annotations;

namespace OngakuVault.Models
{
	/// <summary>
	/// The JobRESTCreationModel is a class designed to be used by the REST api to allow users to create a <see cref="JobModel"/> on the server side.
	/// It include the job configuration and additional data.
	/// </summary>
	[SwaggerSchema(Description = "Model used to make a job creation request to the server with the required configurations")]
	public class JobRESTCreationModel
	{
		/// <summary>
		/// Contains the media info (additional data inside the job)
		/// </summary>
		[SwaggerSchema(Description = "The informations about the media, fields left empty will keep default metadata, otherwewise we overwrite metadata.")]
		public required MediaInfoModel MediaInfo { get; set; }

		/// <summary>
		/// Contains the job configuration
		/// </summary>
		[SwaggerSchema(Description = "Configuration for the job, mostly related to actions done during job execution.")]
		public required JobConfigurationModel JobConfiguration { get; set; }

	}
}

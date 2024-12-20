using Swashbuckle.AspNetCore.Annotations;
using YoutubeDLSharp.Options;

namespace OngakuVault.Models
{
	/// <summary>
	/// Contains configurations for a specific <see cref="JobModel"/>. Will be used during the job execution to do actions with the job additional data <see cref="JobModel.Data"/>
	/// </summary>
	public class JobConfigurationModel
	{
		/// <summary>
		/// The audio format to which the best audio will be converted after being downloaded.
		/// </summary>
		[SwaggerSchema( Description = "The audio format to which the best audio will be converted after being downloaded. Defaults to 'Mp3'. Choose 'Best' to avoid re-encoding.")]
		public AudioConversionFormat FinalAudioFormat { get; set; } = AudioConversionFormat.Mp3;
	}
}

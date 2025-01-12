using Swashbuckle.AspNetCore.Annotations;
using YoutubeDLSharp.Options;

namespace OngakuVault.Models
{
	/// <summary>
	/// Contains configurations for a specific <see cref="JobModel"/>. Will be used during the job execution to do actions with the job additional data <see cref="JobModel.Data"/>.
	/// Can also contains additional informations to complement the data of <see cref="JobModel.Data"/>
	/// </summary>
	public class JobConfigurationModel
	{
		/// <summary>
		/// The audio format to which the best audio will be converted after being downloaded.
		/// </summary>
		[SwaggerSchema( Description = "The audio format to which the best audio will be converted after being downloaded. Defaults to 'Mp3'. Choose 'Best' to avoid re-encoding.")]
		public AudioConversionFormat FinalAudioFormat { get; set; } = AudioConversionFormat.Mp3;

		/// <summary>
		/// The lyrics to put inside the file metadata.
		/// NOTE: If all <see cref="MediaLyric.Time"/> have a value >= 0, the lyrics are considered "synced lyrics", if
		/// all values are null, it's considered as "lyrics" with no timestamp. If both types are mixed, fallback to "lyrics".
		/// </summary>
		public List<MediaLyric>? Lyrics { get; set; }
	}

	/// <summary>
	/// This class represent one Lyric in a list of Lyric(s).
	/// Can be used for both: "lyrics" and "synced lyrics"
	/// </summary>
	public class MediaLyric
	{
		/// <summary>
		/// The time in the audio, in milliseconds, of the current lyric (for synced lyrics)
		/// </summary>
		public int? Time { get; set; } = null;
		/// <summary>
		/// The lyric text
		/// </summary>
		public required string Content { get; set; }
	}
}

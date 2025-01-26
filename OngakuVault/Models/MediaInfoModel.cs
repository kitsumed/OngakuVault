using Swashbuckle.AspNetCore.Annotations;

namespace OngakuVault.Models
{
	/// <summary>
	/// The MediaInfoModel contains basic information about a requested media.
	/// In this application case, it is usually contained inside a <see cref="JobModel.Data"/>.
	/// </summary>
	public class MediaInfoModel
	{
		/// <summary>
		/// Song name
		/// </summary>
		[SwaggerSchema(Description = "The media name/track title")]
		public required string Name { get; set; }

		/// <summary>
		/// Atrist name
		/// </summary>
		[SwaggerSchema(Description = "The artist name of the media")]
		public string? ArtistName { get; set; }

		/// <summary>
		/// Album name
		/// </summary>
		[SwaggerSchema(Description = "The album name of the media")]
		public string? AlbumName { get; set; }

		/// <summary>
		/// The webpage url of the song
		/// </summary>
		[SwaggerSchema(Description = "The webpage url of the media or direct url")]
		public required string MediaUrl { get; set; }
		/// <summary>
		/// The year of release of the song
		/// </summary>
		[SwaggerSchema(Description = "The year of release of the media")]
		public int? ReleaseYear { get; set; }
		/// <summary>
		/// The main genre of the song
		/// </summary>
		[SwaggerSchema(Description = "The genre of the media")]
		public string? Genre { get; set; }
		/// <summary>
		/// The number (position) of the song in a Album
		/// </summary>
		[SwaggerSchema( Description = "The number (position) of the media in a Album")]
		public int? TrackNumber { get; set; }

		/// <summary>
		/// The description of the file
		/// </summary>
		[SwaggerSchema(Description = "The description of the media")]
		public string? Description { get; set; }
	}

	/// <summary>
	/// The MediaInfoAdvancedModel contains advanced information about a requested media.
	/// In this application case, it is usually used in API Responses like <see cref="Controllers.MediaController.GetMediaInfo(string)"/> where
	/// the client request more informations about a media than needed by the server when receiving a job creation request.
	/// </summary>
	public class MediaInfoAdvancedModel : MediaInfoModel
	{
		/// <summary>
		/// Indicates whether a lossless audio with the best quality in all of the available audio was found on the MediaUrl page
		/// This key is only used for REST API responses.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "Indicates whether a lossless audio with the best quality in all of the available audio was found on the MediaUrl page")]
		public bool IsLosslessRecommended { get; set; } = false;

		/// <summary>
		/// Return the lyrics/subtitle of the current media selected. Only applicable if the setting value
		/// <see cref="AppSettingsModel.LYRICS_LANGUAGE_PRIORITY"/> is defined and the media have lyrics/subtitle.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "Contains the media lyrics/subtitles")]
		public List<MediaLyric> Lyrics { get; set; } = new List<MediaLyric>();
	}
}

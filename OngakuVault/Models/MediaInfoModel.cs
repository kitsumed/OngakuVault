using Swashbuckle.AspNetCore.Annotations;

namespace OngakuVault.Models
{
	/// <summary>
	/// The MediaInfoModel contains basic information about a requested media.
	/// In this application case, it is usually contained inside a <see cref="JobModel.Data"/>.
	/// </summary>
	[SwaggerSchema(Description = "Contains basic information about a requested media")]
	public class MediaInfoModel
	{
		/// <summary>
		/// Song name
		/// </summary>
		[SwaggerSchema(Nullable = false, Description = "The media name/track title")]
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
		[SwaggerSchema(Nullable = false, Description = "The webpage url of the media or direct url")]
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
	/// In this application case, it is usually used in API Responses like <see cref="Controllers.MediaController.GetMediaInfo"/> where
	/// the client request more informations about a media than needed by the server when receiving a job creation request.
	/// </summary>
	[SwaggerSchema(Description = "Contains advanced information about a requested media")]
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

		/// <summary>
		/// Indicates whether the ArtistName field contains multiple artists separated by the configured separator.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "Indicates whether the ArtistName field contains multiple artists")]
		public bool HasMultipleArtists { get; set; } = false;

		/// <summary>
		/// The primary (first) artist when multiple artists are present.
		/// Returns the same value as ArtistName if only one artist exists.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "The primary (first) artist when multiple artists are present")]
		public string? PrimaryArtistName { get; set; }

		/// <summary>
		/// Indicates whether the Genre field contains multiple genres separated by the configured separator.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "Indicates whether the Genre field contains multiple genres")]
		public bool HasMultipleGenres { get; set; } = false;

		/// <summary>
		/// The primary (first) genre when multiple genres are present.
		/// Returns the same value as Genre if only one genre exists.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "The primary (first) genre when multiple genres are present")]
		public string? PrimaryGenre { get; set; }
	}
}

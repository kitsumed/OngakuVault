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


		private string _artistName = "Unknown";
		/// <summary>
		/// Atrist name. Defaults to 'Unknown' if null or empty.
		/// </summary>
		[SwaggerSchema(Description = "The artist name of the media")]
		public string ArtistName
		{
			get => _artistName;
			set => _artistName = string.IsNullOrEmpty(value) ? "Unknown" : value;
		}

		private string _albumName = "Unknown";
		/// <summary>
		/// Album name. Defaults to 'Unknown' if null or empty.
		/// </summary>
		[SwaggerSchema(Description = "The album name of the media")]
		public string AlbumName
		{
			get => _albumName;
			set => _albumName = string.IsNullOrEmpty(value) ? "Unknown" : value;
		}

		/// <summary>
		/// The webpage url of the song
		/// </summary>
		[SwaggerSchema(Description = "The webpage url of the media")]
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
	/// In this application case, it is usually used in API Responses like <see cref="Controllers.MediaController.GetMediaInfo(string)"/>.
	/// </summary>
	public class MediaInfoAdvancedModel : MediaInfoModel
	{
		/// <summary>
		/// Indicates whether a lossless audio with the best quality in all of the available audio was found on the MediaUrl page
		/// This key is only used for REST API responses.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "Indicates whether a lossless audio with the best quality in all of the available audio was found on the MediaUrl page")]
		public bool IsLosslessRecommended { get; set; } = false;
	}
}

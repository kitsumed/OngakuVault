using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace OngakuVault.Models
{
	/// <summary>
	/// The MediaInfoModel contains information about a requested media.
	/// In this application case, it is usually contained inside a <see cref="JobModel.Data"/>.
	/// </summary>
	public class MediaInfoModel
	{
        /// <summary>
        /// Song name
        /// </summary>
        public required string Name { get; set; }


		private string _artistName = "Unknown";
		/// <summary>
		/// Album name. Defaults to 'Unknown' if null or empty.
		/// </summary>
		public string ArtistName
		{
			get => _artistName;
			set => _artistName = string.IsNullOrEmpty(value) ? "Unknown" : value;
		}

		private string _albumName = "Unknown";
		/// <summary>
		/// Album name. Defaults to 'Unknown' if null or empty.
		/// </summary>
		public string AlbumName
		{
			get => _albumName;
			set => _albumName = string.IsNullOrEmpty(value) ? "Unknown" : value;
		}

		/// <summary>
		/// The webpage url of the song
		/// </summary>
		public required string MediaUrl { get; set; }
		/// <summary>
		/// The year of release of the song
		/// </summary>
		public string? ReleaseYear { get; set; }
		/// <summary>
		/// The main genre of the song
		/// </summary>
		public string? Genre { get; set; }
		/// <summary>
		/// The number (position) of the song in a Album
		/// </summary>
		public int? TrackNumber { get; set; }


		/// <summary>
		/// Indicates whether a lossless audio was found on the MediaUrl page.
		/// This key is only used for REST API responses.
		/// </summary>
		[SwaggerSchema(ReadOnly = true, Description = "Indicates whether a lossless audio was found on the MediaUrl page")]
		public bool IsLosslessAvailable { get; set; } = false;
	}
}

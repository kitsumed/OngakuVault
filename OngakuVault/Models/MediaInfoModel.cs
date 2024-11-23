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


		private string _album = "Unknown";
		/// <summary>
		/// Album name. Defaults to 'Unknown' if null or empty.
		/// </summary>
		public string Album
		{
			get => _album;
			set => _album = string.IsNullOrEmpty(value) ? "Unknown" : value;
		}


		private string _artist = "Unknown";
		/// <summary>
		/// Album name. Defaults to 'Unknown' if null or empty.
		/// </summary>
		public string Artist
		{
			get => _artist;
			set => _artist = string.IsNullOrEmpty(value) ? "Unknown" : value;
		}
		/// <summary>
		/// The webpage url of the song
		/// </summary>
		public required string MediaUrl { get; set; }
	}
}

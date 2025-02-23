using ATL;
using System.Text;

namespace OngakuVault.Helpers
{
	/// <summary>
	/// This helper class allows for easier and less repetitive replacement of certains "dynamic" string value.
	/// Methods are called processors and will process a string for specific PIPE (|) schemes to replace them with
	/// the matching data.
	/// <code>
	/// All processors support values in between two PIPE (|) like '<strong>|example|</strong>'.
	/// </code>
	/// </summary>
	public class ValueReplacingHelper
    {
		/// <summary>
		/// Will perform Date value replacing on a input.
		/// </summary>
		/// <remarks>
		/// <strong>Supports: NOW_YEAR,NOW_MONTH,NOW_DAY,NOW_SECOND,NOW_TICKS</strong>
		/// </remarks>
		/// <param name="input">Input to process</param>
		/// <returns>The processed input with dates values</returns>
		public static StringBuilder ProcessDate(StringBuilder input) => input
			.Replace("|NOW_YEAR|", DateTime.Now.Year.ToString())
			.Replace("|NOW_MONTH|", DateTime.Now.Month.ToString())
			.Replace("|NOW_DAY|", DateTime.Now.Day.ToString())
			.Replace("|NOW_SECOND|", DateTime.Now.Second.ToString())
			.Replace("|NOW_TICKS|", DateTime.Now.Ticks.ToString());

		/// <summary>
		/// Will perform ATL Track value replacing on a input.
		/// </summary>
		/// <remarks>
		/// <strong>Supports: AUDIO_TITLE,AUDIO_ARTIST,AUDIO_ALBUM,AUDIO_YEAR,AUDIO_TRACK_NUMBER,
		/// AUDIO_LANGUAGE,AUDIO_GENRE,AUDIO_COMPOSER</strong>
		/// </remarks>
		/// <param name="input">Input to process</param>
		/// <param name="track">The Track informations</param>
		/// <returns>The processed input with ATL Track values</returns>
		public static StringBuilder ProcessTrack(StringBuilder input, Track track) => input
			.Replace("|AUDIO_TITLE|", track.Title ?? "Unknown")
			.Replace("|AUDIO_ARTIST|", track.Artist ?? "Unknown")
			.Replace("|AUDIO_ALBUM|", track.Album ?? "Unknown")
			.Replace("|AUDIO_YEAR|", (track.Year ?? 0).ToString())
			.Replace("|AUDIO_TRACK_NUMBER|", (track.TrackNumber ?? 0).ToString())
			.Replace("|AUDIO_LANGUAGE|", track.Language ?? "Unknown")
			.Replace("|AUDIO_GENRE|", track.Genre ?? "Unknown")
			.Replace("|AUDIO_COMPOSER|", track.Composer ?? "Unknown");
	}
}

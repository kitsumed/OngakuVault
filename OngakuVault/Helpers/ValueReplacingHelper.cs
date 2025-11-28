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
		/// <strong>Supports: NOW_YEAR,NOW_MONTH,NOW_DAY,NOW_HOUR,NOW_MINUTE,NOW_SECOND,NOW_TICKS</strong>
		/// </remarks>
		/// <param name="input">Input to process</param>
		/// <returns>The processed input with dates values</returns>
		public static StringBuilder ProcessDate(StringBuilder input) => input
			.Replace("|NOW_YEAR|", DateTime.Now.Year.ToString())
			.Replace("|NOW_MONTH|", DateTime.Now.Month.ToString())
			.Replace("|NOW_DAY|", DateTime.Now.Day.ToString())
			.Replace("|NOW_HOUR|", DateTime.Now.Hour.ToString())
			.Replace("|NOW_MINUTE|", DateTime.Now.Minute.ToString())
			.Replace("|NOW_SECOND|", DateTime.Now.Second.ToString())
			.Replace("|NOW_TICKS|", DateTime.Now.Ticks.ToString());

		/// <summary>
		/// Will perform ATL Track value replacing on a input.
		/// </summary>
		/// <remarks>
		/// <strong>Supports: AUDIO_TITLE,AUDIO_ARTIST,AUDIO_ALBUM,AUDIO_YEAR,AUDIO_TRACK_NUMBER,
		/// AUDIO_DISC_NUMBER,AUDIO_LANGUAGE,AUDIO_GENRE,AUDIO_COMPOSER,AUDIO_DURATION,AUDIO_DURATION_MS</strong>
		/// <br/>
		/// Note: For AUDIO_ARTIST and AUDIO_GENRE, only the primary (first) value is used when multiple values are present,
		/// separated by ATL.Settings.DisplayValueSeparator.
		/// </remarks>
		/// <param name="input">Input to process</param>
		/// <param name="track">The Track informations</param>
		/// <returns>The processed input with ATL Track values</returns>
		public static StringBuilder ProcessTrack(StringBuilder input, Track track) => input
			.Replace("|AUDIO_TITLE|", track?.Title ?? "Unknown")
			.Replace("|AUDIO_ARTIST|", GetPrimaryValue(track?.Artist))
			.Replace("|AUDIO_ALBUM|", track?.Album ?? "Unknown")
			.Replace("|AUDIO_YEAR|", (track?.Year ?? 0).ToString())
			.Replace("|AUDIO_TRACK_NUMBER|", (track?.TrackNumber ?? 0).ToString())
			.Replace("|AUDIO_DISC_NUMBER|", (track?.DiscNumber ?? 0).ToString())
			.Replace("|AUDIO_ISRC|", track?.ISRC ?? "CC-XXX-YY-NNNNN")
			.Replace("|AUDIO_CATALOG_NUMBER|", track?.CatalogNumber ?? "CatalogUnknown")
			.Replace("|AUDIO_LANGUAGE|", track?.Language ?? "Unknown")
			.Replace("|AUDIO_GENRE|", GetPrimaryValue(track?.Genre))
			.Replace("|AUDIO_COMPOSER|", track?.Composer ?? "Unknown")
			.Replace("|AUDIO_DURATION|", (track?.Duration ?? 0).ToString())
			.Replace("|AUDIO_DURATION_MS|", (track?.DurationMs ?? 0.0).ToString());

		/// <summary>
		/// Extracts the primary (first) value from a string that may contain multiple values
		/// separated by ATL.Settings.DisplayValueSeparator.
		/// This is useful for fields like Artist or Genre that can contain multiple values.
		/// </summary>
		/// <param name="value">The value string, which may contain multiple values</param>
		/// <param name="defaultValue">The default value to return if input is null or empty. Defaults to "Unknown"</param>
		/// <returns>The primary (first) value, or the default value if the input is null or empty</returns>
		public static string GetPrimaryValue(string? value, string defaultValue = "Unknown")
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}

			// Split by the ATL DisplayValueSeparator and return the first value (trimmed)
			char separator = ATL.Settings.DisplayValueSeparator;
			int separatorIndex = value.IndexOf(separator);
			
			if (separatorIndex >= 0)
			{
				return value.Substring(0, separatorIndex).Trim();
			}

			return value.Trim();
		}

		/// <summary>
		/// Extracts the primary (first) value from a string that may contain multiple values
		/// separated by ATL.Settings.DisplayValueSeparator.
		/// This overload returns null if the input is null or empty.
		/// </summary>
		/// <param name="value">The value string, which may contain multiple values</param>
		/// <returns>The primary (first) value, or null if the input is null or empty</returns>
		public static string? GetPrimaryValueOrNull(string? value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}

			// Split by the ATL DisplayValueSeparator and return the first value (trimmed)
			char separator = ATL.Settings.DisplayValueSeparator;
			int separatorIndex = value.IndexOf(separator);
			
			if (separatorIndex >= 0)
			{
				return value.Substring(0, separatorIndex).Trim();
			}

			return value.Trim();
		}

		/// <summary>
		/// Checks if a string contains multiple values separated by ATL.Settings.DisplayValueSeparator.
		/// </summary>
		/// <param name="value">The value string to check</param>
		/// <returns>True if the string contains multiple values, false otherwise</returns>
		public static bool HasMultipleValues(string? value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			return value.Contains(ATL.Settings.DisplayValueSeparator);
		}
	}
}

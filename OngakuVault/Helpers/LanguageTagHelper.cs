namespace OngakuVault.Helpers
{
	/// <summary>
	/// This class implements static methods related to IETF language tag
	/// </summary>
	// https://wikipedia.org/wiki/IETF_language_tag
	public static class LanguageTagHelper
    {
		/// <summary>
		/// Compare two IETF Language Tag and return true if they are similar.
		/// </summary>
		/// <remarks>
		/// This method will perform two checks, verify if compare1 and compare2 are equals, if not
		/// verify if compare2 start with compare1 content with "-" added (to follow IETF format).
		/// 
		/// For example, a reference of "en" would return true to compared IETF tag starting with "en-"
		/// </remarks>
		/// <param name="compare1">The basic, reference IETF Language Tag</param>
		/// <param name="compare2">The compared IETF Language Tag</param>
		/// <returns>True if the both language tag are the equals or start with the same "reference".</returns>
		public static bool IsIETFLanguageTagSimilar(string compare1, string compare2) 
		{
			// If "compare1" == "compare2" (without verifying upper/lowercase)
			if (compare1.Equals(compare2, StringComparison.InvariantCultureIgnoreCase)) return true;
			// If "compare2" start with "compare1-"  (without verifying upper/lowercase)
			// Allow IETFT to compare1 "en" against compare2 "en-US" and still return true
			if (compare2.StartsWith(compare1 + "-", StringComparison.InvariantCultureIgnoreCase)) return true;

			return false;
		}
    }
}

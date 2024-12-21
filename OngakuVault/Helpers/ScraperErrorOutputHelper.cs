using System.Text.RegularExpressions;


namespace OngakuVault.Helpers
{
	/// <summary>
	/// This helper class allows for easier and global control over error outputs of the yt-dlp scraper by processings
	/// the outputs for commons error.
	/// </summary>
	public static class ScraperErrorOutputHelper
	{
		/// <summary>
		/// Process the yt-dlp scrapper error outputs for known error (throw custom message)
		/// or unknown error (throw scrapper errors separated by <see cref="Environment.NewLine"/>).
		/// This method should only be called inside a try-catch if the scraper did not success in it's request.
		/// <code>
		/// try
		/// {
		///     var mediaData = await YoutubeDL.RunVideoDataFetch("example.com", default, default, default);
		///     if (!mediaData.Sucess) ScraperErrorOutputHelper.ProcessScraperErrorOutput(mediaData.ErrorOutput);
		///     
		/// }
		/// catch (ScraperErrorException ex)
		/// {
		///     if (ex.IsKnownError)
		///     {
		///         Console.WriteLine("This is a known error, custom message.");
		///     }
		///     else
		///     {
		///         Console.WriteLine("This is an unknown error, scraper messages.");
		///     }
		/// }
		/// </code>
		/// </summary>
		/// <param name="errorOutputs">The scraper (yt-dlp) error outputs</param>
		/// <exception cref="ProcessedScraperErrorOutputException"></exception>
		public static void ProcessScraperErrorOutput(string[] errorOutputs) 
		{
			if (errorOutputs.Length >= 1)
			{
				/// We first verify if it's a "known" error
				// Verify for HTTP error code returned by the webpage
				if (errorOutputs[0].Contains("[generic] Unable to download webpage: HTTP Error"))
				{
					// Search for the HTTP code in the error message
					Match httpErrorCode = Regex.Match(errorOutputs[0], @"HTTP Error (\d{3})");
					if (httpErrorCode.Success)
					{
						throw new ProcessedScraperErrorOutputException($"Scraper request failed and got the HTTP response code '{httpErrorCode.Groups[1]}' from the webpage.", true);
					}
				}
				// Verify if the the error is related to the scraper not finding any media on the webpage
				if (errorOutputs.Length >= 2)
				{
					if (errorOutputs[0].Contains("[generic] Falling back on generic information extractor") && errorOutputs[1].Contains("Unsupported URL: "))
					{
						throw new ProcessedScraperErrorOutputException("The scraper did not find a specific extractor for this URL. Fell back to the generic extractor but did not find any media on the webpage.", true);
					}
				}
				/// The error is a unknown one
				// Send all of the scraper error messages in the exception
				throw new ProcessedScraperErrorOutputException(string.Join(Environment.NewLine, errorOutputs), false);
			}
			else throw new ProcessedScraperErrorOutputException("The scraper did not return any error message in its output.", false);

		}

		/// <summary>
		/// A exception that represent the proccesed scraper error outputs that can be safely handeled
		/// </summary>
		public class ProcessedScraperErrorOutputException : Exception
		{
			/// <summary>
			/// If true, the scraper error was reconised and a custom message was reported.
			/// The message can be returned to the client safely.
			/// If false, exceptions received from the scraper will be separated by <see cref="Environment.NewLine"/>
			/// </summary>
			public bool IsKnownError { get; }

			/// <summary>
			/// A exception that represent the proccesed scraper error outputs that can be safely handeled
			/// </summary>
			/// <param name="message">The scraper error message(s) or custom message</param>
			/// <param name="isKnownError">If the message is a custom message or not</param>
			public ProcessedScraperErrorOutputException(string message, bool isKnownError) : base(message)
			{
				IsKnownError = isKnownError;
			}
		}
	}
}

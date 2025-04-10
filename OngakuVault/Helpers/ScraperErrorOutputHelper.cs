﻿using System.Text.RegularExpressions;


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
			// Separate errors messages from warnings (error messages start with 'ERROR:')
			IEnumerable<string> actualErrors = errorOutputs.Where(errorLine => errorLine.StartsWith("ERROR:"));
			// Ensure we have at least 1 error
			if (actualErrors.Any())
			{
				/// We first verify if it's a "known" error
				foreach (string errorLine in actualErrors) 
				{
					// Search for error related to webpage HTTP code response
					// Example: ERROR: [generic] Got HTTP Error 403 caused by
					Match httpErrorCode = Regex.Match(errorLine, @"HTTP Error (\d{3})");
					if (httpErrorCode.Success)
					{
						throw new ProcessedScraperErrorOutputException($"Scraper request failed and got the HTTP response code '{httpErrorCode.Groups[1]}' from the webpage.", true, errorLine);
					}

					// Search for error related to DNS not resolving
					// Example: ERROR: Unable to obtain version info (<random>: Failed to resolve 'example.com' ([Errno 11004] getaddrinfo failed))
					Match domainName = Regex.Match(errorLine, @"Failed to resolve '([^']+)'");
					if (domainName.Success)
					{
						throw new ProcessedScraperErrorOutputException($"Scraper could not resolve the domain '{domainName.Groups[1].Value}'. Are you using a valid url?", true, errorLine);
					}

					// Search for error reporting a url not supported by the extractor
					// Example: ERROR: Unsupported URL: https://www.youtube.com/watch?v=0qt4BzlGap&feature=youtu.be
					if (errorLine.Contains("Unsupported URL: "))
					{
						throw new ProcessedScraperErrorOutputException("The scraper extractor reported the url as unsupported.", true, errorLine);
					}

					// Search for error related to DRM (websites known for using DRM)
					// Example: ERROR: [DRM] The requested site is known to use DRM protection. It will NOT be supported.
					// Note: Seems to be reported by https://github.com/yt-dlp/yt-dlp/blob/05c8023a27dd37c49163c0498bf98e3e3c1cb4b9/yt_dlp/extractor/unsupported.py#L21
					if (errorLine.Contains("The requested site is known to use DRM protection"))
					{
						throw new ProcessedScraperErrorOutputException("Scraper did not proceed with the request due to the website being know to use DRM protection, which isn't supported.", true, errorLine);
					}

					// Search for error related to DRM (DRM content detected)
					// Example: ERROR: [generic] random: This video is DRM protected
					// Reported by https://github.com/yt-dlp/yt-dlp/blob/05c8023a27dd37c49163c0498bf98e3e3c1cb4b9/yt_dlp/extractor/common.py#L1221
					// Note: might also trigger false positive like: ERROR: This video is either unavailable in your region or is DRM protected
					if (errorLine.Contains("DRM protected"))
					{
						throw new ProcessedScraperErrorOutputException("Scraper detected that the website may/is using DRM protection, which isn't supported.", true, errorLine);
					}

					// Search for error related to login required
					// Example: ERROR: This video is only available for registered users
					// Reported by https://github.com/yt-dlp/yt-dlp/blob/05c8023a27dd37c49163c0498bf98e3e3c1cb4b9/yt_dlp/extractor/common.py#L1242
					if (errorLine.Contains("This video is only available for registered users"))
					{
						throw new ProcessedScraperErrorOutputException("Scraper reported that the video is only available for registered users.", true, errorLine);
					}

					// Search for error related to geo restrictions
					// Example: ERROR: This video is not available from your location due to geo restriction
					// Reported by https://github.com/yt-dlp/yt-dlp/blob/05c8023a27dd37c49163c0498bf98e3e3c1cb4b9/yt_dlp/extractor/common.py#L1252
					if (errorLine.Contains("This video is not available from your location due to geo restriction"))
					{
						throw new ProcessedScraperErrorOutputException("Scraper reported that the video is not available due to geo restriction.", true, errorLine);
					}

				}
				/// The error is a unknown one
			}
			// Send all of the scraper error outputs in the exception
			throw new ProcessedScraperErrorOutputException(string.Join(Environment.NewLine, errorOutputs), false);
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
			/// Only defined when IsKnownError is true, this field contains the original scraper error.
			/// </summary>
			public string? OriginalError { get; }

			/// <summary>
			/// A exception that represent the proccesed scraper error outputs that can be safely handeled
			/// </summary>
			/// <param name="message">The scraper error message(s) or custom message</param>
			/// <param name="isKnownError">If the message is a custom message or not</param>
			/// <param name="originalError">The original scraper error message</param>
			public ProcessedScraperErrorOutputException(string message, bool isKnownError, string? originalError = null) : base(message)
			{
				if (isKnownError && originalError == null) throw new ArgumentException("KnownError with a custom error message need to provide their originalError. (originalError is null)");
				IsKnownError = isKnownError;
				OriginalError = originalError;
			}
		}
	}
}

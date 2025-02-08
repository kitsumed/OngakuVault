using ATL;
using Microsoft.Extensions.Options;
using OngakuVault.Models;
using System.Collections.Concurrent;
using System.Linq;
using YoutubeDLSharp;
using static OngakuVault.Helpers.ScraperErrorOutputHelper;

namespace OngakuVault.Services
{
	public interface IJobService
	{
		/// <summary>
		/// Create and configure a job that can then be added to the execution queue
		/// </summary>
		/// <param name="jobRESTCreationDataModel">A <see cref="JobRESTCreationModel"/> containing the job additional info used during execution</param>
		/// <returns>A JobModel</returns>
		JobModel CreateJob(JobRESTCreationModel jobRESTCreationDataModel);

		/// <summary>
		/// Add a new job inside the list and add it to the execution queue
		/// </summary>
		/// <param name="jobModel">The current job informations</param>
		/// <returns>True if the job was added, false if a job with the same ID is already in the list (probably the same job)</returns>
		bool TryAddJobToQueue(JobModel jobModel);

		/// <summary>
		/// Get a job (JobModel) from it's ID
		/// </summary>
		/// <param name="ID">The Job ID</param>
		/// <param name="job">The job data</param>
		/// <returns>True if found, else false</returns>
		bool TryGetJobByID(string ID, out JobModel? job);

		/// <summary>
		/// Get all Jobs in the list
		/// </summary>
		/// <returns>A ICollection of all JobModels</returns>
		ICollection<JobModel> GetJobs();
	}

	/// <summary>
	/// This class implements the <see cref="IJobService"/> interface and provides functionality
	/// to manage jobs. It allows executing up to 4 async methods in parallel as "jobs".
	/// </summary>
	public class JobService : IJobService
	{
		private readonly ILogger<JobService> _logger;
		private readonly AppSettingsModel _appSettings;
		private readonly IMediaDownloaderService _mediaDownloaderService;
		private readonly IWebSocketManagerService _websocketManagerService;

		/// <summary>
		/// List of Jobs managed by the JobService (waiting for execution, running, completed, etc)
		/// </summary>
		private readonly ConcurrentDictionary<string, JobModel> Jobs = new ConcurrentDictionary<string, JobModel>();

		/// <summary>
		/// JobsSemaphore to allow a specific number of async thread (jobs) at the same time.
		/// Others threads will wait for a place to be free before executing
		/// </summary>
		private readonly SemaphoreSlim JobsSemaphore;

		/// <summary>
		/// Run jobs cleanup timer at every 30 minutes
		/// </summary>
		private readonly TimeSpan _runCleanupAtEvery = TimeSpan.FromMinutes(30);

		public JobService(ILogger<JobService> logger,IOptions<AppSettingsModel> appSettings, IMediaDownloaderService mediaDownloaderService, IWebSocketManagerService webSocketManagerService)
        {
            _logger = logger;
			_appSettings = appSettings.Value;
			_mediaDownloaderService = mediaDownloaderService;
			_websocketManagerService = webSocketManagerService;

			JobsSemaphore = new SemaphoreSlim(_appSettings.PARALLEL_JOBS, _appSettings.PARALLEL_JOBS);
			// Init jobs cleanup async task
			Task.Run(async () =>
			{
				using PeriodicTimer periodicTimer = new PeriodicTimer(_runCleanupAtEvery);
				while (await periodicTimer.WaitForNextTickAsync())
				{
					// Clear eligible jobs that are older than _runCleanupAtEvery
					OldJobsCleanup(_runCleanupAtEvery.TotalMinutes);
				}
			});
		}

		public JobModel CreateJob(JobRESTCreationModel jobRESTCreationDataModel)
		{
			return new JobModel(_websocketManagerService, jobRESTCreationDataModel);
		}

		public bool TryAddJobToQueue(JobModel jobModel)
		{
			bool result = Jobs.TryAdd(jobModel.ID, jobModel);
			// If the Job was added to the list, start a async thread with the JobModel
			if (result) 
			{
				_logger.LogInformation("Job ID: '{ID}' has been added to the execution queue. (Queued)", jobModel.ID);
				jobModel.ReportStatus(JobStatus.Queued); // Report to all websocket connection that the job is now in the execution queue
				AddJobToExecutionQueueAsync(jobModel.ID); // Plan the job for future execution
			} 
			return result;
		}

		public bool TryGetJobByID(string ID, out JobModel? job)
		{
			// Try to get the job, return success boolean, & out JobModel
			return Jobs.TryGetValue(ID, out job);
		}

		public ICollection<JobModel> GetJobs()
		{
			return Jobs.Values;
		}

		// [PRIVATE METHODS]

		/// <summary>
		/// Dispose and remove jobs in the jobs list if they completed/failed execution and are older than a specific duration.
		/// </summary>
		/// <param name="totalMinutes">The minimum number of minutes</param>
		private void OldJobsCleanup(double totalMinutes)
		{
			DateTime dateTimeNow = DateTime.Now;
            foreach (JobModel jobModel in Jobs.Values)
            {
				// Ensure that the job is not currently running or waiting to be ran
				if (jobModel.Status != JobStatus.Running && jobModel.Status != JobStatus.Queued) 
				{
					// If the numbers of minutes between the job creation and NOW is bigger than totalMinutes
					if (dateTimeNow.Subtract(jobModel.CreationDate).TotalMinutes >= totalMinutes) 
					{
						// Free the old job from the list
						if (Jobs.TryRemove(jobModel.ID, out _))
						{
							jobModel?.Dispose();
						}
					};
				}
			}
        }

		/// <summary>
		/// Start a new thread for a Job and wait for JobsSemaphore before processing
		/// </summary>
		/// <param name="jobID">The job ID in the <see cref="Jobs"/> list.</param>
		private async void AddJobToExecutionQueueAsync(string jobID)
		{
			// Allow job failed checks to detect if a the job was set to failed state manually or because of a thrown exception.
			bool didJobFailedDueToThrownException = false;
			try
			{
				// Wait for a new place in the execution queue
				await JobsSemaphore.WaitAsync(Jobs[jobID].CancellationTokenSource.Token);
				Jobs[jobID].ReportStatus(JobStatus.Running, "Starting job...", 0); // Update job status to Running
				_logger.LogInformation("Job ID: '{ID}' is now running.", jobID);
				try
				{
					// Create a progress update for the yt-dlp scraper. Represent 80% of the Job progress
					// We don't need to freeze the job progress to prevent going over 100% here since we are starting at 0%
					IProgress<DownloadProgress> downloadProgress = new Progress<DownloadProgress>(progress =>
					{
						// Progress % contributions in total (0-80)
						const int preProcessingPercentage = 5; // Pre-processing is 5%
						const int downloadingPercentage = 70; // Downloading is 70%
						const int postProcessingPercentage = 5; // Post-processing is 5%
						int? newProgress = null;
						string? newProgressTaskName = null;

						if (progress.State == DownloadState.PreProcessing)
						{
							newProgressTaskName = $"Scraper is preprocessing...";
							// 0 to 5 when scraper return pre-processing at 100%.
							newProgress = (int)(preProcessingPercentage * progress.Progress);
						}
						else if (progress.State == DownloadState.Downloading)
						{
							newProgressTaskName = $"Scraper is downloading media... ETA: {progress.ETA ?? "Unknown"}";
							// 5 to 75 when scraper return downloading at 100%.
							newProgress = preProcessingPercentage + (int)(downloadingPercentage * progress.Progress);
						}
						else if (progress.State == DownloadState.PostProcessing)
						{
							newProgressTaskName = $"Scraper is postprocessing...";
							newProgress = preProcessingPercentage + downloadingPercentage + (int)(postProcessingPercentage * progress.Progress);
						}

						if (newProgress != null)
						{
							// Ensure newProgress is bigger than current job progress before updating
							if (newProgress > Jobs[jobID].Progress)
							{
								Jobs[jobID].ReportStatus(JobStatus.Running, newProgressTaskName, newProgress);
							}
						}
					});

					FileInfo? downloadedFileInfo = await _mediaDownloaderService.DownloadAudio(Jobs[jobID].Data.MediaUrl, Jobs[jobID].Configuration.FinalAudioFormat, Jobs[jobID].CancellationTokenSource.Token, downloadProgress);
					if (downloadedFileInfo != null)
					{
						// Load the file in ATL
						Track audioTrack = new Track(downloadedFileInfo.FullName);
						// Ensure ATL support at least one metadata format for the audio that can be overwriten
						// (Prevent errors since some metadata formats supported by ATL are read-only)
						IList<Format> audioTrackSupportedFormats = audioTrack.SupportedMetadataFormats;
						if (audioTrackSupportedFormats.Any(format => format.Writable == true))
						{
							// Create a progress update, represent 10% of the Job progress
						    int beforeMetadataOverwriteProgressValue = Jobs[jobID].Progress; // Freeze current progress to prevent adding more than +10%
							IProgress<float> metadataOverwriteProgress = new Progress<float>(progress =>
							{
								// Change progress (0.???) to a 0-10 scale
								int currentProgress = (int)(progress * 10);
								int totalProgress = currentProgress + beforeMetadataOverwriteProgressValue;
								// Ensure the new progress is bigger than current (freezed) job progress
								if (totalProgress > Jobs[jobID].Progress) 
								{
									Jobs[jobID].ReportStatus(JobStatus.Running, "Overwriting audio metadata...", totalProgress);
								}
							});
							// Set the new file metadata if the user gived new values
							audioTrack.Title = string.IsNullOrEmpty(Jobs[jobID].Data.Name) ? audioTrack.Title : Jobs[jobID].Data.Name;
							audioTrack.Artist = string.IsNullOrEmpty(Jobs[jobID].Data?.ArtistName) ? audioTrack.Artist : Jobs[jobID].Data.ArtistName;
							audioTrack.Album = string.IsNullOrEmpty(Jobs[jobID].Data?.AlbumName) ? audioTrack.Album : Jobs[jobID].Data.AlbumName;
							audioTrack.Year = Jobs[jobID].Data?.ReleaseYear == null ? audioTrack.Year : Jobs[jobID].Data.ReleaseYear;
							audioTrack.Genre = string.IsNullOrEmpty(Jobs[jobID].Data?.Genre) ? audioTrack.Genre : Jobs[jobID].Data.Genre;
							audioTrack.TrackNumber = Jobs[jobID].Data?.TrackNumber == null ? audioTrack.TrackNumber : Jobs[jobID].Data.TrackNumber;
							audioTrack.Description = string.IsNullOrEmpty(Jobs[jobID].Data?.Description) ? audioTrack.Description : Jobs[jobID].Data.Description;
							audioTrack.Comment = string.IsNullOrEmpty(Jobs[jobID].Data?.Description) ? audioTrack.Description : Jobs[jobID].Data.Description;
							if (Jobs[jobID].Configuration.Lyrics != null)
							{
								// Overwrite media lyrics with empty one / create lyrics
								audioTrack.Lyrics = new LyricsInfo
								{
									ContentType = LyricsInfo.LyricsType.LYRICS,
								};

								// If all lyrics are >= 0, process the lyrics as synced
								bool isSyncLyrics = Jobs[jobID].Configuration.Lyrics!.All(lyric => lyric.Time != null && lyric.Time >= 0);
								if (isSyncLyrics)
								{
									// Add synced lyrics
									Jobs[jobID].Configuration.Lyrics!.ForEach(lyric => audioTrack.Lyrics.SynchronizedLyrics.Add(new LyricsInfo.LyricsPhrase(lyric.Time!.Value, lyric.Content)));
								}
								else 
								{
									// Add lyrics
									IEnumerable<string> lyrics = Jobs[jobID].Configuration.Lyrics!.Select(lyric => lyric.Content);
									audioTrack.Lyrics.UnsynchronizedLyrics = string.Join(Environment.NewLine, lyrics);
								}
							}

							bool wasNewMetadataApplyed = await audioTrack.SaveAsync(metadataOverwriteProgress);
							// Log a warning if ATL failed to overwrite the downloaded audio
							if (!wasNewMetadataApplyed)
							{
								_logger.LogWarning("Job ID: '{ID}'. Tried to overwrite the downloaded audio file metadata using the ALT library but failed. More information about the issue should be available in prior logs.", jobID);
							}
						}
						else _logger.LogWarning("Job ID: '{ID}'. Skipped overwriting metadata using the ALT library since no writable metadata formats where supported for the current audio.", jobID); ;

						// Set file permission for linux based systems
						if (OperatingSystem.IsLinux()) downloadedFileInfo.UnixFileMode = (UnixFileMode.OtherRead | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.UserRead | UnixFileMode.UserWrite);
						// Apply sub directory path format to the output directory if configured
						string outputDirectory = _appSettings.OUTPUT_DIRECTORY;
						if (!string.IsNullOrEmpty(_appSettings.OUTPUT_SUB_DIRECTORY_FORMAT)) 
						{
							outputDirectory = Path.Combine(outputDirectory,_appSettings.OUTPUT_SUB_DIRECTORY_FORMAT
								.Replace("|NOW_YEAR|", DateTime.Now.Year.ToString())
								.Replace("|NOW_MONTH|", DateTime.Now.Month.ToString())
								.Replace("|NOW_DAY|", DateTime.Now.Day.ToString())
								// Here audioTrack has the overwritten value (Jobs[jobID].Data.*) if the field was defined, else the original file values
								.Replace("|AUDIO_ARTIST|", audioTrack.Artist ?? "Unknown")
								.Replace("|AUDIO_ALBUM|", audioTrack.Album ?? "Unknown")
								.Replace("|AUDIO_YEAR|", (audioTrack.Year ?? 0).ToString()));
						}
						// Ensure downloaded audio file name is unique
						string finalAudioPath = Path.Combine(outputDirectory, downloadedFileInfo.Name);
						if (File.Exists(finalAudioPath)) 
						{
							string newAudioName = $"{Path.GetFileNameWithoutExtension(finalAudioPath)}_{DateTimeOffset.Now.ToUnixTimeSeconds()}{Path.GetExtension(finalAudioPath)}";
							finalAudioPath = Path.Combine(outputDirectory, newAudioName);
							_logger.LogWarning("Job ID: '{ID}'. A audio file with the same name ('{audioName}') already exist in the output folder. Appended current timestamp to the downloaded audio name (now '{newAudioName}').", jobID, downloadedFileInfo.Name, newAudioName);
						}
						// Ensure the final output directory exists
						Directory.CreateDirectory(outputDirectory);
						// Move audio file to the output directory (and rename the file to the file name in the path)
						downloadedFileInfo.MoveTo(finalAudioPath);
					}
					else 
					{
						_logger.LogWarning("Job ID: '{ID}'. The scraper did not return the downloaded file path. Error could be due to the scraper not finding any formats/media on the target webpage. (the formats key is empty?)", jobID);
						Jobs[jobID].ReportStatus(JobStatus.Failed, $"Could not locate downloaded file. Did the scraper found any formats? Is webpage supported?", 100);
					}
				}
				// Ignore canceledException when it's thrown due to the cancel signal on our cancellationToken
				catch (OperationCanceledException) when (Jobs[jobID].CancellationTokenSource.IsCancellationRequested) { }
				catch (Exception ex)
				{
					didJobFailedDueToThrownException = true;
					// If it's a error related to the scraper (yt-dlp)
					if (ex is ProcessedScraperErrorOutputException)
					{
						ProcessedScraperErrorOutputException processedEx = (ProcessedScraperErrorOutputException)ex;
						if (processedEx.IsKnownError)
						{
							_logger.LogWarning("Known scraper error occurred during during execution of Job ID : '{ID}'. Error: {message}", jobID, ex.Message);
							Jobs[jobID].ReportStatus(JobStatus.Failed, $"Known scraper error occurred: {ex.Message}", 100);
						}
						else 
						{
							_logger.LogError("An unexpected scraper error occurred during the execution of Job ID : '{ID}'. Error: {message}", jobID, ex.Message);
							Jobs[jobID].ReportStatus(JobStatus.Failed, "An unexpected scraper error occurred", 100);
						}
					}
					else
					{
						_logger.LogError(ex, "An unexpected error occurred during the execution of Job ID : '{ID}'. Error: {message}'", jobID, ex.Message);
						Jobs[jobID].ReportStatus(JobStatus.Failed, "An unexpected error occurred", 100);
					}
				}
				finally
				{
					// Handle final status when the jobs exit it's execution
					if (Jobs[jobID].CancellationTokenSource.IsCancellationRequested) // Cancellation token triggered, job cancelled
					{
						_logger.LogInformation("Job ID: '{ID}' was cancelled during execution.", jobID);
						Jobs[jobID].ReportStatus(JobStatus.Cancelled, "Cancellation was requested", 100);
					}
					else if (Jobs[jobID].Status == JobStatus.Failed) 
					{
						if (didJobFailedDueToThrownException) 
						{
							_logger.LogInformation("Job ID: '{ID}' failed during execution due to a exception.", jobID);
						} else _logger.LogInformation("Job ID: '{ID}' was manually set to failed state during execution.", jobID);

					}
					else if (Jobs[jobID].Status == JobStatus.Running && !Jobs[jobID].CancellationTokenSource.IsCancellationRequested)
					{
						_logger.LogInformation("Job ID: '{ID}' finished execution.", Jobs[jobID].ID);
						TimeSpan jobDuration = DateTime.Now - Jobs[jobID].CreationDate;
						Jobs[jobID].ReportStatus(JobStatus.Completed, $"Completed in {jobDuration.Hours}h:{jobDuration.Minutes}m:{jobDuration.Seconds}s", 100);
					}
					// Release a place inside the semaphore to allow a new job to start
					JobsSemaphore.Release();
				}
			}
			catch (OperationCanceledException)
			{
				// If the CancellationToken is triggered before execution, change the job status to cancelled.
				Jobs[jobID].ReportStatus(JobStatus.Cancelled, "Cancellation was requested", 100);
				_logger.LogInformation("Job ID: '{ID}' was cancelled before execution.", jobID);
			}
		}
	}
}

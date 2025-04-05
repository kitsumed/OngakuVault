using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using OngakuVault.Services;

namespace OngakuVault.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class JobController : ControllerBase
	{
		readonly ILogger<JobController> _logger;
		readonly IJobService _jobService;
		public JobController(ILogger<JobController> logger, IJobService jobService)
        {
			_logger = logger;
			_jobService = jobService;
        }

		[HttpPost("create")]
		[EndpointDescription(@"Use this endpoint to create a new audio download job. Fields with non-empty value will be written in the audio metadata.
						Empty fields will retain the original metadata.")]
		[ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(JobModel))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		public ActionResult CreateJob(JobRESTCreationModel newJobRESTCreationData)
		{
			// Verify if the url is not http/https
			if (!Helpers.UrlHelper.IsUrlUsingHttpScheme(newJobRESTCreationData.MediaInfo.MediaUrl))
			{
				return BadRequest("The mediaUrl inside mediaInfo can only be a scheme of type http or https.");
			}
			// Create a new job JobModel
			JobModel jobModel = _jobService.CreateJob(newJobRESTCreationData);
			// Add the job to the Jobs service execution queue
			if (_jobService.TryAddJobToQueue(jobModel))
			{
				return Accepted(jobModel);
			}
			else 
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while adding the job to the JobService queue.");
			}
		}

		[HttpGet("all")]
		[EndpointDescription(@"Return a list of all jobs that have been queued in the JobService.
					NOTE: You can also register to the websocket endpoint at '/ws' to get live jobs report.")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<JobModel>))]
		[Produces("application/json")]
		public ActionResult GetJobs()
		{
			return Ok(_jobService.GetJobs());
		}

		[HttpGet("info/{ID}")]
		[EndpointDescription("Get informations about a specific Job using its ID")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JobModel))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		public ActionResult GetJobByID(string ID)
		{
			// Verify if the Job ID exist
			if (_jobService.TryGetJobByID(ID, out JobModel? jobModel)) 
			{
				return Ok(jobModel);
			}
			return NotFound("Failed to find a job with the requested ID.");
		}

		[HttpDelete("cancel/{ID}")]
		[EndpointDescription(@"If the job is waiting execution or executing, this will send a cancel signal to the job matching a ID.
			If the job is no longer waiting to be executed and finished running, this will remove the job from the server memory (JobService).")]
		[ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
		[Produces("text/plain")]
		public ActionResult CancelJob(string ID)
		{
			// Verify if the Job ID exist
			if (_jobService.TryGetJobByID(ID, out JobModel? jobModel) && jobModel != null)
			{
				// Verify the job should be removed from server memory (already completed execution)
				if (jobModel.Status == JobStatus.Completed || jobModel.Status == JobStatus.Cancelled || jobModel.Status == JobStatus.Failed)
				{
					if (_jobService.TryRemoveJobFromQueue(jobModel)) 
					{
						return Accepted(string.Empty, "The job was removed from the server memory.");
					}
					return Conflict("Failed to remove the job from the server memory, try again later.");
				}
				else // Send a cancel signal to the job
				{
					if (jobModel.CancellationTokenSource.IsCancellationRequested)
					{
						return Conflict("This job cancel signal was already triggered. Please wait for the job execution to exit.");
					}
					jobModel.CancellationTokenSource.Cancel();
					return Accepted(string.Empty, "A cancel signal has been sent to the job.");
				}
			}
			return NotFound("Failed to find a job with the requested ID.");
		}
	}
}

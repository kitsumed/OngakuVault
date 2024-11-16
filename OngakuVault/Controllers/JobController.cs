using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OngakuVault.Models;
using OngakuVault.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

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
		[EndpointDescription("Create a new download job for a song")]
		[ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(JobModel))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		public ActionResult CreateJob(JobModelCreate jobModelCreate)
		{
			// Verify if the url is http/https
			_ = Uri.TryCreate(jobModelCreate.originalUrl, UriKind.Absolute, out Uri? originalUrlUri);
			if (originalUrlUri?.Scheme != Uri.UriSchemeHttp && originalUrlUri?.Scheme != Uri.UriSchemeHttps && originalUrlUri != null)
			{
				return BadRequest("originalUrl scheme can only be http or https.");
			}
			// Convert the JobModelCreate to a JobModel
			JobModel jobModel = new JobModel(jobModelCreate);
			// Add the jobModel to the list of jobs
			if (_jobService.TryAddJob(jobModel))
			{
				return Accepted(jobModel);
			}
			else 
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while adding the job to the Jobs list.");
			}
		}

		[HttpGet("all")]
		[EndpointDescription("Return a list of all JobModel")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<JobModel>))]
		[Produces("application/json")]
		public ActionResult GetJobs()
		{
			return Ok(_jobService.GetJobs());
		}

		[HttpGet("{ID}/info")]
		[EndpointDescription("Get information about the Job matching the ID")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JobModel))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		[Produces("application/json", "text/plain")]
		public ActionResult GetJobByID(string ID)
		{
			// Verify if the Job ID exist
			JobModel? jobModel = _jobService.TryGetJob(ID);
			if (jobModel != null) 
			{
				return Ok(jobModel);
			}
			return NotFound("Failed to find a job with the requested ID.");
		}

		[HttpDelete("{ID}/cancel")]
		[EndpointDescription("Cancel the job matching this ID")]
		[ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		[ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
		[Produces("text/plain")]
		public ActionResult CancelJob(string ID)
		{
			// Verify if the Job ID exist
			JobModel? jobModel = _jobService.TryGetJob(ID);
			if (jobModel != null)
			{
				if (jobModel.CancellationTokenSource.IsCancellationRequested) 
				{
					return Conflict("This job cancel signal was already triggered.");
				}
				jobModel.CancellationTokenSource.Cancel();
				return Accepted(string.Empty, "Cancel signal has been sent to this job.");
			}
			return NotFound("Failed to find a job with the requested ID.");
		}
	}
}

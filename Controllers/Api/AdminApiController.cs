using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using FileSharePortal.Data;
using FileSharePortal.Filters;
using FileSharePortal.Models;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers.Api
{
    [RoutePrefix("api/admin")]
    [ApiAuthentication(RequireAdmin = true)]
    public class AdminApiController : BaseApiController
    {
        private readonly FileSharePortalContext _context;
        private readonly ADSyncService _adSyncService;

        public AdminApiController()
        {
            _context = new FileSharePortalContext();
            _adSyncService = new ADSyncService();
        }

        /// <summary>
        /// Trigger Active Directory synchronization
        /// POST /api/admin/sync-ad
        /// </summary>
        [HttpPost]
        [Route("sync-ad")]
        public IHttpActionResult SyncActiveDirectory()
        {
            try
            {
                var result = _adSyncService.SynchronizeADUsers();

                return Ok(new
                {
                    message = "AD synchronization completed successfully",
                    usersAdded = result.UsersAdded,
                    usersUpdated = result.UsersUpdated,
                    usersReactivated = result.UsersReactivated,
                    usersDisabled = result.UsersDisabled
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Register a new application for API access
        /// POST /api/admin/applications/register
        /// Body: { "applicationName": "MyApp", "description": "...", "contactEmail": "..." }
        /// </summary>
        [HttpPost]
        [Route("applications/register")]
        public IHttpActionResult RegisterApplication([FromBody] RegisterApplicationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ApplicationName))
            {
                return BadRequest("Application name is required");
            }

            // Check if application name already exists
            if (_context.Applications.Any(a => a.ApplicationName == request.ApplicationName))
            {
                return BadRequest("An application with this name already exists");
            }

            // Generate API key
            var apiKey = GenerateApiKey();

            var application = new Application
            {
                ApplicationName = request.ApplicationName,
                Description = request.Description,
                ContactEmail = request.ContactEmail,
                ApiKey = apiKey,
                RegisteredByUserId = CurrentUser.UserId,
                RegisteredDate = DateTime.Now,
                IsActive = true,
                CurrentStatus = ApplicationStatus.Unknown
            };

            _context.Applications.Add(application);
            _context.SaveChanges();

            return Ok(new
            {
                applicationId = application.ApplicationId,
                applicationName = application.ApplicationName,
                apiKey = apiKey,
                registeredDate = application.RegisteredDate,
                message = "Application registered successfully. Please store the API key securely."
            });
        }

        /// <summary>
        /// Get list of all registered applications
        /// GET /api/admin/applications
        /// </summary>
        [HttpGet]
        [Route("applications")]
        public IHttpActionResult GetApplications()
        {
            var applications = _context.Applications
                .OrderByDescending(a => a.RegisteredDate)
                .Select(a => new
                {
                    applicationId = a.ApplicationId,
                    applicationName = a.ApplicationName,
                    description = a.Description,
                    contactEmail = a.ContactEmail,
                    registeredDate = a.RegisteredDate,
                    isActive = a.IsActive,
                    currentStatus = a.CurrentStatus.ToString(),
                    lastSuccessfulRun = a.LastSuccessfulRun,
                    lastStatusCheck = a.LastStatusCheck
                })
                .ToList();

            return Ok(applications);
        }

        /// <summary>
        /// Update last successful run date for an application
        /// PUT /api/admin/applications/{id}/last-run
        /// Body: { "lastRunDate": "2025-01-01T12:00:00" }
        /// </summary>
        [HttpPut]
        [Route("applications/{id}/last-run")]
        public IHttpActionResult UpdateLastSuccessfulRun(int id, [FromBody] UpdateLastRunRequest request)
        {
            var application = _context.Applications.Find(id);

            if (application == null)
            {
                return NotFound();
            }

            application.LastSuccessfulRun = request.LastRunDate ?? DateTime.Now;
            _context.SaveChanges();

            return Ok(new
            {
                applicationId = application.ApplicationId,
                applicationName = application.ApplicationName,
                lastSuccessfulRun = application.LastSuccessfulRun,
                message = "Last successful run updated"
            });
        }

        /// <summary>
        /// Create a new execution record for an application
        /// POST /api/admin/applications/{id}/executions
        /// Body: { "status": "Running", "statusMessage": "..." }
        /// </summary>
        [HttpPost]
        [Route("applications/{id}/executions")]
        public IHttpActionResult CreateExecution(int id, [FromBody] CreateExecutionRequest request)
        {
            var application = _context.Applications.Find(id);

            if (application == null)
            {
                return NotFound();
            }

            var execution = new ApplicationExecution
            {
                ApplicationId = id,
                StartTime = DateTime.Now,
                Status = ParseApplicationStatus(request.Status),
                ExecutionDetails = request.StatusMessage,
                ExecutedByUserId = CurrentUser.UserId
            };

            _context.ApplicationExecutions.Add(execution);
            _context.SaveChanges();

            return Ok(new
            {
                executionId = execution.ExecutionId,
                applicationId = execution.ApplicationId,
                startTime = execution.StartTime,
                status = execution.Status.ToString(),
                message = "Execution record created"
            });
        }

        /// <summary>
        /// Update an existing execution record
        /// PUT /api/admin/executions/{id}
        /// Body: { "status": "Success", "statusMessage": "...", "endTime": "..." }
        /// </summary>
        [HttpPut]
        [Route("executions/{id}")]
        public IHttpActionResult UpdateExecution(int id, [FromBody] UpdateExecutionRequest request)
        {
            var execution = _context.ApplicationExecutions.Find(id);

            if (execution == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                execution.Status = ParseApplicationStatus(request.Status);
            }

            if (!string.IsNullOrEmpty(request.StatusMessage))
            {
                execution.ExecutionDetails = request.StatusMessage;
            }

            if (request.EndTime.HasValue)
            {
                execution.EndTime = request.EndTime.Value;
            }

            if (request.RecordsProcessed.HasValue)
            {
                execution.RecordsProcessed = request.RecordsProcessed.Value;
            }

            _context.SaveChanges();

            // Update application's last successful run if execution succeeded
            if (execution.Status == ApplicationStatus.Running && request.Status == "Success")
            {
                execution.Application.LastSuccessfulRun = DateTime.Now;
                _context.SaveChanges();
            }

            return Ok(new
            {
                executionId = execution.ExecutionId,
                status = execution.Status.ToString(),
                endTime = execution.EndTime,
                message = "Execution updated successfully"
            });
        }

        /// <summary>
        /// Upload a log file for an execution
        /// POST /api/admin/executions/{id}/logs
        /// Content-Type: multipart/form-data
        /// </summary>
        [HttpPost]
        [Route("executions/{id}/logs")]
        public async Task<IHttpActionResult> UploadLogFile(int id)
        {
            var execution = _context.ApplicationExecutions.Find(id);

            if (execution == null)
            {
                return NotFound();
            }

            if (!Request.Content.IsMimeMultipartContent())
            {
                return BadRequest("Unsupported media type. Use multipart/form-data");
            }

            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                if (provider.Contents.Count == 0)
                {
                    return BadRequest("No file uploaded");
                }

                var fileContent = provider.Contents[0];
                var fileName = fileContent.Headers.ContentDisposition.FileName.Trim('"');
                var contentType = fileContent.Headers.ContentType.MediaType;
                var fileBytes = await fileContent.ReadAsByteArrayAsync();

                // Get description from form data if provided
                var description = provider.Contents.Count > 1
                    ? await provider.Contents[1].ReadAsStringAsync()
                    : null;

                var logFile = new ApplicationLogFile
                {
                    ExecutionId = id,
                    FileName = fileName,
                    ContentType = contentType,
                    FileSize = fileBytes.Length,
                    FileContent = fileBytes,
                    Description = description,
                    UploadedDate = DateTime.Now
                };

                _context.ApplicationLogFiles.Add(logFile);
                _context.SaveChanges();

                return Ok(new
                {
                    logFileId = logFile.LogFileId,
                    fileName = logFile.FileName,
                    fileSize = logFile.FileSize,
                    uploadedDate = logFile.UploadedDate,
                    message = "Log file uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get log files for an execution
        /// GET /api/admin/executions/{id}/logs
        /// </summary>
        [HttpGet]
        [Route("executions/{id}/logs")]
        public IHttpActionResult GetExecutionLogs(int id)
        {
            var execution = _context.ApplicationExecutions.Find(id);

            if (execution == null)
            {
                return NotFound();
            }

            var logs = _context.ApplicationLogFiles
                .Where(l => l.ExecutionId == id)
                .OrderBy(l => l.UploadedDate)
                .Select(l => new
                {
                    logFileId = l.LogFileId,
                    fileName = l.FileName,
                    fileSize = l.FileSize,
                    contentType = l.ContentType,
                    uploadedDate = l.UploadedDate,
                    description = l.Description
                })
                .ToList();

            return Ok(logs);
        }

        /// <summary>
        /// Download a log file
        /// GET /api/admin/logs/{id}/download
        /// </summary>
        [HttpGet]
        [Route("logs/{id}/download")]
        public IHttpActionResult DownloadLogFile(int id)
        {
            var logFile = _context.ApplicationLogFiles.Find(id);

            if (logFile == null)
            {
                return NotFound();
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(logFile.FileContent);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(logFile.ContentType);
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = logFile.FileName
            };

            return ResponseMessage(response);
        }

        /// <summary>
        /// Get executions for an application
        /// GET /api/admin/applications/{id}/executions
        /// </summary>
        [HttpGet]
        [Route("applications/{id}/executions")]
        public IHttpActionResult GetApplicationExecutions(int id, [FromUri] int? limit = 50)
        {
            var application = _context.Applications.Find(id);

            if (application == null)
            {
                return NotFound();
            }

            var executions = _context.ApplicationExecutions
                .Where(e => e.ApplicationId == id)
                .OrderByDescending(e => e.StartTime)
                .Take(limit.Value)
                .Select(e => new
                {
                    executionId = e.ExecutionId,
                    startTime = e.StartTime,
                    endTime = e.EndTime,
                    status = e.Status.ToString(),
                    statusMessage = e.ExecutionDetails,
                    recordsProcessed = e.RecordsProcessed,
                    executedBy = e.ExecutedBy != null ? e.ExecutedBy.FullName : null,
                    logFileCount = e.LogFiles.Count
                })
                .ToList();

            return Ok(executions);
        }

        private string GenerateApiKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var keyData = new byte[32];
                rng.GetBytes(keyData);
                return Convert.ToBase64String(keyData).Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
        }

        private ApplicationStatus ParseApplicationStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return ApplicationStatus.Unknown;

            switch (status.ToLower())
            {
                case "running":
                    return ApplicationStatus.Running;
                case "stopped":
                case "success":
                    return ApplicationStatus.Stopped;
                case "error":
                case "failed":
                    return ApplicationStatus.Error;
                default:
                    return ApplicationStatus.Unknown;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _adSyncService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class RegisterApplicationRequest
    {
        public string ApplicationName { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
    }

    public class UpdateLastRunRequest
    {
        public DateTime? LastRunDate { get; set; }
    }

    public class CreateExecutionRequest
    {
        public string Status { get; set; }
        public string StatusMessage { get; set; }
    }

    public class UpdateExecutionRequest
    {
        public string Status { get; set; }
        public string StatusMessage { get; set; }
        public DateTime? EndTime { get; set; }
        public int? RecordsProcessed { get; set; }
    }
}

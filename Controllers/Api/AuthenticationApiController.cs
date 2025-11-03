using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
//using System.Web.Mvc;
using FileSharePortal.Filters;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers.Api
{
    [RoutePrefix("api/auth")]
    public class AuthenticationApiController : ApiController
    {
        private readonly ApiTokenService _tokenService;

        public AuthenticationApiController()
        {
            _tokenService = new ApiTokenService();
        }

        /// <summary>
        /// Authenticate with username and password
        /// POST /api/auth/connect
        /// Body: { "username": "user", "password": "pass" }
        /// </summary>
        [HttpPost]
        [Route("connect")]
        public IHttpActionResult Connect([FromBody] UserAuthRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            var ipAddress = GetClientIpAddress();
            var result = _tokenService.AuthenticateUser(request.Username, request.Password, ipAddress);

            if (!result.Success)
            {
                return Content(HttpStatusCode.Unauthorized, new { error = result.Message });
            }

            return Ok(new
            {
                token = result.Token,
                expiresDate = result.ExpiresDate,
                user = new
                {
                    userId = result.User.UserId,
                    username = result.User.Username,
                    fullName = result.User.FullName,
                    email = result.User.Email,
                    isAdmin = result.User.IsAdmin
                }
            });
        }

        /// <summary>
        /// Authenticate as an application using API key
        /// POST /api/auth/authenticate
        /// Body: { "applicationName": "MyApp", "apiKey": "key" }
        /// </summary>
        [HttpPost]
        [Route("authenticate")]
        public IHttpActionResult Authenticate([FromBody] ApplicationAuthRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ApplicationName) || string.IsNullOrEmpty(request.ApiKey))
            {
                return BadRequest("Application name and API key are required");
            }

            var ipAddress = GetClientIpAddress();
            var result = _tokenService.AuthenticateApplication(request.ApplicationName, request.ApiKey, ipAddress);

            if (!result.Success)
            {
                return Content(HttpStatusCode.Unauthorized, new { error = result.Message });
            }

            return Ok(new
            {
                token = result.Token,
                expiresDate = result.ExpiresDate,
                applicationId = result.ApplicationId,
                user = new
                {
                    userId = result.User.UserId,
                    username = result.User.Username,
                    fullName = result.User.FullName,
                    email = result.User.Email,
                    isAdmin = result.User.IsAdmin
                }
            });
        }

        /// <summary>
        /// Revoke the current token
        /// POST /api/auth/revoke
        /// Header: Authorization: Bearer {token}
        /// </summary>
        [HttpPost]
        [Route("revoke")]
        [ApiAuthentication]
        public IHttpActionResult Revoke()
        {
            var authHeader = Request.Headers.Authorization;
            if (authHeader != null && authHeader.Scheme == "Bearer")
            {
                var token = authHeader.Parameter;
                var success = _tokenService.RevokeToken(token);

                if (success)
                {
                    return Ok(new { message = "Token revoked successfully" });
                }
            }

            return BadRequest("Failed to revoke token");
        }

        private string GetClientIpAddress()
        {
            if (Request.Properties.ContainsKey("MS_HttpContext"))
            {
                var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                if (context != null)
                {
                    return context.Request.UserHostAddress;
                }
            }
            return "Unknown";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tokenService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class UserAuthRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ApplicationAuthRequest
    {
        public string ApplicationName { get; set; }
        public string ApiKey { get; set; }
    }
}

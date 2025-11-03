using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using FileSharePortal.Services;

namespace FileSharePortal.Filters
{
    public class ApiAuthenticationAttribute : AuthorizationFilterAttribute
    {
        public bool RequireAdmin { get; set; } = false;

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var authHeader = actionContext.Request.Headers.Authorization;

            if (authHeader == null || authHeader.Scheme != "Bearer")
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    new { error = "Missing or invalid authorization header. Use 'Bearer {token}'" });
                return;
            }

            var token = authHeader.Parameter;

            using (var tokenService = new ApiTokenService())
            {
                var validationResult = tokenService.ValidateToken(token);

                if (!validationResult.IsValid)
                {
                    actionContext.Response = actionContext.Request.CreateResponse(
                        HttpStatusCode.Unauthorized,
                        new { error = validationResult.Message });
                    return;
                }

                // Check if admin is required
                if (RequireAdmin && !validationResult.User.IsAdmin)
                {
                    actionContext.Response = actionContext.Request.CreateResponse(
                        HttpStatusCode.Forbidden,
                        new { error = "This action requires administrator privileges" });
                    return;
                }

                // Store user and application ID in request properties for later use
                actionContext.Request.Properties["User"] = validationResult.User;
                if (validationResult.ApplicationId.HasValue)
                {
                    actionContext.Request.Properties["ApplicationId"] = validationResult.ApplicationId.Value;
                }
            }

            base.OnAuthorization(actionContext);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Security;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using FileSharePortal.Services;
using log4net;

namespace FileSharePortal.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(AccountController));
        private readonly AuthenticationService _authService;

        public AccountController()
        {
            _authService = new AuthenticationService();
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            var parameters = new Dictionary<string, object>
            {
                { "returnUrl", returnUrl }
            };
            try
            {
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, bool? rememberMe, string returnUrl)
        {
            var parameters = new Dictionary<string, object>
            {
                { "username", username },
                { "password", password },
                { "rememberMe", rememberMe },
                { "returnUrl", returnUrl }
            };
            var maskedParams = LoggingHelper.MaskSensitiveData(parameters);
            Logger.Info($"Login (POST) called - Username: {username}, RememberMe: {rememberMe}, ReturnUrl: {returnUrl}");
            LoggingHelper.LogWithParams(Logger, "Login (POST)", username, "***PASSWORD***", rememberMe, returnUrl);

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    Logger.Warn($"Login failed: Empty username or password for user: {username}");
                    ViewBag.Error = "Please enter both username and password.";
                    return View();
                }

                var user = _authService.AuthenticateUser(username, password);

                if (user != null)
                {
                    Logger.Info($"Authentication successful for user: {username} (UserId: {user.UserId})");
                    _authService.SetAuthenticationTicket(user, rememberMe.HasValue ? rememberMe.Value : false);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                Logger.Warn($"Authentication failed for user: {username}");
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error during login for user: {username}", ex);
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Logger.Info($"Logout called for user: {User.Identity.Name}");

            try
            {
                FormsAuthentication.SignOut();
                Session.Clear();
                Logger.Info($"User {User.Identity.Name} logged out successfully");
                
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error during logout", ex);
                throw;
            }
        }

        public ActionResult AccessDenied()
        {
            Logger.Warn($"Access denied page accessed by user: {User.Identity.Name ?? "Anonymous"}");

            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error in AccessDenied", ex);
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {

            try
            {
                if (disposing)
                {
                    _authService?.Dispose();
                }
                base.Dispose(disposing);
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error during dispose", ex);
                throw;
            }
        }
    }
}

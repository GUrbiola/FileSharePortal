using System.Web.Mvc;
using System.Web.Security;
using FileSharePortal.Models;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AuthenticationService _authService;

        public AccountController()
        {
            _authService = new AuthenticationService();
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, bool? rememberMe, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both username and password.";
                return View();
            }

            var user = _authService.AuthenticateUser(username, password);

            if (user != null)
            {
                _authService.SetAuthenticationTicket(user, rememberMe.HasValue ? rememberMe.Value : false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        public ActionResult AccessDenied()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _authService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

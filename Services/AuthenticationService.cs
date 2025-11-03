using System;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;
using FileSharePortal.Data;
using FileSharePortal.Models;

namespace FileSharePortal.Services
{
    public class AuthenticationService
    {
        private readonly FileSharePortalContext _context;
        private readonly bool _useActiveDirectory;
        private readonly string _adDomain;

        public AuthenticationService()
        {
            _context = new FileSharePortalContext();
            _useActiveDirectory = ConfigurationManager.AppSettings["UseActiveDirectory"] == "true";
            _adDomain = ConfigurationManager.AppSettings["ADDomain"];
        }

        public User AuthenticateUser(string username, string password)
        {
            string adUserName = String.Empty;
            if(username.Contains("@ff.com"))
            {
                adUserName = username.Split('@')[0];
            }

            if (_useActiveDirectory && !string.IsNullOrEmpty(adUserName))
            {
                return AuthenticateActiveDirectoryUser(adUserName, password);
            }
            else
            {
                return AuthenticateDatabaseUser(username, password);
            }
        }

        private User AuthenticateActiveDirectoryUser(string username, string password)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _adDomain))
                {
                    if (context.ValidateCredentials(username, password))
                    {
                        var userPrincipal = UserPrincipal.FindByIdentity(context, username);
                        if (userPrincipal != null)
                        {
                            // Check if user exists in database
                            var user = _context.Users.FirstOrDefault(u => u.Username == username);

                            if (user == null)
                            {
                                // Auto-create user from AD
                                user = new User
                                {
                                    Username = username,
                                    FullName = userPrincipal.DisplayName ?? username,
                                    Email = userPrincipal.EmailAddress ?? $"{username}@{_adDomain}",
                                    IsFromActiveDirectory = true,
                                    IsActive = true,
                                    IsAdmin = false
                                };
                                _context.Users.Add(user);
                                _context.SaveChanges();
                            }

                            user.LastLoginDate = DateTime.Now;
                            _context.SaveChanges();

                            return user;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"AD Authentication error: {ex.Message}");
            }

            return null;
        }

        private User AuthenticateDatabaseUser(string username, string password)
        {
            var hashedPassword = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u =>
                u.Username == username &&
                u.PasswordHash == hashedPassword &&
                u.IsActive);

            if (user != null)
            {
                user.LastLoginDate = DateTime.Now;
                _context.SaveChanges();
            }

            return user;
        }

        public void SetAuthenticationTicket(User user, bool persistCookie = false)
        {
            var ticket = new FormsAuthenticationTicket(
                1,
                user.Username,
                DateTime.Now,
                DateTime.Now.AddHours(8),
                persistCookie,
                user.UserId.ToString(),
                FormsAuthentication.FormsCookiePath
            );

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath
            };

            if (persistCookie)
            {
                cookie.Expires = ticket.Expiration;
            }

            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public User GetCurrentUser()
        {
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                var username = HttpContext.Current.User.Identity.Name;
                return _context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
            }

            return null;
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        public static bool VerifyPassword(string password, string passwordHash)
        {
            string hashedPassword = HashPassword(password);
            return hashedPassword == passwordHash;
        }
    }
}

using System;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;
using FileSharePortal.Data;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using log4net;

namespace FileSharePortal.Services
{
    public class AuthenticationService
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(AuthenticationService));
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
            Logger.Info($"Attempting to authenticate user: {username}");

            try
            {
                string adUserName = String.Empty;

                if (username.Contains("@ff.com"))
                {
                    adUserName = username.Split('@')[0];
                    Logger.Debug($"Extracted AD username: {adUserName}");
                }

                if (_useActiveDirectory && !string.IsNullOrEmpty(adUserName))
                {
                    Logger.Info($"Using Active Directory authentication for user: {adUserName}");
                    var result = AuthenticateActiveDirectoryUser(adUserName, password);
                    return result;
                }
                else
                {
                    Logger.Info($"Using database authentication for user: {username}");
                    var result = AuthenticateDatabaseUser(username, password);
                    return result;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error authenticating user: {username}", ex);
                throw;
            }
        }

        private User AuthenticateActiveDirectoryUser(string username, string password)
        {
            Logger.Info($"Authenticating AD user: {username} against domain: {_adDomain}");

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _adDomain))
                {
                    if (context.ValidateCredentials(username, password))
                    {
                        Logger.Info($"AD credentials validated successfully for user: {username}");

                        var userPrincipal = UserPrincipal.FindByIdentity(context, username);

                        if (userPrincipal != null)
                        {
                            var user = _context.Users.FirstOrDefault(u => u.Username == username);

                            if (user == null)
                            {
                                Logger.Info($"User not found in database, auto-creating AD user: {username}");

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
                            else
                            {
                                Logger.Debug($"User found in database with UserId: {user.UserId}");
                            }

                            user.LastLoginDate = DateTime.Now;
                            _context.SaveChanges();

                            Logger.Info($"AD authentication successful for user: {username} (UserId: {user.UserId})");
                            return user;
                        }
                        else
                        {
                            Logger.Warn($"User principal not found in AD for: {username}");
                        }
                    }
                    else
                    {
                        Logger.Warn($"AD credentials validation failed for user: {username}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"AD Authentication error for user: {username}", ex);
            }

            Logger.Warn($"AD authentication failed for user: {username}");
            return null;
        }

        private User AuthenticateDatabaseUser(string username, string password)
        {
            Logger.Info($"Authenticating database user: {username}");

            try
            {
                var hashedPassword = HashPassword(password);

                var user = _context.Users.FirstOrDefault(u =>
                    u.Username == username &&
                    u.PasswordHash == hashedPassword &&
                    u.IsActive);

                if (user != null)
                {
                    Logger.Info($"Database user found and authenticated: {username} (UserId: {user.UserId})");

                    user.LastLoginDate = DateTime.Now;
                    _context.SaveChanges();
                    return user;
                }
                else
                {
                    Logger.Warn($"Database authentication failed for user: {username} - User not found or inactive");
                }

                return null;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error during database authentication for user: {username}", ex);
                throw;
            }
        }

        public void SetAuthenticationTicket(User user, bool persistCookie = false)
        {
            Logger.Info($"Setting authentication ticket for user: {user?.Username} (Persist: {persistCookie})");

            try
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

                Logger.Debug($"Ticket created - Expiration: {ticket.Expiration}, UserId: {user.UserId}");

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
                    Logger.Debug($"Cookie set to expire at: {cookie.Expires}");
                }

                HttpContext.Current.Response.Cookies.Add(cookie);

                Logger.Info($"Authentication ticket set successfully for user: {user.Username}");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error setting authentication ticket for user: {user?.Username}", ex);
                throw;
            }
        }

        public User GetCurrentUser()
        {
            Logger.Debug($"GetCurrentUser method was called");
            try
            {
                if (HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    var username = HttpContext.Current.User.Identity.Name;
                    Logger.Debug($"User is authenticated: {username}");

                    var user = _context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                    if (user != null)
                    {
                        Logger.Debug($"User found in database: {username} (UserId: {user.UserId})");
                        return user;
                    }
                    else
                    {
                        Logger.Warn($"Authenticated user not found or inactive in database: {username}");
                    }
                }
                else
                {
                    Logger.Debug("No authenticated user");
                }

                return null;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error getting current user", ex);
                throw;
            }
        }

        public static string HashPassword(string password)
        {
            // Note: Static method - no instance logging
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        public void Dispose()
        {
            try
            {
                _context?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error during dispose", ex);
                throw;
            }
        }

        public static bool VerifyPassword(string password, string passwordHash)
        {
            // Note: Static method - no instance logging
            string hashedPassword = HashPassword(password);
            return hashedPassword == passwordHash;
        }
    }
}

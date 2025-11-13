using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FileSharePortal.Data;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using log4net;

namespace FileSharePortal.Services
{
    public class ApiTokenService : IDisposable
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(ApiTokenService));
        private readonly FileSharePortalContext _context;

        public ApiTokenService()
        {
            _context = new FileSharePortalContext();
        }

        /// <summary>
        /// Authenticates a user with username/password and generates a token
        /// </summary>
        public AuthenticationResult AuthenticateUser(string username, string password, string ipAddress, int? applicationId = null)
        {
            LoggingHelper.LogWithParams(Logger, "AuthenticateUser", username, "***PASSWORD***", ipAddress, applicationId);
            Logger.Info($"Authenticating user for API token: {username} from IP: {ipAddress}");

            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                if (user == null)
                {
                    Logger.Warn($"User not found or inactive: {username}");
                    return new AuthenticationResult { Success = false, Message = "Invalid username or password" };
                }

                Logger.Debug($"User found: {username} (UserId: {user.UserId})");

                // Check if user is from AD (AD users can't authenticate via API with password)
                if (user.IsFromActiveDirectory)
                {
                    Logger.Warn($"AD user attempted API authentication: {username}");
                    return new AuthenticationResult { Success = false, Message = "Active Directory users cannot authenticate via API" };
                }

                // Verify password
                if (!AuthenticationService.VerifyPassword(password, user.PasswordHash))
                {
                    Logger.Warn($"Invalid password for user: {username}");
                    return new AuthenticationResult { Success = false, Message = "Invalid username or password" };
                }


                // Generate token
                LoggingHelper.LogWithParams(Logger, $"Generating API token for user: {username}");
                var token = GenerateToken(user.UserId, ipAddress, applicationId);

                Logger.Info($"API token generated successfully for user: {username}");
                return new AuthenticationResult
                {
                    Success = true,
                    Token = token.Token,
                    ExpiresDate = token.ExpiresDate,
                    User = user,
                    Message = "Authentication successful"
                };
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error authenticating user: {username}", ex);
                throw;
            }
        }

        /// <summary>
        /// Authenticates an application using its API key
        /// </summary>
        public AuthenticationResult AuthenticateApplication(string applicationName, string apiKey, string ipAddress)
        {
            LoggingHelper.LogWithParams(Logger, "AuthenticateApplication", applicationName, "***API_KEY***", ipAddress);
            Logger.Info($"Authenticating application: {applicationName} from IP: {ipAddress}");

            try
            {
                var application = _context.Applications.FirstOrDefault(a =>
                    a.ApplicationName == applicationName &&
                    a.ApiKey == apiKey &&
                    a.IsActive);

                if (application == null)
                {
                    Logger.Warn($"Application not found or inactive: {applicationName}");
                    return new AuthenticationResult { Success = false, Message = "Invalid application name or API key" };
                }

                Logger.Debug($"Application found: {applicationName} (ApplicationId: {application.ApplicationId})");

                // Get the user who registered the application
                if (!application.RegisteredByUserId.HasValue)
                {
                    Logger.Warn($"Application {applicationName} has no registered owner");
                    return new AuthenticationResult { Success = false, Message = "Application not properly registered" };
                }

                var user = _context.Users.Find(application.RegisteredByUserId.Value);

                if (user == null || !user.IsActive)
                {
                    Logger.Warn($"Application owner is inactive or not found for: {applicationName}");
                    return new AuthenticationResult { Success = false, Message = "Application owner is inactive" };
                }

                Logger.Debug($"Application owner found and active: {user.Username}");

                // Generate token for the application
                var token = GenerateToken(user.UserId, ipAddress, application.ApplicationId);

                Logger.Info($"API token generated successfully for application: {applicationName}");

                return new AuthenticationResult
                {
                    Success = true,
                    Token = token.Token,
                    ExpiresDate = token.ExpiresDate,
                    User = user,
                    ApplicationId = application.ApplicationId,
                    Message = "Authentication successful"
                };
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error authenticating application: {applicationName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Validates a token and returns the associated user
        /// </summary>
        public TokenValidationResult ValidateToken(string token)
        {
            LoggingHelper.LogWithParams(Logger, "ValidateToken", token?.Substring(0, Math.Min(10, token?.Length ?? 0)) + "...");
            Logger.Debug("Validating API token");

            try
            {
                var apiToken = _context.ApiTokens.FirstOrDefault(t => t.Token == token && !t.IsRevoked);

                if (apiToken == null)
                {
                    Logger.Warn("Token not found or has been revoked");
                    return new TokenValidationResult { IsValid = false, Message = "Invalid token" };
                }

                Logger.Debug($"Token found - UserId: {apiToken.UserId}, Expires: {apiToken.ExpiresDate}");

                if (apiToken.ExpiresDate < DateTime.Now)
                {
                    Logger.Warn($"Token has expired - Expiry date: {apiToken.ExpiresDate}");
                    return new TokenValidationResult { IsValid = false, Message = "Token has expired" };
                }

                // Update last used date
                apiToken.LastUsedDate = DateTime.Now;
                _context.SaveChanges();

                var user = _context.Users.Find(apiToken.UserId);

                if (user == null || !user.IsActive)
                {
                    Logger.Warn($"User is inactive or not found (UserId: {apiToken.UserId})");
                    return new TokenValidationResult { IsValid = false, Message = "User is inactive" };
                }

                Logger.Info($"Token validated successfully for user: {user.Username}");

                return new TokenValidationResult
                {
                    IsValid = true,
                    User = user,
                    ApplicationId = apiToken.ApplicationId,
                    Message = "Token is valid"
                };
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error validating token", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a new API token
        /// </summary>
        private ApiToken GenerateToken(int userId, string ipAddress, int? applicationId = null)
        {
            Logger.Info($"Generating new API token for UserId: {userId}");

            try
            {
                var tokenString = GenerateRandomToken();

                var expiresDate = DateTime.Now.AddDays(30); // Token valid for 30 days
                Logger.Debug($"Token expiration set to: {expiresDate}");

                var apiToken = new ApiToken
                {
                    Token = tokenString,
                    UserId = userId,
                    ApplicationId = applicationId,
                    CreatedDate = DateTime.Now,
                    ExpiresDate = expiresDate,
                    IpAddress = ipAddress,
                    IsRevoked = false
                };

                _context.ApiTokens.Add(apiToken);

                _context.SaveChanges();

                Logger.Info($"API token created successfully - TokenId: {apiToken.TokenId}, Expires: {expiresDate}");
                return apiToken;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error generating token for UserId: {userId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random token
        /// </summary>
        private string GenerateRandomToken()
        {
            Logger.Debug("Generating cryptographically secure random token");

            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    var tokenData = new byte[32];
                    rng.GetBytes(tokenData);

                    var token = Convert.ToBase64String(tokenData);
                    // Make URL-safe
                    token = token.Replace("+", "-").Replace("/", "_").Replace("=", "");

                    Logger.Debug($"Random token generated (length: {token.Length})");

                    return token;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error generating random token", ex);
                throw;
            }
        }

        /// <summary>
        /// Revokes a token
        /// </summary>
        public bool RevokeToken(string token)
        {
            Logger.Info("Revoking API token");

            try
            {
                var apiToken = _context.ApiTokens.FirstOrDefault(t => t.Token == token);

                if (apiToken == null)
                {
                    Logger.Warn("Token not found");
                    return false;
                }

                Logger.Debug($"Token found - TokenId: {apiToken.TokenId}, UserId: {apiToken.UserId}");

                apiToken.IsRevoked = true;

                _context.SaveChanges();

                Logger.Info($"Token revoked successfully - TokenId: {apiToken.TokenId}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error revoking token", ex);
                throw;
            }
        }

        /// <summary>
        /// Revokes all tokens for a user
        /// </summary>
        public int RevokeUserTokens(int userId)
        {
            Logger.Info($"Revoking all tokens for UserId: {userId}");

            try
            {
                var tokens = _context.ApiTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToList();
                Logger.Debug($"Found {tokens.Count} active token(s) for user");

                if (tokens.Count > 0)
                {
                    foreach (var token in tokens)
                    {
                        token.IsRevoked = true;
                    }

                    _context.SaveChanges();

                    Logger.Info($"Revoked {tokens.Count} token(s) for UserId: {userId}");
                }
                else
                {
                    Logger.Debug($"No active tokens found for UserId: {userId}");
                }

                return tokens.Count;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error revoking tokens for UserId: {userId}", ex);
                throw;
            }
        }

        public void Dispose()
        {
            Logger.Debug("Disposing ApiTokenService");

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
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresDate { get; set; }
        public User User { get; set; }
        public int? ApplicationId { get; set; }
        public string Message { get; set; }
    }

    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public User User { get; set; }
        public int? ApplicationId { get; set; }
        public string Message { get; set; }
    }
}

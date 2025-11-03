using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FileSharePortal.Data;
using FileSharePortal.Models;

namespace FileSharePortal.Services
{
    public class ApiTokenService : IDisposable
    {
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
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

            if (user == null)
            {
                return new AuthenticationResult { Success = false, Message = "Invalid username or password" };
            }

            // Check if user is from AD (AD users can't authenticate via API with password)
            if (user.IsFromActiveDirectory)
            {
                return new AuthenticationResult { Success = false, Message = "Active Directory users cannot authenticate via API" };
            }

            // Verify password
            if (!AuthenticationService.VerifyPassword(password, user.PasswordHash))
            {
                return new AuthenticationResult { Success = false, Message = "Invalid username or password" };
            }

            // Generate token
            var token = GenerateToken(user.UserId, ipAddress, applicationId);

            return new AuthenticationResult
            {
                Success = true,
                Token = token.Token,
                ExpiresDate = token.ExpiresDate,
                User = user,
                Message = "Authentication successful"
            };
        }

        /// <summary>
        /// Authenticates an application using its API key
        /// </summary>
        public AuthenticationResult AuthenticateApplication(string applicationName, string apiKey, string ipAddress)
        {
            var application = _context.Applications.FirstOrDefault(a =>
                a.ApplicationName == applicationName &&
                a.ApiKey == apiKey &&
                a.IsActive);

            if (application == null)
            {
                return new AuthenticationResult { Success = false, Message = "Invalid application name or API key" };
            }

            // Get the user who registered the application
            if (!application.RegisteredByUserId.HasValue)
            {
                return new AuthenticationResult { Success = false, Message = "Application not properly registered" };
            }

            var user = _context.Users.Find(application.RegisteredByUserId.Value);
            if (user == null || !user.IsActive)
            {
                return new AuthenticationResult { Success = false, Message = "Application owner is inactive" };
            }

            // Generate token for the application
            var token = GenerateToken(user.UserId, ipAddress, application.ApplicationId);

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

        /// <summary>
        /// Validates a token and returns the associated user
        /// </summary>
        public TokenValidationResult ValidateToken(string token)
        {
            var apiToken = _context.ApiTokens.FirstOrDefault(t => t.Token == token && !t.IsRevoked);

            if (apiToken == null)
            {
                return new TokenValidationResult { IsValid = false, Message = "Invalid token" };
            }

            if (apiToken.ExpiresDate < DateTime.Now)
            {
                return new TokenValidationResult { IsValid = false, Message = "Token has expired" };
            }

            // Update last used date
            apiToken.LastUsedDate = DateTime.Now;
            _context.SaveChanges();

            var user = _context.Users.Find(apiToken.UserId);
            if (user == null || !user.IsActive)
            {
                return new TokenValidationResult { IsValid = false, Message = "User is inactive" };
            }

            return new TokenValidationResult
            {
                IsValid = true,
                User = user,
                ApplicationId = apiToken.ApplicationId,
                Message = "Token is valid"
            };
        }

        /// <summary>
        /// Generates a new API token
        /// </summary>
        private ApiToken GenerateToken(int userId, string ipAddress, int? applicationId = null)
        {
            var tokenString = GenerateRandomToken();
            var expiresDate = DateTime.Now.AddDays(30); // Token valid for 30 days

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

            return apiToken;
        }

        /// <summary>
        /// Generates a cryptographically secure random token
        /// </summary>
        private string GenerateRandomToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var tokenData = new byte[32];
                rng.GetBytes(tokenData);

                var token = Convert.ToBase64String(tokenData);
                // Make URL-safe
                token = token.Replace("+", "-").Replace("/", "_").Replace("=", "");

                return token;
            }
        }

        /// <summary>
        /// Revokes a token
        /// </summary>
        public bool RevokeToken(string token)
        {
            var apiToken = _context.ApiTokens.FirstOrDefault(t => t.Token == token);
            if (apiToken == null)
            {
                return false;
            }

            apiToken.IsRevoked = true;
            _context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Revokes all tokens for a user
        /// </summary>
        public int RevokeUserTokens(int userId)
        {
            var tokens = _context.ApiTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToList();
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }
            _context.SaveChanges();
            return tokens.Count;
        }

        public void Dispose()
        {
            _context?.Dispose();
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

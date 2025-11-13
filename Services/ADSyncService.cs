using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using FileSharePortal.Data;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using log4net;

namespace FileSharePortal.Services
{
    public class ADSyncService
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(ADSyncService));
        private readonly FileSharePortalContext _context;
        private readonly string _adDomain;

        public ADSyncService()
        {
            _context = new FileSharePortalContext();

            _adDomain = ConfigurationManager.AppSettings["ADDomain"];
            Logger.Debug($"ADDomain = {_adDomain}");
            Logger.Debug("ADSyncService constructor completed");
        }

        public ADSyncResult SynchronizeADUsers()
        {
            Logger.Info("Starting Active Directory user synchronization");

            var result = new ADSyncResult();

            try
            {
                if (string.IsNullOrEmpty(_adDomain))
                {
                    Logger.Warn("Active Directory domain not configured");
                    result.ErrorMessage = "Active Directory domain not configured";
                    return result;
                }
                Logger.Debug($"AD Domain configured: {_adDomain}");

                // Get all users from Active Directory
                var adUsers = GetAllADUsers();
                result.TotalADUsers = adUsers.Count;
                Logger.Info($"Retrieved {adUsers.Count} user(s) from Active Directory");

                // Get all AD users from database
                var dbADUsers = _context.Users.Where(u => u.IsFromActiveDirectory).ToList();
                Logger.Debug($"Found {dbADUsers.Count} AD user(s) in database");

                // Create a dictionary for quick lookup
                var adUserDict = adUsers.ToDictionary(u => u.Username.ToLower(), u => u);
                var dbUserDict = dbADUsers.ToDictionary(u => u.Username.ToLower(), u => u);

                // Update existing users and add new ones
                foreach (var adUser in adUsers)
                {
                    var username = adUser.Username.ToLower();

                    if (dbUserDict.ContainsKey(username))
                    {
                        // Update existing user
                        var dbUser = dbUserDict[username];
                        bool updated = false;

                        if (dbUser.FullName != adUser.FullName)
                        {
                            dbUser.FullName = adUser.FullName;
                            updated = true;
                        }

                        if (dbUser.Email != adUser.Email)
                        {
                            dbUser.Email = adUser.Email;
                            updated = true;
                        }

                        // Reactivate user if they were disabled
                        if (!dbUser.IsActive)
                        {
                            dbUser.IsActive = true;
                            updated = true;
                            result.UsersReactivated++;
                        }

                        if (updated)
                        {
                            Logger.Debug($"User updated: {username}");
                            result.UsersUpdated++;
                        }
                    }
                    else
                    {
                        // Add new user
                        Logger.Info($"Adding new user from AD: {username}");
                        var newUser = new User
                        {
                            Username = adUser.Username,
                            FullName = adUser.FullName,
                            Email = adUser.Email,
                            IsFromActiveDirectory = true,
                            IsActive = true,
                            IsAdmin = false,
                            CreatedDate = DateTime.Now
                        };
                        _context.Users.Add(newUser);
                        result.UsersAdded++;
                    }
                }

                // Disable users that are in DB but not in AD
                foreach (var dbUser in dbADUsers)
                {
                    var username = dbUser.Username.ToLower();
                    if (!adUserDict.ContainsKey(username) && dbUser.IsActive)
                    {
                        Logger.Info($"Disabling user (not found in AD): {username}");
                        dbUser.IsActive = false;
                        result.UsersDisabled++;
                    }
                }

                _context.SaveChanges();
                result.Success = true;

                Logger.Info($"AD Synchronization completed successfully: {result.GetSummary()}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                LoggingHelper.LogError(Logger, "AD Synchronization failed", ex);
            }

            return result;
        }

        private List<ADUserInfo> GetAllADUsers()
        {
            Logger.Info($"Retrieving all users from Active Directory domain: {_adDomain}");

            var users = new List<ADUserInfo>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _adDomain))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        int totalCount = 0;
                        int enabledCount = 0;

                        foreach (var result in searcher.FindAll())
                        {
                            totalCount++;
                            var userPrincipal = result as UserPrincipal;

                            if (userPrincipal != null && userPrincipal.Enabled == true && !string.IsNullOrEmpty(userPrincipal.SamAccountName))
                            {
                                enabledCount++;
                                users.Add(new ADUserInfo
                                {
                                    Username = userPrincipal.SamAccountName,
                                    FullName = userPrincipal.DisplayName ?? userPrincipal.Name ?? userPrincipal.SamAccountName,
                                    Email = userPrincipal.EmailAddress ?? $"{userPrincipal.SamAccountName}@{_adDomain}"
                                });
                            }
                            else if (userPrincipal != null)
                            {
                                //Logger.Debug($"Skipping disabled or invalid AD user: {userPrincipal.SamAccountName}");
                            }
                        }

                        Logger.Info($"Processed {totalCount} AD principal(s), retrieved {enabledCount} enabled user(s)");
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error retrieving AD users from domain {_adDomain}", ex);
                throw;
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

        private class ADUserInfo
        {
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
        }
    }

    public class ADSyncResult
    {
        public bool Success { get; set; }
        public int TotalADUsers { get; set; }
        public int UsersAdded { get; set; }
        public int UsersUpdated { get; set; }
        public int UsersDisabled { get; set; }
        public int UsersReactivated { get; set; }
        public string ErrorMessage { get; set; }

        public string GetSummary()
        {
            if (!Success)
            {
                return $"Synchronization failed: {ErrorMessage}";
            }

            return $"Synchronization completed successfully. " +
                   $"Total AD Users: {TotalADUsers}, " +
                   $"Added: {UsersAdded}, " +
                   $"Updated: {UsersUpdated}, " +
                   $"Reactivated: {UsersReactivated}, " +
                   $"Disabled: {UsersDisabled}";
        }
    }
}

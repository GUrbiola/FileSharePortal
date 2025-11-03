using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using FileSharePortal.Data;
using FileSharePortal.Models;

namespace FileSharePortal.Services
{
    public class ADSyncService
    {
        private readonly FileSharePortalContext _context;
        private readonly string _adDomain;

        public ADSyncService()
        {
            _context = new FileSharePortalContext();
            _adDomain = ConfigurationManager.AppSettings["ADDomain"];
        }

        public ADSyncResult SynchronizeADUsers()
        {
            var result = new ADSyncResult();

            try
            {
                if (string.IsNullOrEmpty(_adDomain))
                {
                    result.ErrorMessage = "Active Directory domain not configured";
                    return result;
                }

                // Get all users from Active Directory
                var adUsers = GetAllADUsers();
                result.TotalADUsers = adUsers.Count;

                // Get all AD users from database
                var dbADUsers = _context.Users.Where(u => u.IsFromActiveDirectory).ToList();

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
                            result.UsersUpdated++;
                        }
                    }
                    else
                    {
                        // Add new user
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
                        dbUser.IsActive = false;
                        result.UsersDisabled++;
                    }
                }

                _context.SaveChanges();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                System.Diagnostics.Trace.TraceError($"AD Sync error: {ex.Message}");
            }

            return result;
        }

        private List<ADUserInfo> GetAllADUsers()
        {
            var users = new List<ADUserInfo>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _adDomain))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            var userPrincipal = result as UserPrincipal;
                            if (userPrincipal != null &&
                                userPrincipal.Enabled == true &&
                                !string.IsNullOrEmpty(userPrincipal.SamAccountName))
                            {
                                users.Add(new ADUserInfo
                                {
                                    Username = userPrincipal.SamAccountName,
                                    FullName = userPrincipal.DisplayName ?? userPrincipal.Name ?? userPrincipal.SamAccountName,
                                    Email = userPrincipal.EmailAddress ?? $"{userPrincipal.SamAccountName}@{_adDomain}"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error retrieving AD users: {ex.Message}");
                throw;
            }

            return users;
        }

        public void Dispose()
        {
            _context?.Dispose();
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

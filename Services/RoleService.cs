using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Configuration;
using FileSharePortal.Data;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using log4net;

namespace FileSharePortal.Services
{
    public class RoleService
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(RoleService));
        private readonly FileSharePortalContext _context;
        private readonly string _adDomain;

        public RoleService()
        {
            _context = new FileSharePortalContext();

            _adDomain = ConfigurationManager.AppSettings["ADDomain"];
        }

        public List<User> GetRoleUsers(int roleId)
        {
            LoggingHelper.LogWithParams(Logger, "GetRoleUsers", roleId);
            Logger.Info($"Retrieving users for role {roleId}");

            try
            {
                var role = _context.Roles.Find(roleId);

                if (role == null)
                {
                    Logger.Warn($"Role not found with ID: {roleId}");
                    return new List<User>();
                }

                Logger.Debug($"Role found: {role.RoleName}");

                // Get manually added users
                var manualUsers = _context.RoleUsers
                    .Where(ru => ru.RoleId == roleId)
                    .Select(ru => ru.User)
                    .Where(u => u.IsActive)
                    .ToList();

                // Get users from distribution lists
                var dlUsers = new List<User>();
                var distributionLists = _context.RoleDistributionLists
                    .Where(rdl => rdl.RoleId == roleId)
                    .Select(rdl => rdl.DistributionList)
                    .ToList();

                foreach (var dl in distributionLists)
                {
                    Logger.Debug($"Getting users from distribution list: {dl.Name}");
                    var usersFromDL = GetUsersFromDistributionList(dl.ADDistinguishedName);
                    Logger.Debug($"Found {usersFromDL.Count} user(s) from DL: {dl.Name}");
                    dlUsers.AddRange(usersFromDL);
                }

                // Combine and remove duplicates
                var allUsers = manualUsers.Union(dlUsers, new UserComparer()).ToList();

                Logger.Info($"Retrieved {allUsers.Count} total unique user(s) for role {roleId}");
                return allUsers;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error retrieving users for role {roleId}", ex);
                throw;
            }
        }

        private List<User> GetUsersFromDistributionList(string distinguishedName)
        {
            LoggingHelper.LogWithParams(Logger, "GetUsersFromDistributionList", distinguishedName);
            Logger.Info($"Getting users from distribution list: {distinguishedName}");

            var users = new List<User>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _adDomain))
                {
                    var group = GroupPrincipal.FindByIdentity(context, IdentityType.DistinguishedName, distinguishedName);

                    if (group != null)
                    {
                        var members = group.GetMembers(true);
                        int memberCount = 0;
                        int processedCount = 0;
                        int createdCount = 0;

                        foreach (var member in members)
                        {
                            memberCount++;
                            if (member is UserPrincipal userPrincipal)
                            {
                                var username = userPrincipal.SamAccountName;

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
                                    Logger.Info($"User created successfully: {username} (UserId: {user.UserId})");
                                    createdCount++;
                                }

                                if (user.IsActive)
                                {
                                    users.Add(user);
                                    processedCount++;
                                }
                                else
                                {
                                    Logger.Debug($"Skipping inactive user: {username}");
                                }
                            }
                        }

                        Logger.Info($"Processed {processedCount} active user(s) from {memberCount} total member(s), created {createdCount} new user(s)");
                    }
                    else
                    {
                        Logger.Warn($"Group not found in AD: {distinguishedName}");
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error getting users from distribution list: {distinguishedName}", ex);
                return users; // Return partial results
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

        private class UserComparer : IEqualityComparer<User>
        {
            public bool Equals(User x, User y)
            {
                return x.UserId == y.UserId;
            }

            public int GetHashCode(User obj)
            {
                return obj.UserId.GetHashCode();
            }
        }
    }
}

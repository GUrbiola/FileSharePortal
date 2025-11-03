using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Configuration;
using FileSharePortal.Data;
using FileSharePortal.Models;

namespace FileSharePortal.Services
{
    public class RoleService
    {
        private readonly FileSharePortalContext _context;
        private readonly string _adDomain;

        public RoleService()
        {
            _context = new FileSharePortalContext();
            _adDomain = ConfigurationManager.AppSettings["ADDomain"];
        }

        public List<User> GetRoleUsers(int roleId)
        {
            var role = _context.Roles.Find(roleId);
            if (role == null)
                return new List<User>();

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
                var usersFromDL = GetUsersFromDistributionList(dl.ADDistinguishedName);
                dlUsers.AddRange(usersFromDL);
            }

            // Combine and remove duplicates
            var allUsers = manualUsers.Union(dlUsers, new UserComparer()).ToList();
            return allUsers;
        }

        private List<User> GetUsersFromDistributionList(string distinguishedName)
        {
            var users = new List<User>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _adDomain))
                {
                    var group = GroupPrincipal.FindByIdentity(context, IdentityType.DistinguishedName, distinguishedName);
                    if (group != null)
                    {
                        var members = group.GetMembers(true);
                        foreach (var member in members)
                        {
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
                                }

                                if (user.IsActive)
                                {
                                    users.Add(user);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Error getting users from DL: {ex.Message}");
            }

            return users;
        }

        public void Dispose()
        {
            _context?.Dispose();
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

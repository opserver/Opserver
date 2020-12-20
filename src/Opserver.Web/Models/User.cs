using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using Opserver.Security;

namespace Opserver.Models
{
    public class User
    {
        public string AccountName { get; }
        public bool IsAnonymous { get; }
        public bool IsGlobalAdmin { get; }
        private SecurityProvider Provider { get; }
        /// <summary>
        /// Returns this user's role on the current site.
        /// </summary>
        public ImmutableHashSet<string> Roles { get; }

        public User(SecurityProvider provider, ClaimsPrincipal principal, List<string> baseRoles, IEnumerable<StatusModule> modules)
        {
            Provider = provider;
            var identity = principal?.Identity;
            if (identity == null)
            {
                IsAnonymous = true;
                return;
            }

            IsAnonymous = !identity.IsAuthenticated;
            if (identity.IsAuthenticated)
                AccountName = identity.Name;

            var roles = baseRoles;

            if (IsAnonymous)
            {
                roles.Add(Models.Roles.Anonymous);
            }
            else
            {
                roles.Add(Models.Roles.Authenticated);

                // Global admins are unique and can see a few more things about Opserver itself (not per-module)
                if (provider.IsGlobalAdmin(this))
                {
                    roles.Add(Models.Roles.GlobalAdmin);
                    IsGlobalAdmin = true;
                }
            }

            // Add per-module roles
            foreach (var module in modules)
            {
                if (IsAdmin(module))
                {
                    roles.Add(module.SecuritySettings.AdminRole);
                    roles.Add(module.SecuritySettings.ViewRole);
                }
                else if (HasAccess(module))
                {
                    roles.Add(module.SecuritySettings.ViewRole);
                }
            }

            Roles = roles.ToImmutableHashSet();
        }

        public bool Is(string role) => Roles.Contains(role) || IsGlobalAdmin;
        public bool HasAccess(StatusModule module) => Provider.InReadGroups(this, module);
        public bool IsAdmin(StatusModule module) => Provider.InAdminGroups(this, module);
    }
}

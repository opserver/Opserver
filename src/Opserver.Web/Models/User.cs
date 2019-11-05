using System.Collections.Generic;
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
        public Roles Roles { get; }

        public User(SecurityProvider provider, ClaimsPrincipal principle, Roles baseRoles, IEnumerable<StatusModule> modules)
        {
            Provider = provider;
            var identity = principle?.Identity;
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
                roles |= Roles.Anonymous;
            }
            else
            {
                roles |= Roles.Authenticated;

                // Global admins are unique and can see a few more things about Opserver itself (not per-module)
                if (provider.IsGlobalAdmin(this))
                {
                    roles |= Roles.GlobalAdmin;
                    IsGlobalAdmin = true;
                }
            }

            // Add per-module roles
            foreach (var m in modules)
            {
                roles |= GetRoles(m);
            }

            Roles = roles;
        }

        // TODO: Move from modules being known here
        private Roles GetRoles(StatusModule module)
        {
            switch (module)
            {
                case Data.Cloudflare.CloudflareModule m:
                    return GetRoles(m, Roles.Cloudflare, Roles.CloudflareAdmin);
                case Data.Dashboard.DashboardModule m:
                    return GetRoles(m, Roles.Dashboard, Roles.DashboardAdmin);
                case Data.Elastic.ElasticModule m:
                    return GetRoles(m, Roles.Elastic, Roles.ElasticAdmin);
                case Data.Exceptions.ExceptionsModule m:
                    return GetRoles(m, Roles.Exceptions, Roles.ExceptionsAdmin);
                case Data.HAProxy.HAProxyModule m:
                    return GetRoles(m, Roles.HAProxy, Roles.HAProxyAdmin);
                case Data.Redis.RedisModule m:
                    return GetRoles(m, Roles.Redis, Roles.RedisAdmin);
                case Data.SQL.SQLModule m:
                    return GetRoles(m, Roles.SQL, Roles.SQLAdmin);
                case Data.PagerDuty.PagerDutyModule m:
                    return GetRoles(m, Roles.PagerDuty, Roles.PagerDutyAdmin);
                default:
                    return Roles.None;
            }
        }

        private Roles GetRoles(StatusModule module, Roles user, Roles admin)
        {
            if (IsAdmin(module)) return admin | user;
            if (HasAccess(module)) return user;
            return Roles.None;
        }

        public bool Is(Roles role) => (Roles & role) == role || IsGlobalAdmin;
        public bool HasAccess(StatusModule module) => Provider.InReadGroups(this, module);
        public bool IsAdmin(StatusModule module) => Provider.InAdminGroups(this, module);
    }
}

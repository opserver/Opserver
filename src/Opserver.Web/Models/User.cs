using System;
using System.Security.Claims;
using System.Security.Principal;
using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver.Models
{
    public class User : ClaimsPrincipal
    {
        public string AccountName { get; }
        public bool IsAnonymous { get; }

        public User(IIdentity identity) : base(identity)
        {
            if (Identity == null)
            {
                IsAnonymous = true;
                return;
            }

            IsAnonymous = !Identity.IsAuthenticated;
            if (Identity.IsAuthenticated)
                AccountName = Identity.Name;
        }

        public override bool IsInRole(string role) => Enum.TryParse(role, out Roles r) && Opserver.Current.IsInRole(r);

        private Roles? _role;
        public Roles? RawRoles => _role;

        /// <summary>
        /// Returns this user's role on the current site.
        /// </summary>
        public Roles Role
        {
            get
            {
                if (_role == null)
                {
                    if (IsAnonymous)
                    {
                        _role = Roles.Anonymous;
                    }
                    else
                    {
                        var result = Roles.Authenticated;

                        if (Opserver.Current.Security.IsAdmin) result |= Roles.GlobalAdmin;

                        result |= GetRoles(Opserver.Current.Settings.CloudFlare, Roles.CloudFlare, Roles.CloudFlareAdmin);
                        result |= GetRoles(Opserver.Current.Settings.Dashboard, Roles.Dashboard, Roles.DashboardAdmin);
                        result |= GetRoles(Opserver.Current.Settings.Elastic, Roles.Elastic, Roles.ElasticAdmin);
                        result |= GetRoles(Opserver.Current.Settings.Exceptions, Roles.Exceptions, Roles.ExceptionsAdmin);
                        result |= GetRoles(Opserver.Current.Settings.HAProxy, Roles.HAProxy, Roles.HAProxyAdmin);
                        result |= GetRoles(Opserver.Current.Settings.Redis, Roles.Redis, Roles.RedisAdmin);
                        result |= GetRoles(Opserver.Current.Settings.SQL, Roles.SQL, Roles.SQLAdmin);
                        result |= GetRoles(Opserver.Current.Settings.PagerDuty, Roles.PagerDuty, Roles.PagerDutyAdmin);

                        _role = result;
                    }
                }

                return _role.Value;
            }
        }

        public Roles GetRoles(ISecurableModule module, Roles user, Roles admin)
        {
            if (module.IsAdmin()) return admin | user;
            if (module.HasAccess()) return user;
            return Roles.None;
        }

        public bool IsGlobalAdmin => Opserver.Current.IsInRole(Roles.GlobalAdmin);
        public bool IsExceptionAdmin => Opserver.Current.IsInRole(Roles.ExceptionsAdmin);
        public bool IsHAProxyAdmin => Opserver.Current.IsInRole(Roles.ExceptionsAdmin);
        public bool IsRedisAdmin => Opserver.Current.IsInRole(Roles.RedisAdmin);
        public bool IsSQLAdmin => Opserver.Current.IsInRole(Roles.SQLAdmin);
    }
}

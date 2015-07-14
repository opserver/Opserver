using System;
using System.Security.Principal;
using System.Web.Security;
using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver.Models
{
    public class User : IPrincipal
    {
        public IIdentity Identity { get; private set; }

        public string AccountName { get; private set; }
        public bool IsAnonymous { get; private set; }

        public User(IIdentity identity)
        {
            Identity = identity;
            var i = identity as FormsIdentity;
            if (i == null)
            {
                IsAnonymous = true;
                return;
            }

            IsAnonymous = !i.IsAuthenticated;
            if (i.IsAuthenticated)
                AccountName = i.Name;
        }

        public bool IsInRole(string role)
        {
            Roles r;
            return Enum.TryParse(role, out r) && IsInRole(r);
        }

        public bool IsInRole(Roles roles)
        {
            return (Role & roles) != Roles.None || Role.HasFlag(Roles.GlobalAdmin);
        }

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

                        if (Current.Security.IsAdmin) result |= Roles.GlobalAdmin;

                        result |= GetRoles(Current.Settings.CloudFlare, Roles.CloudFlare, Roles.CloudFlareAdmin);
                        result |= GetRoles(Current.Settings.Dashboard, Roles.Dashboard, Roles.DashboardAdmin);
                        result |= GetRoles(Current.Settings.Elastic, Roles.Elastic, Roles.ElasticAdmin);
                        result |= GetRoles(Current.Settings.Exceptions, Roles.Exceptions, Roles.ExceptionsAdmin);
                        result |= GetRoles(Current.Settings.HAProxy, Roles.HAProxy, Roles.HAProxyAdmin);
                        result |= GetRoles(Current.Settings.Redis, Roles.Redis, Roles.RedisAdmin);
                        result |= GetRoles(Current.Settings.SQL, Roles.SQL, Roles.SQLAdmin);
                        result |= GetRoles(Current.Settings.PagerDuty, Roles.PagerDuty, Roles.PagerDutyAdmin);

                        _role = result;
                    }
                }
                return Current.Security.IsInternalIP(Current.RequestIP)
                           ? _role.Value | Roles.InternalRequest
                           : _role.Value;
            }
        }

        public Roles GetRoles(ISecurableSection section, Roles user, Roles admin)
        {
            if (section.IsAdmin()) return admin | user;
            if (section.HasAccess()) return user;
            return Roles.None;
        }

        public bool IsGlobalAdmin => IsInRole(Roles.GlobalAdmin);
        public bool IsExceptionAdmin => IsInRole(Roles.ExceptionsAdmin);
        public bool IsHAProxyAdmin => IsInRole(Roles.ExceptionsAdmin);
        public bool IsRedisAdmin => IsInRole(Roles.RedisAdmin);
        public bool IsSQLAdmin => IsInRole(Roles.SQLAdmin);
    }
}
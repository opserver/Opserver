using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Models.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class ConfigFileProvider : SecurityProvider
    {
        private UserSettings Settings
        {
            get;
            set;
        }

        public ConfigFileProvider(SecuritySettings settings)
        {
            Settings = Current.Settings.Users;
        }

        public override bool InGroups(string groupNames, string accountName)
        {
            var groups = groupNames.Split(StringSplits.Comma_SemiColon);
            if (groupNames.Length == 0)
                return false;

            return groups.Any(g =>
            {
                var members = GetGroupMembers(g);
                return members != null && members.Contains(accountName, StringComparer.InvariantCultureIgnoreCase);
            });
        }

        public override List<string> GetGroupMembers(string groupName)
        {
            return Current.LocalCache.GetSet<List<string>>("ConfigFileMembers-" + groupName,
                (old, ctx) =>
                {
                    using (MiniProfiler.Current.Step("Getting members for " + groupName))
                    {
                        var users = Settings.Users;
                        if (users == null)
                            return new List<string>();
                        if(!users.Any())
                            return new List<string>();
                        var current = users.Where(u => u.Groups != null && u.Groups.Any(g => string.Equals(g.Name, groupName))).Select(u => u.Username).ToList();
                        return current ?? old ?? new List<string>();
                    }
                }, 5 * 60, 60 * 60 * 24);
        }


        public override bool ValidateUser(string userName, string password)
        {
            return Settings.Users.Any(u => u.Username == userName && u.Password == password);
            
        }
    }
}
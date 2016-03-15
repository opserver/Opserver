using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Models.Security
{
    /// <summary>
    /// To use - set Authentication to Windows in web.config
    /// </summary>
    public class WindowsAuthenticationProvider : SecurityProvider
    {
        public override bool ValidateUser(string userName, string password) { return true; }

        public override bool InGroups(string groupNames, string accountName)
        {
            var groups = groupNames.Split(StringSplits.Comma_SemiColon);
            if (groupNames.Length == 0) return false;

            return groups.Any(g =>
            {
                var members = GetGroupMembers(g);
                return members != null && members.Contains(accountName, StringComparer.InvariantCultureIgnoreCase);
            });
        }
        public override List<string> GetGroupMembers(string groupName)
        {
            return Current.LocalCache.GetSet<List<string>>("ADMembers-" + groupName,
                (old, ctx) =>
                {
                    using (MiniProfiler.Current.Step("Getting members for " + groupName))
                    {
                        //var pc = new PrincipalContext(ContextType.Domain, null);
                        var pc = new PrincipalContext(ContextType.Domain, "business-post.com");
                        using (var gp = GroupPrincipal.FindByIdentity(pc, groupName))
                        {
                            var group = gp?.GetMembers(true).ToList().Select(mp => mp.SamAccountName).ToList() ?? new List<string>();
                            return group ?? old ?? new List<string>();
                        }

                    }
                }, 5 * 60, 60 * 60 * 24);
        }      
    }
}
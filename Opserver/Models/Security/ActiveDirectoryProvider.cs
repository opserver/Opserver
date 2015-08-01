using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security;
using System.Threading;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Models.Security
{
    public class ActiveDirectoryProvider : SecurityProvider
    {
        private List<string> Servers { get; set; }
        private string AuthUser { get; set; }
        private string AuthPassword { get; set; }

        public ActiveDirectoryProvider(SecuritySettings settings)
        {
            if (settings.Server.HasValue()) Servers = settings.Server.Split(StringSplits.Comma_SemiColon).ToList();
            AuthUser = settings.AuthUser;
            AuthPassword = settings.AuthPassword;
        }

        private bool UserAuth => AuthUser.HasValue() && AuthPassword.HasValue();

        public override bool ValidateUser(string userName, string password)
        {
            return RunCommand(pc => pc.ValidateCredentials(userName, password));
        }

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
        
        public override void PurgeCache()
        {
            //Current.LocalCache.RemoveAll("AD-Members-*");
        }

        public override List<string> GetGroupMembers(string groupName)
        {
            return Current.LocalCache.GetSet<List<string>>("ADMembers-" + groupName,
                (old, ctx) =>
                {
                    using (MiniProfiler.Current.Step("Getting members for " + groupName))
                    {
                        var group = RunCommand(pc =>
                            {
                                using (var gp = GroupPrincipal.FindByIdentity(pc, groupName))
                                {
                                    return gp?.GetMembers(true).ToList().Select(mp => mp.SamAccountName).ToList() ?? new List<string>();
                                }
                            });
                        return group ?? old ?? new List<string>();
                    }
                }, 5 * 60, 60 * 60 * 24);
        }

        public T RunCommand<T>(Func<PrincipalContext, T> command, int retries = 3)
        {
            if (Servers != null && Servers.Any())
            {
                foreach (var s in Servers) // try all servers in order, the first success will win
                {
                    try
                    {
                        using (var pc = UserAuth
                                            ? new PrincipalContext(ContextType.Domain, s, AuthUser, AuthPassword)
                                            : new PrincipalContext(ContextType.Domain, s))
                            return command(pc);
                    }
                    catch (Exception ex)
                    {
                        Current.LogException("Couldn't contact AD server: " + s, ex);
                    }
                }
                Current.LogException(new SecurityException("Couldn't read any configured Active Directory servers."));
            }
            else
            {
                // try the current domain
                try
                {
                    using (var pc = new PrincipalContext(ContextType.Domain))
                        return command(pc);
                }
                catch (Exception ex)
                {
                    if (retries > 0)
                    {
                        Thread.Sleep(1000);
                        RunCommand(command, retries - 1);
                    }
                    else
                    {
                        Current.LogException("Couldn't contact current AD", ex);
                    }
                }
            }

            return default(T);
        }
    }
}
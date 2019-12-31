using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Opserver.Models;
using StackExchange.Profiling;

namespace Opserver.Security
{
    public class ActiveDirectoryProvider : SecurityProvider
    {
        private IMemoryCache Cache { get; }
        public override string ProviderName => "Active Directory";
        private HashSet<string> GroupNames { get; } = new HashSet<string>();
        private List<string> Servers { get; }
        private string AuthUser { get; }
        private string AuthPassword { get; }

        public ActiveDirectoryProvider(SecuritySettings settings, IMemoryCache cache) : base(settings)
        {
            Cache = cache;
            if (settings.Server.HasValue()) Servers = settings.Server.Split(StringSplits.Comma_SemiColon).ToList();
            AuthUser = settings.AuthUser;
            AuthPassword = settings.AuthPassword;
        }

        private bool UserAuth => AuthUser.HasValue() && AuthPassword.HasValue();

        public override bool ValidateUser(string userName, string password)
        {
            return RunCommand(pc => pc.ValidateCredentials(userName, password));
        }

        public override bool InGroups(User user, string groupNames)
        {
            var groups = groupNames.Split(StringSplits.Comma_SemiColon);
            if (groupNames.Length == 0) return false;

            // TODO: Move this elsewhere
            if (groups.Any(g => g == "*")) return true;

            return groups.Any(g => GetGroupMembers(g)?.Contains(user.AccountName, StringComparer.InvariantCultureIgnoreCase) == true);
        }

        public override void PurgeCache()
        {
            var toClear = GroupNames.ToList();
            GroupNames.Clear();
            foreach (var g in toClear)
            {
                Cache.Remove("ADMembers-" + g);
            }
        }

        public List<string> GetGroupMembers(string groupName)
        {
            GroupNames.Add(groupName);
            return Cache.GetSet<List<string>>("ADMembers-" + groupName,
                (old, _) =>
                {
                    using (MiniProfiler.Current.Step("Getting members for " + groupName))
                    {
                        var group = RunCommand(pc =>
                            {
                                using var gp = GroupPrincipal.FindByIdentity(pc, groupName);
                                return gp?.GetMembers(true).ToList().Select(mp => mp.SamAccountName).ToList() ?? new List<string>();
                            });
                        return group ?? old ?? new List<string>();
                    }
                }, 5.Minutes(), 24.Hours());
        }

        public T RunCommand<T>(Func<PrincipalContext, T> command, int retries = 3)
        {
            if (Servers?.Count > 0)
            {
                foreach (var s in Servers) // try all servers in order, the first success will win
                {
                    try
                    {
                        using var pc = UserAuth ? new PrincipalContext(ContextType.Domain, s, AuthUser, AuthPassword)
                                                : new PrincipalContext(ContextType.Domain, s);
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
                    using var pc = new PrincipalContext(ContextType.Domain);
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

            return default;
        }
    }
}

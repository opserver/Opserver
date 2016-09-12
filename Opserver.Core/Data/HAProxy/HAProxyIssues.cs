﻿using System.Collections.Generic;

namespace StackExchange.Opserver.Data.HAProxy
{
    public class HAProxyIssues : IIssuesProvider
    {
        public bool Enabled => HAProxyGroup.AllGroups.Count > 0;
        public string Name => "HAProxy";

        public IEnumerable<Issue> GetIssues()
        {
            foreach (var g in HAProxyGroup.AllGroups.WithIssues())
            {
                yield return new Issue<HAProxyGroup>(g, g.Name);
            }
        }
    }
}

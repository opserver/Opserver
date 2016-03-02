using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class TfsAction : IssueTrackerActionBase, ISettingsCollectionItem<TfsAction>
    {
        public int Id => GetHashCode();

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;

                foreach (var a in Applications)
                    hashCode = (hashCode * 397) ^ a.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (Caption?.GetHashCode() ?? 0);

                return hashCode;
            }
        }
    }

    public class TfsSettings : Settings<TfsSettings>
    {
        public override bool Enabled => Actions.Any();

        public List<TfsAction> Actions { set; get; } 

        public string InstanceUrl { set; get; }

        public string ApiVersion { set; get; }

        public string DefaultUsername { set; get; }
        public string DefaultPassword { set; get; }

        public string DefaultProjectKey { set; get; }

        public string DefaultCollection { set; get; }

        public List<string> Applications { get; set; }

        public TfsSettings()
        {
            this.Actions=new List<TfsAction>();
        }

        public List<TfsAction> GetActionsForApplication(string application)
        {
            var isValidApp = Applications.Any(a => a.Equals(application, StringComparison.OrdinalIgnoreCase));
            if (!isValidApp)
                return null;
            var actions = Actions.Where(i => i.Applications == null
                                             || i.Applications.Count == 0
                                             || i.Applications.Contains(application, StringComparer.OrdinalIgnoreCase)
                ).Select(i => i).ToList();

            return actions;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class JiraSettings : Settings<JiraSettings>
    {
        public override bool Enabled => Actions.Any();

        public List<JiraAction> Actions { get; set; } = new List<JiraAction>();

        public List<string> Applications { get; set; } = new List<string>();

        /// <summary>
        /// Default url for all actions
        /// </summary>
        public string DefaultUrl { get; set; }
        /// <summary>
        /// Default username for all actions
        /// </summary>
        public string DefaultUsername { get; set; }
        /// <summary>
        /// Default password for all actions
        /// </summary>
        public string DefaultPassword { get; set; }
        /// <summary>
        /// Default host
        /// </summary>
        public string DefaultHost { get; set; }
        /// <summary>
        /// Default project key for all actions
        /// </summary>
        public string DefaultProjectKey { get; set; }
        
        public List<JiraAction> GetActionsForApplication(string application)
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

    public class JiraAction : ISettingsCollectionItem
    {
        public List<JiraComponent> Components { get; set; } = new List<JiraComponent>();

        public List<string> Applications { get; set; } = new List<string>();

        public int Id => GetHashCode();

        /// <summary>
        /// Host url
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Jira base url
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Jira username
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Jira password
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Jira project key 
        /// </summary>
        public string ProjectKey { get; set; }
        /// <summary>
        /// Name of the jira issue type
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Action link caption
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Comma delimited list of labels
        /// </summary>
        public string Labels { get; set; }

        public List<object> GetComponentsForApplication(string application)
        {
            if (Components == null || Components.Count == 0)
                return new List<object>();

            var components = Components
                .Where(c => c.Application != null && application.Equals(c.Application, StringComparison.OrdinalIgnoreCase))
                .Select(c => c).ToList();


            return (from c in components
                    select new { name = c.Name }).ToList<object>();
        }
    }

    public class JiraComponent : ISettingsCollectionItem
    {
        /// <summary>
        /// Name of the jira component
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Application name the component is valid for
        /// </summary>
        public string Application { get; set; }
    }
}

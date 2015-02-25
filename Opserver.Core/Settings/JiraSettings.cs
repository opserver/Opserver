using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StackExchange.Opserver
{
    public class JiraSettings : Settings<JiraSettings>, IAfterLoadActions
    {
        public override bool Enabled { get { return Actions.Any(); } }

        public ObservableCollection<JiraAction> Actions { get; set; }
        public event EventHandler<JiraAction> ActionAdded = delegate { };
        public event EventHandler<List<JiraAction>> ActionChanged = delegate { };
        public event EventHandler<JiraAction> ActionRemoved = delegate { };

        public ObservableCollection<string> Applications { get; set; }
        public event EventHandler<string> ApplicationAdded = delegate { };
        public event EventHandler<List<string>> ApplicationsChanged = delegate { };
        public event EventHandler<string> ApplicationRemoved = delegate { };


        public JiraSettings()
        {
            Actions = new ObservableCollection<JiraAction>();
            Applications = new ObservableCollection<string>();
        }
        public void AfterLoad()
        {
            Actions.AddHandlers(this, ActionAdded, ActionChanged, ActionRemoved);
            Applications.AddHandlers(this, ApplicationAdded, ApplicationsChanged, ApplicationRemoved);
        }

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

    public class JiraAction : ISettingsCollectionItem<JiraAction>, IAfterLoadActions
    {
        public ObservableCollection<JiraComponent> Components { get; set; }
        public event EventHandler<JiraComponent> ComponentAdded = delegate { };
        public event EventHandler<List<JiraComponent>> ComponentsChanged = delegate { };
        public event EventHandler<JiraComponent> ComponentRemoved = delegate { };

        public ObservableCollection<string> Applications { get; set; }
        public event EventHandler<string> ApplicationAdded = delegate { };
        public event EventHandler<List<string>> ApplicationsChanged = delegate { };
        public event EventHandler<string> ApplicationRemoved = delegate { };

        public JiraAction()
        {
            Components = new ObservableCollection<JiraComponent>();
            Applications = new ObservableCollection<string>();
        }

        public void AfterLoad()
        {
            Components.AddHandlers(this, ComponentAdded, ComponentsChanged, ComponentRemoved);
            Applications.AddHandlers(this, ApplicationAdded, ApplicationsChanged, ApplicationRemoved);
        }

        public int Id
        {
            get { return GetHashCode(); }
        }

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
        public bool Equals(JiraAction other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Components.SequenceEqual(other.Components)
                && Applications.SequenceEqual(other.Applications)
                && string.Equals(Host, other.Host)
                && string.Equals(Url, other.Url)
                && string.Equals(Username, other.Username)
                && string.Equals(Password, other.Password)
                && string.Equals(ProjectKey, other.ProjectKey)
                && string.Equals(Name, other.Name)
                && string.Equals(Caption, other.Caption)
                && string.Equals(Labels, other.Labels);

        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JiraAction)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;

                foreach (var c in Components)
                    hashCode = (hashCode * 397) ^ c.GetHashCode();

                foreach (var a in Applications)
                    hashCode = (hashCode * 397) ^ a.GetHashCode();


                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Password != null ? Password.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Username != null ? Username.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProjectKey != null ? ProjectKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Caption != null ? Caption.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Labels != null ? Labels.GetHashCode() : 0);
                return hashCode;
            }
        }

    }

    public class JiraComponent : ISettingsCollectionItem<JiraComponent>
    {
        /// <summary>
        /// Name of the jira component
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Application name the component is valid for
        /// </summary>
        public string Application { get; set; }

        public bool Equals(JiraComponent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Application, other.Application)
                && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JiraAction)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Application != null ? Application.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}

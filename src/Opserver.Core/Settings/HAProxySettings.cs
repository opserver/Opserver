using System;
using System.Collections.Generic;

namespace Opserver
{
    public partial class HAProxySettings : ModuleSettings
    {
        public override bool Enabled => Instances.Count > 0 || Groups.Count > 0;

        public List<Group> Groups { get; set; } = new List<Group>();

        public List<Instance> Instances { get; set; } = new List<Instance>();

        public Dictionary<string, string> Aliases { get; set; } = new Dictionary<string, string>();

        public InstanceSettings GetInstanceSettings(Instance instance, Group group)
        {
            // Grab setting from node, then category, then global
            string getVal(Func<IInstanceSettings, string> f, string d) => f(instance)
                              .IsNullOrEmptyReturn(group != null ? f(group) : null)
                              .IsNullOrEmptyReturn(d);

            return new InstanceSettings
            {
                Name = instance.Name.IsNullOrEmptyReturn(group != null ? group.Name : "Unknown"),
                Description = instance.Description.IsNullOrEmptyReturn(group != null ? group.Description : "Unknown"),
                QueryTimeoutMs = instance.QueryTimeoutMs ?? @group?.QueryTimeoutMs ?? QueryTimeoutMs,
                User = getVal(i => i.User, User),
                Password = getVal(i => i.Password, Password),
                AdminUser = getVal(i => i.AdminUser, AdminUser),
                AdminPassword = getVal(i => i.AdminPassword, AdminPassword)
            };
        }

        /// <summary>
        /// Default username to use on all instances
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Default password to use on all instances
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Default admin username to use on all instances
        /// </summary>
        public string AdminUser { get; set; }

        /// <summary>
        /// Default admin password to use on all instances
        /// </summary>
        public string AdminPassword { get; set; }

        /// <summary>
        /// Default maximum timeout in milliseconds before giving up on an instance, defaults to 60,000ms
        /// </summary>
        public int QueryTimeoutMs { get; set; } = 60 * 1000;

        public class Group : ISettingsCollectionItem, IInstanceSettings
        {
            /// <summary>
            /// Instances in this group
            /// </summary>
            public List<Instance> Instances { get; set; } = new List<Instance>();

            /// <summary>
            /// The name that appears for this group
            /// </summary>
            public string Name { get; set; }

            public string Description { get; set; }

            /// <summary>
            /// Maximum timeout in milliseconds before giving up on this instances in this group
            /// </summary>
            public int? QueryTimeoutMs { get; set; }

            /// <summary>
            /// Username to use for this group of instances, unless specified by the individual instance
            /// </summary>
            public string User { get; set; }

            /// <summary>
            /// Password to use for this group of instances, unless specified by the individual instance
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Admin Username to use for this group of instances, unless specified by the individual instance
            /// </summary>
            public string AdminUser { get; set; }

            /// <summary>
            /// Admin Default admin password to use on all group of instances, unless specified by the individual instance
            /// </summary>
            public string AdminPassword { get; set; }
        }

        public class Instance : ISettingsCollectionItem, IInstanceSettings
        {
            /// <summary>
            /// URL to use for this instance
            /// </summary>
            public string Url { get; set; }

            /// <summary>
            /// The name that appears for this instance
            /// </summary>
            public string Name { get; set; }

            public string Description { get; set; }

            /// <summary>
            /// Maximum timeout in milliseconds before giving up on this instance
            /// </summary>
            public int? QueryTimeoutMs { get; set; }

            /// <summary>
            /// Username to use for this instance
            /// </summary>
            public string User { get; set; }

            /// <summary>
            /// Password to use for this instance
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Admin Username to use for this instance
            /// </summary>
            public string AdminUser { get; set; }

            /// <summary>
            /// Admin Default admin password to use on all instances
            /// </summary>
            public string AdminPassword { get; set; }
        }

        public class InstanceSettings
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public int? QueryTimeoutMs { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string AdminUser { get; set; }
            public string AdminPassword { get; set; }
        }

        public interface IInstanceSettings
        {
            string Name { get; set; }
            string Description { get; set; }
            int? QueryTimeoutMs { get; set; }
            string User { get; set; }
            string Password { get; set; }
            string AdminUser { get; set; }
            string AdminPassword { get; set; }
        }
    }
}

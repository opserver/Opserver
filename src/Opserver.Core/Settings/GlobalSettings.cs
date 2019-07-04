namespace StackExchange.Opserver
{
    public class GlobalSettings : ISecurableModule
    {
        bool ISecurableModule.Enabled => true;

        /// <summary>
        /// The name of the site.
        /// </summary>
        public string SiteName { get; set; } = "Opserver";

        /// <summary>
        /// The profiling mode to use.
        /// </summary>
        public ProfilingModes ProfilingMode { get; set; } = ProfilingModes.AdminOnly;

        /// <summary>
        /// Whether to profile pollers.
        /// </summary>
        public bool ProfilePollers { get; set; }

        /// <summary>
        /// Whether to log exceptions when polling.
        /// </summary>
        public bool LogPollerExceptions { get; set; }

        /// <summary>
        /// Semicolon delimited list of groups that can administer the entire site, exceptions, HAProxy servers, etc.
        /// </summary>
        public string AdminGroups { get; set; }

        /// <summary>
        /// Semicolon delimited list of groups that can view the entire site, exceptions, HAProxy servers, etc.
        /// </summary>
        public string ViewGroups { get; set; }

        public enum ProfilingModes
        {
            Enabled = 0,
            Disabled = 1,
            LocalOnly = 2,
            AdminOnly = 3
        }
    }
}

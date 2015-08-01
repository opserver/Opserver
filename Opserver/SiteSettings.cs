using System.Configuration;

namespace StackExchange.Opserver
{
    public static class SiteSettings
    {
        public static string SiteName => ConfigurationManager.AppSettings["SiteName"].IsNullOrEmptyReturn("opserver");

        public static string SiteNameLong => ConfigurationManager.AppSettings["SiteNameLong"].IsNullOrEmptyReturn("opserver");

        public static int HeaderRefreshSeconds => int.Parse(ConfigurationManager.AppSettings["HeaderRefreshSeconds"].IsNullOrEmptyReturn("30"));

        public static string Profiling => ConfigurationManager.AppSettings["Profiling"].IsNullOrEmptyReturn("admin");

        public static bool PollerProfiling
        {
            get
            {
                bool setting;
                return bool.TryParse(ConfigurationManager.AppSettings["PollerProfiling"], out setting) && setting;
            }
        }

        /// <summary>
        /// Semilcolon delimited list of groups that can administer the entire site, exceptions, HAProxy servers, etc.
        /// </summary>
        public static string AdminGroups => ConfigurationManager.AppSettings["AdminGroups"].IsNullOrEmptyReturn("");

        /// <summary>
        /// Semilcolon delimited list of groups that can view the entire site, exceptions, HAProxy servers, etc.
        /// </summary>
        public static string ViewGroups => ConfigurationManager.AppSettings["ViewGroups"].IsNullOrEmptyReturn("");

        private static ProfilingModes? _profilingMode;
        public static ProfilingModes ProfilingMode
        {
            get
            {
                if (!_profilingMode.HasValue)
                {
                    switch (Profiling.ToLower())
                    {
                        case "true":
                        case "yes":
                            _profilingMode = ProfilingModes.Enabled;
                            break;
                        case "local":
                            _profilingMode = ProfilingModes.LocalOnly;
                            break;
                        case "admin":
                            _profilingMode = ProfilingModes.AdminOnly;
                            break;
                        default:
                            _profilingMode = ProfilingModes.Disabled;
                            break;
                    }
                }
                return _profilingMode.Value;
            }
        }

        public enum ProfilingModes
        {
            Enabled,
            Disabled,
            LocalOnly,
            AdminOnly
        }
    }
}
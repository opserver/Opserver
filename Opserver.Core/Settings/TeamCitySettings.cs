using System;
using System.Collections.Generic;

namespace StackExchange.Opserver
{
    public class TeamCitySettings : Settings<TeamCitySettings>
    {
        public override bool Enabled => Url.HasValue();

        private Dictionary<string, List<string>> _serverMaps;
        public Dictionary<string, List<string>> ServerMaps
        {
            get { return _serverMaps; }
            set
            {
                _serverMaps = value;
                ServerMapsChanged(this, value);
            }
        }
        public event EventHandler<Dictionary<string, List<string>>> ServerMapsChanged = delegate { };

        public TeamCitySettings()
        {
            // Defaults
            BuildFetchIntervalSeconds = 30;
        }

        /// <summary>
        /// Url for the TeamCity Server
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// User to use when hitting the TeamCity API
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Password to use when hitting the TeamCity API
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// How many seconds to cache builds for before fetching for new ones, defaults to 30
        /// </summary>
        public int BuildFetchIntervalSeconds { get; set; }
    }
}

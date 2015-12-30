using System.Collections.Generic;

namespace StackExchange.Opserver
{
    public class TeamCitySettings : Settings<TeamCitySettings>
    {
        public override bool Enabled => Url.HasValue();

        public Dictionary<string, List<string>> ServerMaps { get; set; }

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
        public int BuildFetchIntervalSeconds { get; set; } = 30;
    }
}

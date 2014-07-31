using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public class DataCenters
    {
        private static Dictionary<string, List<IPNet>> _all;

        public static Dictionary<string, List<IPNet>> All
        {
            get { return _all ?? (_all = GetDataCenters()); }
        }

        private static Dictionary<string, List<IPNet>> GetDataCenters()
        {
            if (Current.Settings.CloudFlare != null)
            {
                return Current.Settings.CloudFlare.DataCenters
                    .ToDictionary(
                        dc => dc.Name,
                        dc => dc.Ranges.Select(r => IPNet.Parse(r)).ToList()
                    );
            }
            return new Dictionary<string, List<IPNet>>();
        }
    }
}

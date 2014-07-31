using System.Collections.Generic;
using StackExchange.Opserver.Data.CloudFlare;

namespace StackExchange.Opserver.Views.CloudFlare
{
    public class DashboardModel
    {
        public List<RailgunInstance> Railguns { get; set; } 

        public enum Views
        {
            Overview,
            Railgun,
            DNS,
            Analytics
        }
        public Views View { get; set; }
    }
}
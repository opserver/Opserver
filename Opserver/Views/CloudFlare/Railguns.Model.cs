using System.Collections.Generic;
using StackExchange.Opserver.Data.CloudFlare;

namespace StackExchange.Opserver.Views.CloudFlare
{
    public class RailgunsModel : DashboardModel
    {
        public override Views View => Views.Railgun;

        public List<RailgunInstance> Railguns { get; set; }
    }
}
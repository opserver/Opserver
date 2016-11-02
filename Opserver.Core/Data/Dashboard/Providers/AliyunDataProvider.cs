using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class AliyunDataProvider : DashboardDataProvider<AliyunSettings>
    {
        private const string ECSAPIBaseUrl = "https://ecs.aliyuncs.com";
        private const string CMAPIBaseUrl = "http://metrics.aliyuncs.com";
        private const string DescribeInstanceMonitorData = "DescribeInstanceMonitorData";

        public override List<Node> AllNodes => NodeCache.Data ?? new List<Node>();

        public override IEnumerable<Cache> DataPollers
        {
            get { yield return NodeCache; }
        }

        public override bool HasData => NodeCache.HasData();

        public override int MinSecondsBetweenPolls => 10;

        public override string NodeType => "Aliyun";

        private Cache<List<Node>> _nodeCache;
        public Cache<List<Node>> NodeCache => _nodeCache ?? (_nodeCache = ProviderCache(GetAllAliyunNodesAsync, 60));

        public AliyunDataProvider(AliyunSettings settings) : base(settings)
        { }
    }
}

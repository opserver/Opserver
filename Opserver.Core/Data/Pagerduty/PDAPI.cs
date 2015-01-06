using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Pagerduty
{
    public partial class PDAPI : PollNode
    {
        private static PDAPI _instance;
        public static PDAPI Instance
        {
            get { return _instance ?? (_instance = GetInstance()); }
        }

        public static PDAPI GetInstance()
        {
            var api = new PDAPI(Current.Settings.Pagerduty);
            api.TryAddToGlobalPollers();
            return api;
        }

        public PagerdutySettings Settings { get; internal set; }
        public override string NodeType { get { return "PagerdutyAPI"; } }
        public override int MinSecondsBetweenPolls { get { return 3600; } }
        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return ""; }


        public override IEnumerable<Cache> DataPollers
        {
            get { return null; }
        }

        public PDAPI(PagerdutySettings settings) : base ("Pagerduty API: ")
        {
            Settings = settings;
        }
    }
}

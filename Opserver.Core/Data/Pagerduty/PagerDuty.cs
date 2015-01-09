using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi : PollNode
    {
        private static PagerDutyApi _instance;
        public static PagerDutyApi Instance
        {
            get { return _instance ?? (_instance = GetInstance()); }
        }

        public static PagerDutyApi GetInstance()
        {
            var api = new PagerDutyApi(Current.Settings.PagerDuty);
            api.TryAddToGlobalPollers();
            return api;
        }

        public PagerDutySettings Settings { get; internal set; }
        public override string NodeType { get { return "PagerDutyAPI"; } }
        public override int MinSecondsBetweenPolls { get { return 3600; } }
        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return ""; }
        public string APIKey 
        {
            get { return Settings.APIKey; }
        }


        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return PrimaryOnCall;
                // yield return Incedents;
            }
        }

        public Action<Cache<T>> GetFromPagerDuty<T>(string opName, Func<PagerDutyApi, T> getFromConnection) where T : class
        {
            return UpdateCacheItem("PagerDuty - API: " + opName, () => getFromConnection(this));
        }

        public PagerDutyApi(PagerDutySettings settings) : base ("PagerDuty API: ")
        {
            Settings = settings;
        }

        public T GetFromPagerDuty<T>(string route, NameValueCollection queryString , Func<string, T> getFromJson)
        {
            const string url = "https://stackoverflow.pagerduty.com/api/v1/";
            using (var wb = new WebClient())
            {
                wb.Headers.Add("Token token:", APIKey);
                wb.QueryString = queryString;
                var response = wb.DownloadString(url + route);
                return getFromJson(response);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
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
        public string APIKey 
        {
            get { return Settings.APIKey; }
        }


        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return OnCall;
                yield return Incedents;
            }
        }

        public Action<Cache<T>> GetFromPagerduty<T>(string opName, Func<PDAPI, T> getFromConnection) where T : class
        {
            return UpdateCacheItem("Pagerduty - API: " + opName, () => getFromConnection(this));
        }

        public PDAPI(PagerdutySettings settings) : base ("Pagerduty API: ")
        {
            Settings = settings;
        }

        public T GetFromPagerduty<T>(string route, NameValueCollection queryString , Func<string, T> getFromJson)
        {
            const string url = "https://stackoverflow.pagerduty.com/api/v1/";
            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["token"] = APIKey;
                wb.Headers.Add("Token token:", APIKey);
                wb.QueryString = queryString;
                var response = wb.DownloadString(url + route);
                return getFromJson(response);
            }

        }
    }
}

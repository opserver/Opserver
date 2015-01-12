using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Profiling;

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
            return UpdateCacheItem("PagerDuty - API: " + opName, () => getFromConnection(this), logExceptions:true);
        }

        public PagerDutyApi(PagerDutySettings settings) : base ("PagerDuty API: ")
        {
            Settings = settings;
        }

        public T GetFromPagerDuty<T>(string route, string qs , Func<string, T> getFromJson)
        {
            const string url = "https://stackoverflow.pagerduty.com/api/v1/";
            var fullUri = url + route + "?" + qs;
            using (MiniProfiler.Current.CustomTiming("http", fullUri, "GET"))
            {
                var req = (HttpWebRequest)WebRequest.Create(fullUri);
                req.Headers.Add("Authorization: Token token=" + APIKey);
                using (var resp = req.GetResponse())
                using (var rs = resp.GetResponseStream())
                {
                    if (rs == null) return getFromJson(null);
                    using (var sr = new StreamReader(rs))
                    {
                        return getFromJson(sr.ReadToEnd());
                    }
                }
            }
        }
    }
}

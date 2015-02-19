using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi : SinglePollNode<PagerDutyApi>
    {

        public PagerDutySettings Settings { get; internal set; }
        public override string NodeType { get { return "PagerDutyAPI"; } }
        public override int MinSecondsBetweenPolls { get { return 3600; } }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (AllUsers.ContainsData)
            {
                foreach (var a in GetSchedule())
                    yield return a.MonitorStatus;
            }
            if (Incidents.ContainsData)
            {
                foreach (var i in Incidents.Data)
                    yield return i.MonitorStatus;
            }
            yield return MonitorStatus.Good;
        }
        protected override string GetMonitorStatusReason() { return ""; }
        public string APIKey 
        {
            get { return Settings.APIKey; }
        }


        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return AllUsers;
                yield return Incidents;
            }
        }

        public Action<Cache<T>> GetFromPagerDuty<T>(string opName, Func<PagerDutyApi, T> getFromConnection) where T : class
        {
            return UpdateCacheItem("PagerDuty - API: " + opName, () => getFromConnection(this), logExceptions:true);
        }

        public PagerDutyApi()
        {
            Settings = Current.Settings.PagerDuty;
            CacheItemFetched += (sender, args) => { _scheduleCache = null; };
        }

        /// <summary>
        /// Gets content from the PagerDuty API
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="path">The path to return, including any query string</param>
        /// <param name="getFromJson"></param>
        /// <returns></returns>
        public T GetFromPagerDuty<T>(string path, Func<string, T> getFromJson)
        {
            var url = Settings.APIBaseUrl;
            var fullUri = url + path;
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

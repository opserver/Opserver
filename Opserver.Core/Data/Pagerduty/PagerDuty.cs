using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using StackExchange.Profiling;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi : SinglePollNode<PagerDutyApi>
    {

        public PagerDutySettings Settings { get; internal set; }
        public override string NodeType { get { return "PagerDutyAPI"; } }
        public override int MinSecondsBetweenPolls { get { return 3600; } }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (OnCallUsers.ContainsData)
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
                yield return OnCallUsers;
                yield return Incidents;
            }
        }

        public static class Statuses
        {
            public const string Triggered = "triggered";
            public const string Acknowledged = "acknowledged";
            public const string Resolved = "resolved";

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
        /// <param name="httpMethod"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public T GetFromPagerDuty<T>(string path, Func<string, T> getFromJson, string httpMethod = "GET", object data = null)
        {
            var url = Settings.APIBaseUrl;
            var fullUri = url + path;
            
            using (MiniProfiler.Current.CustomTiming("http", fullUri, httpMethod))
            {
                var req = (HttpWebRequest)WebRequest.Create(fullUri);
                req.Method = httpMethod;
                req.Headers.Add("Authorization: Token token=" + APIKey);

                if (httpMethod == "POST" || httpMethod == "PUT")
                {
                    
                    if (data != null)
                    {
                        var stringData = JSON.Serialize(data);
                        req.ContentType = "application/json";
                        var byteData = new ASCIIEncoding().GetBytes(stringData);
                        req.ContentLength = byteData.Length;
                        var putStream = req.GetRequestStream();
                        putStream.Write(byteData,0,byteData.Length);
                    }
                    


                }
                try
                {
                    var resp = req.GetResponse();
                    using (var rs = resp.GetResponseStream())
                    {
                        if (rs == null) return getFromJson(null);
                        using (var sr = new StreamReader(rs))
                        {
                            return getFromJson(sr.ReadToEnd());
                        }
                    }
                }
                catch (WebException e)
                {
                    try
                    {
                        using (var ers = e.Response.GetResponseStream())
                        {
                            if (ers == null) return getFromJson("fail");
                            using (var er = new StreamReader(ers))
                            {
                                e.AddLoggedData("API Response JSON", er.ReadToEnd());
                            }
                        }
                    }
                    catch
                    {
                        
                    }
                    
                    e.AddLoggedData("Sent Data", JSON.Serialize(data));
                    e.AddLoggedData("Endpoint", fullUri);
                    e.AddLoggedData("Headers", req.Headers.ToString());
                    e.AddLoggedData("Contecnt Type", req.ContentType);

                    Current.LogException(e);
                    return getFromJson("fail");
                }
                


            }
        }

        private Cache<List<PagerDutyPerson>> _allusers;

        public Cache<List<PagerDutyPerson>> AllUsers
        {
            get
            {
                return _allusers ?? (_allusers = new Cache<List<PagerDutyPerson>>()
                {
                    CacheForSeconds = 60 * 60,
                    UpdateCache = UpdateCacheItem(
                        description: "All Users in The Organization",
                        getData: GetAllUsers,
                        logExceptions: true
                        )
                });
            }
        }

        private List<PagerDutyPerson> GetAllUsers()
        {
            return GetFromPagerDuty("users/", getFromJson:
                response => JSON.Deserialize<PagerDutyUserResponse>(response.ToString(), Options.ISO8601).Users);
        }
    }
}

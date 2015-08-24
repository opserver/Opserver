using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Profiling;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi : SinglePollNode<PagerDutyApi>
    {
        internal static Options JilOptions = new Options(
            dateFormat: DateTimeFormat.ISO8601,
            unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC,
            excludeNulls: true
            );

        public PagerDutySettings Settings { get; internal set; }
        public override string NodeType => "PagerDutyAPI";
        public override int MinSecondsBetweenPolls => 3600;

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
        public string APIKey => Settings.APIKey;

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return OnCallUsers;
                yield return Incidents;
            }
        }
        
        public Action<Cache<T>> GetFromPagerDutyAsync<T>(string opName, Func<PagerDutyApi, Task<T>> getFromPD) where T : class
        {
            return UpdateCacheItem("PagerDuty - API: " + opName, () => getFromPD(this), logExceptions:true);
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
        public async Task<T> GetFromPagerDutyAsync<T>(string path, Func<string, T> getFromJson, string httpMethod = "GET", object data = null)
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
                        var stringData = JSON.Serialize(data, JilOptions);
                        var byteData = new ASCIIEncoding().GetBytes(stringData);
                        req.ContentType = "application/json";
                        req.ContentLength = byteData.Length;
                        var putStream = await req.GetRequestStreamAsync();
                        await putStream.WriteAsync(byteData, 0, byteData.Length);
                    }
                }
                try
                {
                    var resp = await req.GetResponseAsync();
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
                    catch { }

                    Current.LogException(
                        e.AddLoggedData("Sent Data", JSON.Serialize(data, JilOptions))
                            .AddLoggedData("Endpoint", fullUri)
                            .AddLoggedData("Headers", req.Headers.ToString())
                            .AddLoggedData("Contecnt Type", req.ContentType));
                    return getFromJson("fail");
                }
            }
        }

        private Cache<List<PagerDutyPerson>> _allusers;

        public Cache<List<PagerDutyPerson>> AllUsers => _allusers ?? (_allusers = new Cache<List<PagerDutyPerson>>()
        {
            CacheForSeconds = 60 * 60,
            UpdateCache = UpdateCacheItem(
                description: "All Users in The Organization",
                getData: GetAllUsers,
                logExceptions: true
                )
        });

        public PagerDutyPerson GetPerson(string id) => AllUsers.Data.FirstOrDefault(u => u.Id == id);

        private Task<List<PagerDutyPerson>> GetAllUsers()
        {
            return GetFromPagerDutyAsync("users/", r => JSON.Deserialize<PagerDutyUserResponse>(r.ToString(), JilOptions).Users);
        }
    }
}

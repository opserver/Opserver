using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Profiling;
using Jil;
using System.Diagnostics;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyAPI : SinglePollNode<PagerDutyAPI>
    {
        internal static readonly Options JilOptions = new Options(
            dateFormat: DateTimeFormat.ISO8601,
            unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC,
            excludeNulls: true
            );

        public PagerDutySettings Settings { get; internal set; }
        public override string NodeType => nameof(PagerDutyAPI);
        public override int MinSecondsBetweenPolls => 3600;

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (OnCallInfo.ContainsData)
            {
                foreach (var a in OnCallInfo.Data)
                    yield return a.MonitorStatus;
            }
            if (Incidents.ContainsData)
            {
                foreach (var i in Incidents.Data)
                    yield return i.MonitorStatus;
            }
            yield return MonitorStatus.Good;
        }

        protected override string GetMonitorStatusReason() => "";
        public string APIKey => Settings.APIKey;

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return AllUsers;
                yield return OnCallInfo;
                yield return Incidents;
                yield return AllSchedules;
                yield return AllUsers;
            }
        }

        private Cache<T> GetPagerDutyCache<T>(
            TimeSpan cacheDuration,
            Func<Task<T>> get,
            bool logExceptions = true,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            ) where T : class
        {
            return new Cache<T>(this, "PagerDuty - API: " + memberName,
                cacheDuration,
                get,
                logExceptions: logExceptions,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        public PagerDutyAPI()
        {
            Settings = Current.Settings.PagerDuty;
        }

        /// <summary>
        /// Gets content from the PagerDuty API.
        /// </summary>
        /// <typeparam name="T">The Type to return (deserialized from the returned JSON).</typeparam>
        /// <param name="path">The path to return, including any query string.</param>
        /// <param name="getFromJson">The deserialize function.</param>
        /// <param name="httpMethod">The HTTP method to use for this API call.</param>
        /// <param name="data">Data to serialize for the request.</param>
        /// <param name="extraHeaders">Headers to add to the API request.</param>
        /// <returns>The deserialized content from the PagerDuty API.</returns>
        public async Task<T> GetFromPagerDutyAsync<T>(string path, Func<string, T> getFromJson, string httpMethod = "GET", object data = null, Dictionary<string,string> extraHeaders = null)
        {
            const string baseUrl = "https://api.pagerduty.com/";
            var fullUri = baseUrl + path;

            using (MiniProfiler.Current.CustomTiming("http", fullUri, httpMethod))
            {
                var req = (HttpWebRequest)WebRequest.Create(fullUri);
                req.Method = httpMethod;
                req.Accept = "application/vnd.pagerduty+json;version=2";
                req.Headers.Add("Authorization: Token token=" + APIKey);
                if (extraHeaders != null)
                {
                    foreach (var h in extraHeaders.Keys)
                    {
                        req.Headers.Add(h, extraHeaders[h]);
                    }
                }

                if (httpMethod == "POST" || httpMethod == "PUT")
                {
                    if (data != null)
                    {
                        var stringData = JSON.Serialize(data, JilOptions);
                        var byteData = new ASCIIEncoding().GetBytes(stringData);
                        req.ContentType = "application/json";
                        req.ContentLength = byteData.Length;
                        var putStream = await req.GetRequestStreamAsync().ConfigureAwait(false);
                        await putStream.WriteAsync(byteData, 0, byteData.Length).ConfigureAwait(false);
                    }
                }
                try
                {
                    var resp = await req.GetResponseAsync().ConfigureAwait(false);
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
                    catch (Exception rex)
                    {
                        // ignored, best effort
                        Trace.WriteLine(rex);
                    }

                    Current.LogException(
                        e.AddLoggedData("Sent Data", JSON.Serialize(data, JilOptions))
                         .AddLoggedData("Endpoint", fullUri)
                         .AddLoggedData("Headers", req.Headers.ToString())
                         .AddLoggedData("Content Type", req.ContentType));
                    return getFromJson("fail");
                }
            }
        }

        private Cache<List<PagerDutyPerson>> _allusers;
        public Cache<List<PagerDutyPerson>> AllUsers =>
            _allusers ?? (_allusers = GetPagerDutyCache(60.Minutes(),
                    () => GetFromPagerDutyAsync("users?include[]=contact_methods", r => JSON.Deserialize<PagerDutyUserResponse>(r, JilOptions).Users))
            );
    }
}

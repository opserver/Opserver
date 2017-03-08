using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class CloudFlareAPI : SinglePollNode<CloudFlareAPI>
    {
        private const string APIBaseUrl = "https://api.cloudflare.com/client/v4/";

        public CloudFlareSettings Settings { get; internal set; }
        public string Email => Settings.Email;
        public string APIKey => Settings.APIKey;

        public override string NodeType => nameof(CloudFlareAPI);
        public override int MinSecondsBetweenPolls => 5;

        private static readonly Options JilOptions = Options.ISO8601;

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return Zones;
                yield return DNSRecords;
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return ""; }

        public CloudFlareAPI()
        {
            Settings = Current.Settings.CloudFlare;
        }

        private Cache<T> GetCloudFlareCache<T>(
            TimeSpan cacheDuration,
            Func<Task<T>> get,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            ) where T : class, new()
        {
            return new Cache<T>(this, "CloudFlare - API: " + memberName,
                cacheDuration,
                get,
                logExceptions: true,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        /// <summary>
        /// Gets a response from the CloudFlare API via GET
        /// </summary>
        /// <param name="path">The API path to call, e.g. zones</param>
        /// <param name="values">Variables to pass into this method</param>
        /// <returns>The CloudFlare API response</returns>
        public async Task<T> Get<T>(string path, NameValueCollection values = null)
        {
            using (var wc = GetWebClient())
            {
                var url = new Uri($"{APIBaseUrl}{path}{(values != null ? "?" + values : "")}");
                var rawResult = await wc.DownloadStringTaskAsync(url).ConfigureAwait(false);
                return JSON.Deserialize<CloudFlareResult<T>>(rawResult, JilOptions).Result;
            }
        }

        /// <summary>
        /// Gets a response from the CloudFlare API via POST
        /// </summary>
        /// <param name="path">The API path to call, e.g. zones</param>
        /// <param name="values">Variables to pass into this method</param>
        /// <returns>The CloudFlare API response</returns>
        public T Post<T>(string path, NameValueCollection values = null) => Action<T>("POST", path, values);

        /// <summary>
        /// Gets a response from the CloudFlare API via DELETE
        /// </summary>
        /// <param name="path">The API path to call, e.g. zones</param>
        /// <param name="values">Variables to pass into this method</param>
        /// <returns>The CloudFlare API response</returns>
        public T Delete<T>(string path, NameValueCollection values = null) => Action<T>("DELETE", path, values);

        private T Action<T>(string method, string path, NameValueCollection values)
        {
            using (var wc = GetWebClient())
            {
                var url = new Uri($"{APIBaseUrl}{path}");
                var rawResult = wc.UploadValues(url, method, values);
                var resultString = Encoding.ASCII.GetString(rawResult);
                return JSON.Deserialize<T>(resultString, JilOptions);
            }
        }

        private WebClient GetWebClient(string contentType = "application/json") =>
            new WebClient
            {
                Headers =
                {
                    ["X-Auth-Email"] = Email,
                    ["X-Auth-Key"] = APIKey,
                    ["Content-Type"] = contentType
                }
            };

        public override string ToString() => string.Concat("CloudFlare API: ", Email);
    }
}

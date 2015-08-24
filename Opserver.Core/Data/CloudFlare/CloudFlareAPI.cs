using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class CloudFlareAPI : SinglePollNode<CloudFlareAPI>
    {
        public CloudFlareSettings Settings { get; internal set; }
        public string Email => Settings.Email;
        public string APIKey => Settings.APIKey;

        public override string NodeType => "CloudFlareAPI";
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

        public Action<Cache<T>> CloudFlareFetch<T>(string opName, Func<CloudFlareAPI, Task<T>> get) where T : class
        {
            return UpdateCacheItem("CloudFlare - API: " + opName, () => get(this), logExceptions: true);
        }
        
        const string apiBaseUrl = "https://api.cloudflare.com/client/v4/";

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
                var url = new Uri($"{apiBaseUrl}{path}{(values != null ? "?" + values : "")}");
                var rawResult = await wc.DownloadStringTaskAsync(url);
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

        private T Action<T>(string method, string path, NameValueCollection values = null)
        {
            using (var wc = GetWebClient())
            {
                var url = new Uri($"{apiBaseUrl}{path}");
                var rawResult = wc.UploadValues(url, method, values);
                var resultString = Encoding.ASCII.GetString(rawResult);
                return JSON.Deserialize<T>(resultString, JilOptions);
            }
        }

        private WebClient GetWebClient(string contentType = "application/json")
        {
            return new WebClient
            {
                Headers =
                {
                    ["X-Auth-Email"] = Email,
                    ["X-Auth-Key"] = APIKey,
                    ["Content-Type"] = contentType
                }
            };
        }

        public override string ToString() => string.Concat("CloudFlare API: ", Email);
    }
}

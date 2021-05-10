using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using StackExchange.Utils;

namespace Opserver.Data.Cloudflare
{
    public partial class CloudflareAPI : PollNode<CloudflareModule>
    {
        private const string APIBaseUrl = "https://api.cloudflare.com/client/v4/";

        public CloudflareSettings Settings => Module.Settings;
        public string Email => Settings.Email;
        public string APIKey => Settings.APIKey;

        public override string NodeType => nameof(CloudflareAPI);
        public override int MinSecondsBetweenPolls => 5;

        private Dictionary<string, string> _requestHeaders;
        private IDictionary<string, string> RequestHeaders => _requestHeaders ??= new Dictionary<string, string>
        {
            ["X-Auth-Email"] = Email,
            ["X-Auth-Key"] = APIKey,
            ["Content-Type"] = "application/json"
        };

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

        public CloudflareAPI(CloudflareModule module) : base(module, nameof(CloudflareAPI)) { }

        private Cache<T> GetCloudflareCache<T>(
            TimeSpan cacheDuration,
            Func<Task<T>> get,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            ) where T : class, new()
        {
            return new Cache<T>(this, "Cloudflare - API: " + memberName,
                cacheDuration,
                get,
                logExceptions: true,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        /// <summary>
        /// Gets a response from the Cloudflare API via GET
        /// </summary>
        /// <typeparam name="T">The type to deserialize from the result JSON</typeparam>
        /// <param name="path">The API path to call, e.g. zones</param>
        /// <param name="values">Variables to pass into this method</param>
        /// <returns>The Cloudflare API response</returns>
        public async Task<T> Get<T>(string path, NameValueCollection values = null)
        {
            var result = await Http.Request(APIBaseUrl + path + (values != null ? "?" + values : ""))
                                   .AddHeaders(RequestHeaders)
                                   .ExpectJson<CloudflareResult<T>>()
                                   .GetAsync();
            return result.Data.Result;
        }

        /// <summary>
        /// Gets a response from the Cloudflare API via POST
        /// </summary>
        /// <typeparam name="T">The type to serialize and POST to Cloudflare as JSON</typeparam>
        /// <param name="path">The API path to call, e.g. zones</param>
        /// <param name="values">Variables to pass into this method</param>
        /// <returns>The Cloudflare API response</returns>
        public async Task<T> PostAsync<T>(string path, NameValueCollection values = null)
        {
            var result = await Http.Request(APIBaseUrl + path)
                                   .AddHeaders(RequestHeaders)
                                   .SendForm(values)
                                   .ExpectJson<T>()
                                   .PostAsync();
            return result.Data;
        }

        /// <summary>
        /// Gets a response from the Cloudflare API via DELETE
        /// </summary>
        /// <typeparam name="T">The type to serialize and delete from Cloudflare</typeparam>
        /// <param name="path">The API path to call, e.g. zones</param>
        /// <param name="values">Variables to pass into this method</param>
        /// <returns>The Cloudflare API response</returns>
        public async Task<T> DeleteAsync<T>(string path, NameValueCollection values = null)
        {
            var result = await Http.Request(APIBaseUrl + path)
                                   .AddHeaders(RequestHeaders)
                                   .SendForm(values)
                                   .ExpectJson<T>()
                                   .DeleteAsync();
            return result.Data;
        }

        public override string ToString() => string.Concat("Cloudflare API: ", Email);
    }
}

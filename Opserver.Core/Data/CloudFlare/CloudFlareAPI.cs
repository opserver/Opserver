using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class CloudFlareAPI : SinglePollNode<CloudFlareAPI>
    {
        public CloudFlareSettings Settings { get; internal set; }
        public string Email { get { return Settings.Email; } }
        public string APIKey { get { return Settings.APIKey; } }

        public override string NodeType { get { return "CloudFlareAPI"; } }
        public override int MinSecondsBetweenPolls { get { return 5; } }

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

        public Action<Cache<T>> GetFromCloudFlare<T>(string opName, Func<CloudFlareAPI, T> getFromConnection) where T : class
        {
            return UpdateCacheItem("CloudFlare - API: " + opName, () => getFromConnection(this));
        }

        /// <summary>
        /// If you just want to check the CloudFlare response for result: success
        /// </summary>
        private Func<string, bool> CheckForSuccess = response =>
        {
            var parsedResponse = JObject.Parse(response);
            return parsedResponse["result"].Value<string>() == "success";
        };

        /// <summary>
        /// Gets a response from the CloudFlare API via POST
        /// </summary>
        /// <param name="action">The action to call</param>
        /// <param name="getFromJson">Function to get the object from Json</param>
        /// <param name="zone">The zone to perform against, if relevant</param>
        /// <param name="variables">Variables to pass into this param beyond the email, token and action</param>
        /// <param name="start">The start index to pull</param>
        /// <returns>The CloudFlare API response</returns>
        /// <remarks>
        /// This totally doesn't handle paging yet, ignoring until CloudFlare API v2 unless we need it
        /// </remarks>
        public T GetFromCloudFlare<T>(string action, Func<string, T> getFromJson, string zone = null, NameValueCollection variables = null, int start = 0)
        {
            const string url = "https://www.cloudflare.com/api_json.html";
            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                if (variables != null)
                {
                    foreach (var k in variables.AllKeys)
                    {
                        data[k] = variables[k];
                    }
                }
                data["email"] = Email;
                data["tkn"] = APIKey;
                data["a"] = action;
                if (zone.HasValue())
                    data["z"] = zone;
                if (start > 0)
                    data["o"] = start.ToString();

                var response = wb.UploadValues(url, "POST", data);
                var responeString = Encoding.Default.GetString(response);
                return getFromJson(responeString);
            }
        }

        public override string ToString()
        {
            return string.Concat("CloudFlare API: ", Email);
        }
    }
}

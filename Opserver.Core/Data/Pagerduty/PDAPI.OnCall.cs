using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Jil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StackExchange.Opserver.Data.Pagerduty
{
    public partial class PDAPI
    {
        private Cache<string> _oncallnow;
        public Cache<string> onCallNow
        {
            get { return null; }
        }

        private Cache<List<PDPerson>> _allusers;


        public Cache<List<PDPerson>> allUsers
        {
            get
            {
                return _allusers ?? (_allusers = new Cache<List<PDPerson>>()
                {
                    CacheForSeconds = 60*60,
                    UpdateCache =
                        GetFromPagerduty("users/", new NameValueCollection() {{"include", "contact_methods"}},
                            getFromJson:
                                response =>
                                {
                                    return JSON.Deserialize<PDUserResponse>(response).Users;

                                })
                });

            }
        }

    }

    public class PDUserResponse
    {
        [DataMember(Name = "users")]
        public List<PDPerson> Users;
    }

    public class PDPerson
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("name")]
        public string FullName { get; set; }
        [JsonProperty("email")]
        public string email { get; set; }
        [JsonProperty("time_zone")]
        public string TimeZone { get; set; }
        [JsonProperty("color")]
        public string color { get; set; }
        [JsonProperty("role")]
        public string role { get; set; }
        [JsonProperty("avatar_url")]
        public string Avatar { get; set; }
        [JsonProperty("user_url")]
        public string UserURL { get; set; }

    }

    public class Contact
    {
        public string id {get; set; }
        public string label { get; set; }
        public string address { get; set; }
        public string type { get; set; }
    }



}

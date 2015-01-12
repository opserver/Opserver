using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Jil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {

        
        private Cache<PdPerson> _primaryoncall;

        public Cache<PdPerson> PrimaryOnCall
        {
            get
            {
                return _primaryoncall ?? (_primaryoncall = new Cache<PdPerson>()
                {
                    CacheForSeconds = 60*60,
                    UpdateCache = UpdateCacheItem(
                        description: "Pager Duty Primary On Call",
                        getData: GetOnCall,
                        logExceptions: true
                        )
                });
            }
        }

        private Cache<List<PdPerson>> _allusers;
        public Cache<List<PdPerson>> AllUsers
        {
            get
            {
                return _allusers ?? (_allusers = new Cache<List<PdPerson>>()
                {
                    CacheForSeconds = 60*60,
                    UpdateCache = UpdateCacheItem(
                        description: "On Call info",
                        getData: GetAllUsers,
                        logExceptions: true
                        )
                });

            }
        }

        public PdPerson GetOnCall()
        {
            const string primaryScheduleId = "P7I0G4O";
            var test = AllUsers;
            foreach (var p in AllUsers.Data)
            {
                if (p.OnCallSchedule.Any(oc => oc.EscalationLevel == 1 && oc.Policy["id"] == primaryScheduleId))
                {
                    return p;
                }
            }
            return null;
        }

        private List<PdPerson> GetAllUsers()
        {
            var users = GetFromPagerDuty("users/on_call/", "include[]=contact_methods", getFromJson:
                response =>
                {
                    Debug.WriteLine(response);
                    var myResp = JSON.Deserialize<PdUserResponse>(response.ToString(), Options.ISO8601).Users;
                    return myResp;

                });
            return users;
        }

    }


    public class PdUserResponse
    {
        [DataMember(Name = "users")]
        public List<PdPerson> Users;
    }

    public class PdPerson
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string FullName { get; set; }
        [DataMember(Name = "email")]
        public string Email { get; set; }
        [DataMember(Name = "time_zone")]
        public string TimeZone { get; set; }
        [DataMember(Name = "color")]
        public string Color { get; set; }
        [DataMember(Name = "role")]
        public string Role { get; set; }
        [DataMember(Name = "avatar_url")]
        public string Avatar { get; set; }
        [DataMember(Name = "user_url")]
        public string UserUrl { get; set; }
        [DataMember(Name = "contact_methods")]
        public List<Contact> ContactMethods { get; set; }
        [DataMember(Name = "on_call")]
        public List<OnCall> OnCallSchedule { get; set; } 


    }

    public class Contact
    {
        [DataMember(Name = "id")]
        public string Id {get; set; }
        [DataMember(Name = "label")]
        public string Label { get; set; }
        [DataMember(Name = "address")]
        public string Address { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
    }

    public class OnCall
    {
        [DataMember(Name = "level")]
        public int EscalationLevel { get; set; }
        [DataMember(Name = "start")]
        public DateTime? OnCallStart { get; set; }
        [DataMember(Name = "end")]
        public DateTime? OnCallend { get; set; }
        [DataMember(Name = "escalation_policy")]
        public Dictionary<string,string> Policy { get; set; } 
    }
  
}

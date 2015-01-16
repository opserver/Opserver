using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {

        
        private Cache<PagerDutyPerson> _primaryoncall;
        public Cache<PagerDutyPerson> PrimaryOnCall
        {
            get
            {
                return _primaryoncall ?? (_primaryoncall = new Cache<PagerDutyPerson>()
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

        private Cache<PagerDutyPerson> _secondaryoncall;
        public Cache<PagerDutyPerson> SecondaryOnCall
        {
            get
            {
                return _secondaryoncall ?? (_secondaryoncall = new Cache<PagerDutyPerson>()
                {
                    CacheForSeconds = 60*60,
                    UpdateCache = UpdateCacheItem(
                        description: "Pager Duty Backup On Call",
                        getData: GetEscOnCall,
                        logExceptions: true
                        )
                });
            }
        }

        private Cache<List<PagerDutyPerson>> _allusers;
        public Cache<List<PagerDutyPerson>> AllUsers
        {
            get
            {
                return _allusers ?? (_allusers = new Cache<List<PagerDutyPerson>>()
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

        // TODO: We need to able able to handle when people have more than one on call schedule
        public PagerDutyPerson GetOnCall()
        {
            return AllUsers.Data.FirstOrDefault(p => p.Schedule[0].EscalationLevel == 1);
        }

        public PagerDutyPerson GetEscOnCall()
        {
            return AllUsers.Data.FirstOrDefault(p => p.Schedule[0].EscalationLevel == 2);
        }

        private List<PagerDutyPerson> GetAllUsers()
        {
            var users = GetFromPagerDuty("users/on_call/", "include[]=contact_methods", getFromJson:
                response =>
                {
                    var myResp = JSON.Deserialize<PagerDutyUserResponse>(response.ToString(), Options.ISO8601).Users;
                    return myResp;

                });
            return users;
        }

    }


    public class PagerDutyUserResponse
    {
        [DataMember(Name = "users")]
        public List<PagerDutyPerson> Users;
    }

    public class PagerDutyPerson
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
        public List<PagerDutyContact> ContactMethods { get; set; }
        [DataMember(Name = "on_call")]
        public List<PagerDutyOnCall> Schedule { get; set; } 


    }

    public class PagerDutyContact
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

    public class PagerDutyOnCall
    {
        [DataMember(Name = "level")]
        public int EscalationLevel { get; set; }
        [DataMember(Name = "start")]
        public DateTime? StartDate { get; set; }
        [DataMember(Name = "end")]
        public DateTime? EndDate { get; set; }
        [DataMember(Name = "escalation_policy")]
        public Dictionary<string,string> Policy { get; set; } 
    }
  
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {
        // TODO: We need to able able to handle when people have more than one on call schedule
        public PagerDutyPerson PrimaryOnCall
        {
            get { return AllUsers.Data.FirstOrDefault(p => p.CurrentEscalationLevel == 1); }
        }

        public PagerDutyPerson SecondaryOnCall
        {
            get { return AllUsers.Data.FirstOrDefault(p => p.CurrentEscalationLevel == 2); }
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

        private List<PagerDutyPerson> GetAllUsers()
        {
            return GetFromPagerDuty("users/on_call?include[]=contact_methods", getFromJson:
                response => JSON.Deserialize<PagerDutyUserResponse>(response.ToString(), Options.ISO8601).Users);
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
        public string AvatarUrl { get; set; }
        [DataMember(Name = "user_url")]
        public string UserUrl { get; set; }
        [DataMember(Name = "contact_methods")]
        public List<PagerDutyContact> ContactMethods { get; set; }
        [DataMember(Name = "on_call")]
        public List<OnCall> Schedule { get; set; }

        private string _phone;
        public string Phone
        {
            get
            {
                if (_phone == null)
                {
                    var m = ContactMethods.FirstOrDefault(cm => cm.Type == "phone" || cm.Type == "SMS");
                    _phone = m != null ? m.FormattedAddress : "";
                }
                return _phone;
            }
        }

        public bool IsPrimary
        {
            get { return CurrentEscalationLevel == 1; }
        }

        public int? CurrentEscalationLevel
        {
            get { return Schedule != null && Schedule.Count > 0 ? Schedule[0].EscalationLevel : (int?)null; }
        }

        public string CurrentEscalationLevelDescription
        {
            get
            {
                switch (CurrentEscalationLevel)
                {
                    case 1:
                        return "Primary";
                    case 2:
                        return "Secondary";
                    case 3:
                        return "Third";
                    case null:
                        return "Unknown";
                    default:
                        return CurrentEscalationLevel + "th";
                }
            }
        }
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

        public string FormattedAddress
        {
            get
            {
                switch (Type)
                {
                    case "SMS":
                    case "phone":
                        // I'm sure no one outside the US uses this...
                        return Regex.Replace(Address, @"(\d{3})(\d{3})(\d{4})", "$1-$2-$3");
                    default:
                        return Address;
                }
            }
        }
    }

    public class OnCall
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

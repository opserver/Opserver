using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {
        // TODO: We need to able able to handle when people have more than one on call schedule
        public PagerDutyPerson PrimaryOnCall
        {
            get { return OnCallUsers.Data.FirstOrDefault(p => p.EscalationLevel == 1); }
        }

        public PagerDutyPerson SecondaryOnCall
        {
            get { return OnCallUsers.Data.FirstOrDefault(p => p.EscalationLevel == 2); }
        }

        private Cache<List<PagerDutyPerson>> _oncallusers;
        public Cache<List<PagerDutyPerson>> OnCallUsers => _oncallusers ?? (_oncallusers = new Cache<List<PagerDutyPerson>>()
        {
            CacheForSeconds = 60*60,
            UpdateCache = UpdateCacheItem(
                description: "On Call info",
                getData: GetOnCallUsers,
                logExceptions: true
                )
        });

        private Task<List<PagerDutyPerson>> GetOnCallUsers()
        {
            return GetFromPagerDutyAsync("users/on_call?include[]=contact_methods", getFromJson:
                response => JSON.Deserialize<PagerDutyUserResponse>(response.ToString(), JilOptions).Users);
        }

        private List<OnCallAssignment> _scheduleCache;

        public List<OnCallAssignment> GetSchedule()
        {
            if (_scheduleCache == null)
            {
                var result = new List<OnCallAssignment>();
                var overrides = PrimaryScheduleOverrides.SafeData(true);
                if (!OnCallUsers.HasData()) return result;
                foreach (var p in OnCallUsers.Data)
                {
                    if (p.Schedule == null) continue;
                    for (var i = 0; i < p.Schedule.Count; i++)
                    {
                        var isOverride = overrides.Any(o => o.StartTime <= DateTime.UtcNow && DateTime.UtcNow <= o.EndTime && o.User.Id == p.Id);
                        result.Add(new OnCallAssignment { Person = p, Schedule = p.Schedule[i], IsOverride = isOverride });
                    }
                }
                result.Sort((a, b) => a.EscalationLevel.GetValueOrDefault(int.MaxValue).CompareTo(b.EscalationLevel.GetValueOrDefault(int.MaxValue)));

                if (result.Count > 1 && result[0].Person.Id == result[1].Person.Id)
                {
                    result[1].MonitorStatus = MonitorStatus.Warning;
                    result[1].MonitorStatusReason = "Primary and secondary on call are the same";
                }

                _scheduleCache = result;
            }
            return _scheduleCache;
        }
    }

    public class OnCallAssignment : IMonitorStatus
    {
        public PagerDutyPerson Person { get; set; }
        public OnCall Schedule { get; set; }
        public bool IsOverride { get; set; }

        public int? EscalationLevel => Schedule?.EscalationLevel;

        public bool IsPrimary => EscalationLevel == 1;

        public string EscalationLevelDescription => PagerDutyPerson.GetEscalationLevelDescription(EscalationLevel);

        public MonitorStatus MonitorStatus { get; internal set; }

        public string MonitorStatusReason { get; internal set; }
    }

    public class PagerDutyUserResponse
    {
        [DataMember(Name = "users")]
        public List<PagerDutyPerson> Users;
    }

    public class PagerDutySingleUserResponse
    {
        [DataMember(Name = "user")] public PagerDutyPerson User;
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
                    // The PagerDuty API does not always return a full contact. HANDLE IT.
                    var m = ContactMethods?.FirstOrDefault(cm => cm.Type == "phone" || cm.Type == "SMS");
                    _phone = m != null ? m.FormattedAddress : "n/a";
                }
                return _phone;
            }
        }

        public int? EscalationLevel => Schedule != null && Schedule.Count > 0 ? Schedule[0].EscalationLevel : (int?)null;

        public static string GetEscalationLevelDescription(int? level)
        {
            switch (level)
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
                    return level + "th";
            }
        }

        private string _emailusername;
        public string EmailUserName
        {
            get
            {
                return _emailusername = (_emailusername ?? (Email.HasValue() ? Email.Split(StringSplits.AtSign)[0] : ""));
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
                        // we will have to fix this soon
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

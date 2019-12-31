using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jil;

namespace Opserver.Data.PagerDuty
{
    public partial class PagerDutyAPI
    {
        // TODO: We need to able able to handle when people have more than one on call schedule
        public PagerDutyPerson PrimaryOnCall =>
            OnCallInfo.Data?.FirstOrDefault(p => p.EscalationLevel == 1)?.AssignedUser;

        public PagerDutyPerson SecondaryOnCall =>
            OnCallInfo.Data?.FirstOrDefault(p => p.EscalationLevel == 2)?.AssignedUser;

        private Cache<List<OnCall>> _oncallinfo;
        public Cache<List<OnCall>> OnCallInfo => _oncallinfo ??= new Cache<List<OnCall>>(
            this,
            "On Call information for Pagerduty",
            5.Minutes(),
            getData: GetOnCallUsers,
            logExceptions: true);

        public List<OnCall> GetOnCall(int? maxPerSchedule = null) =>
            OnCallInfo.SafeData(true).Where(c => c.EscalationLevel.GetValueOrDefault(int.MaxValue) <= (maxPerSchedule ?? Settings.OnCallToShow)).ToList();

        private async Task<List<OnCall>> GetOnCallUsers()
        {
            try
            {
                var users = await GetFromPagerDutyAsync("oncalls", getFromJson:
                    response => JSON.Deserialize<PagerDutyOnCallResponse>(response, JilOptions).OnCallInfo);

                users.Sort((a, b) =>
                    a.EscalationLevel.GetValueOrDefault(int.MaxValue)
                        .CompareTo(b.EscalationLevel.GetValueOrDefault(int.MaxValue)));

                // Loop over each schedule
                foreach (var p in users.GroupBy(u => u.PolicyTitle))
                {
                    foreach (var u in p)
                    {
                        u.Module = Module;
                        u.SharedLevelCount = p.Count(tu => tu.EscalationLevel == u.EscalationLevel);
                        if (p.Count(pu => pu.User?.Id == u.User?.Id) > 1)
                        {
                            u.MonitorStatus = MonitorStatus.Warning;
                            u.MonitorStatusReason = "Multiple escalations for the same user";
                        }
                    }
                }

                return users;
            }
            catch (DeserializationException de)
            {
                de.AddLoggedData("Snippet After", de.SnippetAfterError)
                  .AddLoggedData("Message", de.Message)
                  .Log();
                return null;
            }
        }
    }

    public class PagerDutyOnCallResponse
    {
        [DataMember(Name = "oncalls")]
        public List<OnCall> OnCallInfo { get; set; }
    }

    public class PagerDutyUserResponse
    {
        [DataMember(Name = "users")]
        public List<PagerDutyPerson> Users { get; set; }
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
        public List<PagerDutyContactMethod> ContactMethods { get; set; }

        private string _phone;
        public string Phone
        {
            get
            {
                if (_phone == null)
                {
                    // The PagerDuty API does not always return a full contact. HANDLE IT.
                    var m = ContactMethods?.FirstOrDefault(cm => cm.Type == "phone_contact_method" || cm.Type == "sms_contact_method");
                    _phone = m?.FormattedAddress ?? "n/a";
                }
                return _phone;
            }
        }

        [DataMember(Name = "escalation_level")]
        public int? EscalationLevel { get; set; }

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
                    return level.ToString() + "th";
            }
        }

        private string _emailusername;
        public string EmailUserName => _emailusername ??= Email.HasValue() ? Email.Split(StringSplits.AtSign)[0] : "";
    }

    public class PagerDutyContactMethod
    {
        [DataMember(Name = "id")]
        public string Id {get; set; }
        [DataMember(Name = "label")]
        public string Label { get; set; }
        [DataMember(Name="address")]
        public string Address { get; set; }
        [DataMember(Name="country_code")]
        public int? CountryCode { get; set; }
        public string FormattedAddress
        {
            get
            {
                var sb = StringBuilderCache.Get();
                if (CountryCode.HasValue)
                {
                    sb.Append("+").Append(CountryCode).Append(" ");
                }
                switch (Type)
                {
                    case "sms_contact_method":
                    case "phone_contact_method":
                        switch (CountryCode)
                        {
                            case 1: // US
                                sb.Append(Regex.Replace(Address, @"(\d{3})(\d{3})(\d{4})", "($1) $2-$3"));
                                break;
                            case 61: // AU
                                sb.Append(Regex.Replace(Address, @"(\d{4})(\d{4})", "$1 $2"));
                                break;
                            // Do we want to increase our deployment payload by 50% to handle phone number formatting here?
                            // The options are crazy bad, so just doing a little for the moment...
                            default:
                                sb.Append(Address);
                                break;
                        }
                        break;
                    default:
                        sb.Append(Address);
                        break;
                }
                return sb.ToStringRecycle();
            }
        }

        [DataMember(Name = "type")]
        public string Type { get; set; }
    }

    public class OnCallUser
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "summary")]
        public string Name { get; set; }
        [DataMember(Name = "html_url")]
        public string Url { get; set; }
    }

    public class OnCallEscalationPolicy
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "summary")]
        public string Title { get; set; }
        [DataMember(Name = "html_url")]
        public string Url { get; set; }
    }

    public class OnCallSchedule
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "summary")]
        public string Title { get; set; }
        [DataMember(Name = "html_url")]
        public string Url { get; set; }
    }

    public class OnCall : IMonitorStatus
    {
        internal PagerDutyModule Module { private get; set; }
        [DataMember(Name = "escalation_level")]
        public int? EscalationLevel { get; set; }
        [DataMember(Name = "start")]
        public DateTime? StartDate { get; set; }
        [DataMember(Name = "end")]
        public DateTime? EndDate { get; set; }
        [DataMember(Name = "escalation_policy")]
        public OnCallEscalationPolicy Policy { get; set; }
        [DataMember(Name = "user")]
        public OnCallUser User { get; set; }
        [DataMember(Name = "schedule")]
        public OnCallSchedule Schedule { get; set; }

        public string ScheduleTitle => Schedule?.Title ?? "Default";
        public string PolicyTitle => Policy?.Title;

        public PagerDutyPerson AssignedUser => Module.API.AllUsers.Data?.FirstOrDefault(u => u.Id == User.Id);

        public bool IsOverride =>
            Module.API.PrimaryScheduleOverrides.Data?.Any(
                o => o.StartTime <= DateTime.UtcNow && DateTime.UtcNow <= o.EndTime && o.User.Id == AssignedUser.Id) ?? false;

        public bool IsPrimary => EscalationLevel == 1;
        public bool IsOnlyPrimary => IsPrimary && SharedLevelCount < 2;

        /// <summary>
        /// The number of people that share this escalation level
        /// </summary>
        public int SharedLevelCount { get; set; }

        public string EscalationLevelDescription
        {
            get
            {
                if (EscalationLevel.HasValue)
                {
                    switch (EscalationLevel.Value)
                    {
                        case 1:
                            return "Primary";
                        case 2:
                            return "Secondary";
                        case 3:
                            return "Third";
                        default:
                            return EscalationLevel.Value + "th";
                    }
                }

                return "unknown";
            }
        }

        public string SharedLevelDescription => SharedLevelCount > 1 ? " (1 of " + SharedLevelCount + ")" : null;

        public MonitorStatus MonitorStatus { get; set; }

        public string MonitorStatusReason { get; set; }
    }

    public class PagerDutyInfoReference
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
        [DataMember(Name = "self")]
        public string ApiLink { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {
        public PagerDutySchedule PrimarySchedule
        {
            get
            {
                var data = AllSchedules.SafeData(true);
                // Many people likely have 1 schedule, or the primary is #1,
                // so we make some assumptions here with no setting present.
                return data.FirstOrDefault(s => s.Name == Settings.PrimaryScheduleName) ??
                       data.FirstOrDefault();
            }
        }

        public string PrimaryScheduleId
        {
            get
            {
                var schedule = PrimarySchedule;
                return schedule != null ? schedule.Id : "";
            }
        }
        private Cache<List<PagerDutySchedule>> _schedules;

        public Cache<List<PagerDutySchedule>> AllSchedules => _schedules ?? (_schedules = new Cache<List<PagerDutySchedule>>()
        {
            CacheForSeconds = 10 * 60,
            UpdateCache = UpdateCacheItem(
                description: "Pager Duty Incidents",
                getData: GetAllSchedulesAsync,
                logExceptions: true
                )
        });

        private Cache<List<PagerDutyScheduleOverride>> _primaryScheduleOverrides;
        public Cache<List<PagerDutyScheduleOverride>> PrimaryScheduleOverrides
        {
            get
            {
                var schedule = PrimarySchedule;
                if (schedule == null) return null;
                return _primaryScheduleOverrides ??
                       (_primaryScheduleOverrides = new Cache<List<PagerDutyScheduleOverride>>()
                       {
                           CacheForSeconds = 10*60,
                           UpdateCache = UpdateCacheItem(
                               description: "Pager Duty Overrides",
                               getData: PrimarySchedule.GetOverridesAsync,
                               logExceptions: true
                               )
                       });
            }
        }

        private Task<List<PagerDutySchedule>> GetAllSchedulesAsync()
        {
            return GetFromPagerDutyAsync("schedules", getFromJson:
                response => JSON.Deserialize<PagerDutyScheduleResponse>(response.ToString(), JilOptions).Schedules);
        }
    }

    class PagerDutyScheduleResponse
    {
        [DataMember(Name="schedules")]
        public List<PagerDutySchedule> Schedules { get; set; }
    }

    public class PagerDutySchedule
    {
        [DataMember(Name="id")]
        public string Id { get; set; }
        [DataMember(Name="name")]
        public string Name { get; set; }
        [DataMember(Name="time_zone")]
        public string TimeZone { get; set; }

        public async Task<string> SetOverrideAsync(DateTime start, DateTime end, PagerDutyPerson pdPerson)
        {
            var overrideData = new
            {
                @override = new
                {
                    start,
                    end,
                    user_id = pdPerson.Id
                }
            };
            var result = await PagerDutyApi.Instance.GetFromPagerDutyAsync("schedules/" + Id + "/overrides",
                getFromJson: response => response.ToString(), httpMethod: "POST", data: overrideData);

            PagerDutyApi.Instance.OnCallUsers.Poll(true);
            PagerDutyApi.Instance.PrimaryScheduleOverrides.Poll(true);
            return result;
        }
        
        public Task<List<PagerDutyScheduleOverride>> GetOverridesAsync()
        {
            string since = DateTime.UtcNow.AddDays(-1).ToString("s"),
                    until = DateTime.UtcNow.AddDays(1).ToString("s");

            return PagerDutyApi.Instance.GetFromPagerDutyAsync("schedules/" + Id + "/overrides?since=" + since  + "&until=" + until, getFromJson:
                response => JSON.Deserialize<PagerDutyScheduleOverrideResponse>(response.ToString(), PagerDutyApi.JilOptions).Overrides);
        }
    }

    class PagerDutyScheduleOverrideResponse
    {
        [DataMember(Name = "total")]
        public int Total { get; set; }
        [DataMember(Name = "overrides")]
        public List<PagerDutyScheduleOverride> Overrides { get; set; }
    }

    public class PagerDutyScheduleOverride
    {
        [DataMember(Name = "user")]
        public PagerDutyPerson User { get; set; }
        [DataMember(Name = "start")]
        public DateTimeOffset StartTime { get; set; }
        [DataMember(Name = "end")]
        public DateTimeOffset EndTime { get; set; }
    }
}

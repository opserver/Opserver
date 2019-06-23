using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyAPI
    {
        public PagerDutySchedule PrimarySchedule
        {
            get
            {
                var data = AllSchedules.Data;
                // Many people likely have 1 schedule, or the primary is #1,
                // so we make some assumptions here with no setting present.
                return data?.FirstOrDefault(s => s.Name == Settings.PrimaryScheduleName) ??
                       data?.FirstOrDefault();
            }
        }

        public string PrimaryScheduleId => PrimarySchedule?.Id ?? "";

        private Cache<List<PagerDutySchedule>> _schedules;

        public Cache<List<PagerDutySchedule>> AllSchedules =>
            _schedules ?? (_schedules = GetPagerDutyCache(10.Minutes(),
                () => GetFromPagerDutyAsync("schedules",
                        response => JSON.Deserialize<PagerDutyScheduleResponse>(response, JilOptions).Schedules)
            ));

        private Cache<List<PagerDutyScheduleOverride>> _primaryScheduleOverrides;
        public Cache<List<PagerDutyScheduleOverride>> PrimaryScheduleOverrides
        {
            get
            {
                var schedule = PrimarySchedule;
                if (schedule == null) return null;
                return _primaryScheduleOverrides ?? (_primaryScheduleOverrides = GetPagerDutyCache(10.Minutes(), () => GetOverridesAsync(PrimarySchedule)));
            }
        }

        public async Task<string> SetOverrideAsync(PagerDutySchedule shedule, DateTime start, DateTime end, PagerDutyPerson pdPerson)
        {
            var overrideData = new
            {
                @override = new
                {
                    start,
                    end,
                    user = new
                    {
                        id = pdPerson.Id,
                        type = "user_reference"
                    }
                }
            };
            var result = await GetFromPagerDutyAsync("schedules/" + shedule.Id + "/overrides",
                getFromJson: response => response, httpMethod: "POST", data: overrideData).ConfigureAwait(false);

            await OnCallInfo.PollAsync(true).ConfigureAwait(false);
            await PrimaryScheduleOverrides.PollAsync(true).ConfigureAwait(false);
            return result;
        }

        public Task<List<PagerDutyScheduleOverride>> GetOverridesAsync(PagerDutySchedule shedule)
        {
            string since = DateTime.UtcNow.AddDays(-1).ToString("s"),
                    until = DateTime.UtcNow.AddDays(1).ToString("s");

            return GetFromPagerDutyAsync("schedules/" + shedule.Id + "/overrides?since=" + since + "&until=" + until, getFromJson:
                response => JSON.Deserialize<PagerDutyScheduleOverrideResponse>(response, JilOptions).Overrides);
        }
    }

    public class PagerDutyScheduleResponse
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
    }

    internal class PagerDutyScheduleOverrideResponse
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

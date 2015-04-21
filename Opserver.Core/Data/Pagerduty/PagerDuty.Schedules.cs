using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {

        public string PrimaryScheduleId
        {
            get { return AllSchedules.Data.Find(s => s.Name == "Primary On Call").Id; }
        }
        private Cache<List<PagerDutySchedule>> _schedules;

        public Cache<List<PagerDutySchedule>> AllSchedules
        {
            get
            {
                return _schedules ?? (_schedules = new Cache<List<PagerDutySchedule>>()
                {
                    CacheForSeconds = 10 * 60,
                    UpdateCache = UpdateCacheItem(
                        description: "Pager Duty Incidents",
                        getData: GetAllSchedules,
                        logExceptions: true
                        )
                });
            }
        }

        private List<PagerDutySchedule> GetAllSchedules()
        {
            return GetFromPagerDuty("schedules", getFromJson:
                response => JSON.Deserialize<PagerDutyScheduleResponse>(response.ToString(), Options.ISO8601).Schedules);
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
    }
}

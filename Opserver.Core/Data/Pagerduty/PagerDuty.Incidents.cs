using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {
        private Cache<List<PagerDutyIncident>> _incidents;

        public Cache<List<PagerDutyIncident>> Incidents
        {
            get
            {
                return _incidents ?? (_incidents = new Cache<List<PagerDutyIncident>>
                {
                    CacheForSeconds = 10 * 60,
                    UpdateCache = UpdateCacheItem(
                        description: "Pager Duty Incidents",
                        getData: GetIncidents,
                        logExceptions: true
                        )
                });
            }
        }

        private List<PagerDutyIncident> GetIncidents()
        {
            var url = string.Format("incidents?since={0}&until={1}&sort_by=created_on:desc",
                DateTime.UtcNow.AddDays(-Settings.DaysToCache).ToString("yyyy-MM-dd"),
                DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"));
            var i = GetFromPagerDuty(url, getFromJson:
                response =>
                {
                    var myResp =
                        JSON.Deserialize<PagerDutyIncidentResponce>(response.ToString(), Options.ISO8601)
                            .Incidents.OrderBy(ic => ic.CreationDate)
                            .ToList();
                    return myResp;
                });
            return i;
        }
    }

    public class PagerDutyIncidentResponce
    {
        [DataMember(Name = "incidents")]
        public List<PagerDutyIncident> Incidents { get; set; }
    }

    public class PagerDutyIncident
    {
        [DataMember(Name = "incident_number")]
        public int Number { get; set; }
        [DataMember(Name = "status")]
        public string Status { get; set; }
        [DataMember(Name = "created_on")]
        public DateTime? CreationDate { get; set; }
        [DataMember(Name = "html_url")]
        public string Uri { get; set; }
        [DataMember(Name = "assigned_to")]
        public List<PagerDutyPerson> AssignedTo { get; set; }
        [DataMember(Name = "last_status_change_on")]
        public DateTime? LastChangedOn { get; set; }
        [DataMember(Name = "last_status_change_by")]
        public PagerDutyPerson LastChangedBy { get; set; }
        [DataMember(Name = "resolved_by_user")]
        public PagerDutyPerson ResolvedBy { get; set; }
        [DataMember(Name = "acknowledgers")]
        public List<PagerDutyAcknowledgement> AcknowledgedBy { get; set; }
        [DataMember(Name="trigger_summary_data")]
        public Dictionary<string, string> SummaryData { get; set; }
        [DataMember(Name = "service")]
        public PagerDutyService AffectedService { get; set; }
        [DataMember(Name = "number_of_escalations")]
        public int NumberOfEscalations { get; set; }


        public MonitorStatus MonitorStatus
        {
            get
            {
                switch (Status)
                {
                    case "triggered":
                        return MonitorStatus.Critical;
                    case "acknowledged":
                        return MonitorStatus.Warning;
                    case "resolved":
                        return MonitorStatus.Good;
                    default:
                        return MonitorStatus.Unknown;
                }
            }
        }
    }

    public class PagerDutyAcknowledgement
    {
        [DataMember(Name = "at")]
        public DateTime? AckTime { get; set; }
        [DataMember(Name = "object")]
        public PagerDutyPerson AckPerson { get; set; }
    }

    public class PagerDutyService
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "html_url")]
        public string ServiceUri { get; set; }
    }

}

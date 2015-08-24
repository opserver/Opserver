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
        private Cache<List<Incident>> _incidents;
        public Cache<List<Incident>> Incidents => _incidents ?? (_incidents = new Cache<List<Incident>>
        {
            CacheForSeconds = 10 * 60,
            UpdateCache = UpdateCacheItem("Pager Duty Incidents", GetIncidents, true)
        });

        private Task<List<Incident>> GetIncidents()
        {
            string since = DateTime.UtcNow.AddDays(-Settings.DaysToCache).ToString("yyyy-MM-dd"),
                until = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
            var url = $"incidents?since={since}&until={until}&sort_by=created_on:desc";
            return GetFromPagerDutyAsync(url, getFromJson: response =>
                JSON.Deserialize<IncidentResponse>(response.ToString(), JilOptions)
                    .Incidents.OrderBy(ic => ic.CreationDate)
                    .ToList()
                );
        }
    }
    
    public class IncidentResponse
    {
        [DataMember(Name = "incidents")]
        public List<Incident> Incidents { get; set; }
    }

    public class IncidentMinimal
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "status")]
        public IncidentStatus Status { get; set; }
    }

    public class Incident : IncidentMinimal
    {
        [DataMember(Name = "incident_number")]
        public int Number { get; set; }
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
        public List<Acknowledgement> AcknowledgedBy { get; set; }
        [DataMember(Name="trigger_summary_data")]
        public Dictionary<string, string> SummaryData { get; set; }
        [DataMember(Name = "service")]
        public PagerDutyService AffectedService { get; set; }
        [DataMember(Name = "number_of_escalations")]
        public int? NumberOfEscalations { get; set; }
        
        public Task<List<LogEntry>> Logs => PagerDutyApi.Instance.GetIncidentEntriesAsync(Id);

        public MonitorStatus MonitorStatus
        {
            get
            {
                switch (Status)
                {
                    case IncidentStatus.triggered:
                        return MonitorStatus.Critical;
                    case IncidentStatus.acknowledged:
                        return MonitorStatus.Warning;
                    case IncidentStatus.resolved:
                        return MonitorStatus.Good;
                    default:
                        return MonitorStatus.Unknown;
                }
            }
        }
    }

    public class Acknowledgement
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

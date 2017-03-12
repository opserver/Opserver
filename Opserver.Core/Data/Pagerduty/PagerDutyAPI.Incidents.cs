using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jil;
using UnconstrainedMelody;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyAPI
    {
        private Cache<List<Incident>> _incidents;

        public Cache<List<Incident>> Incidents =>
            _incidents ?? (_incidents = GetPagerDutyCache(10.Minutes(), () =>
            {
                string since = DateTime.UtcNow.AddDays(-Settings.DaysToCache).ToString("yyyy-MM-dd"),
                       until = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
                var url = $"incidents?since={since}&until={until}&sort_by=created_at:desc";
                return GetFromPagerDutyAsync(url, getFromJson: response =>
                    JSON.Deserialize<IncidentResponse>(response, JilOptions)
                        .Incidents.OrderBy(ic => ic.CreationDate)
                        .ToList()
                    );
            }));
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

    public class Incident : IncidentMinimal, IMonitorStatus
    {
        [DataMember(Name = "incident_number")]
        public int Number { get; set; }
        [DataMember(Name = "created_at")]
        public DateTime? CreationDate { get; set; }
        [DataMember(Name = "urgency")]
        public string Urgency { get; set; }
        [DataMember(Name = "html_url")]
        public string Uri { get; set; }
        [DataMember(Name = "assigned_to")]
        public List<PagerDutyPerson> AssignedTo { get; set; }
        [DataMember(Name = "last_status_change_at")]
        public DateTime? LastChangedOn { get; set; }
        [DataMember(Name = "last_status_change_by")]
        public PagerDutyInfoReference LastChangedBy { get; set; }
        [DataMember(Name = "resolved_by_user")]
        public string ResolvedBy => Logs.Result.Find(r => r.LogType == "resolve_log_entry")?.Agent.Person;
        [DataMember(Name = "resolve_reason")]
        public string ResolveReason { get; set; }

        [DataMember(Name = "acknowledgers")]
        public List<Acknowledgement> AcknowledgedBy
        {
            get
            {
                var a = new List<Acknowledgement>();
                foreach (var i in Logs.Result.FindAll(l => l.LogType == "acknowledge_log_entry"))
                {
                    a.Add(new Acknowledgement()
                    {
                        AckPerson = i.Agent.Person,
                        AckTime = i.CreationTime
                    });
                }
                return a;
            }
        }

        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
        [DataMember(Name = "service")]
        public PagerDutyService AffectedService { get; set; }
        [DataMember(Name = "number_of_escalations")]
        public int? NumberOfEscalations { get; set; }

        public Task<List<LogEntry>> Logs => PagerDutyAPI.Instance.GetIncidentEntriesAsync(Id);

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

        public string MonitorStatusReason => "Status is " + Status.GetDescription();
    }

    public class Acknowledgement
    {
        public DateTime? AckTime { get; set; }
        public string AckPerson { get; set; }
    }

    public class PagerDutyService
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "summary")]
        public string Name { get; set; }
        [DataMember(Name = "html_url")]
        public string ServiceUri { get; set; }
    }
}

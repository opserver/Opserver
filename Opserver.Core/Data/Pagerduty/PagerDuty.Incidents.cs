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
        private Cache<List<PdIncident>> _incidents;

        public Cache<List<PdIncident>> Incidents
        {
            get
            {
                return _incidents ?? (_incidents = new Cache<List<PdIncident>>()
                {
                    CacheForSeconds = 60 * 60,
                    UpdateCache = UpdateCacheItem(
                        description: "Pager Duty Primary On Call",
                        getData: GetIncidents,
                        logExceptions: true
                        )
                });
            }
        }

        private List<PdIncident> GetIncidents()
        {
            var i = GetFromPagerDuty("incidents","", getFromJson:
                response =>
                {
                    var myResp =
                        JSON.Deserialize<PdIncidentResponce>(response.ToString(), Options.ISO8601)
                            .PDI.OrderBy(ic => ic.CreationDate)
                            .ToList();
                    return myResp;
                });
            return i;
        }
    }

    public class PdIncidentResponce
    {
        [DataMember(Name = "incidents")]
        public List<PdIncident> PDI { get; set; }
    }

    public class PdIncident
    {
        [DataMember(Name = "incident_number")]
        public int IncidentNumber { get; set; }
        [DataMember(Name = "status")]
        public string IncidentStatus { get; set; }
        [DataMember(Name = "created_on")]
        public DateTime? CreationDate { get; set; }
        [DataMember(Name = "html_url")]
        public string IncidentUri { get; set; }
        [DataMember(Name = "assigned_to")]
        public List<PdPerson> Owners { get; set; }
        [DataMember(Name = "last_status_change_by")]
        public PdPerson LastChangedBy { get; set; }
        [DataMember(Name = "acknowledgers")]
        public List<PdAcknowledgement> AckdBy { get; set; }
        [DataMember(Name="trigger_summary_data")]
        public Dictionary<string, string> SummaryData { get; set; }
        [DataMember(Name = "service")]
        public PdService AffectedService { get; set; }
    }

    public class PdAcknowledgement
    {
        [DataMember(Name = "at")]
        public DateTime? AckTime { get; set; }
        [DataMember(Name = "object")]
        public PdPerson AckPerson { get; set; }
    }

    public class PdService
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "html_url")]
        public string ServiceUri { get; set; }
    }

}

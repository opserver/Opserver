using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyApi
    {
        public async Task<List<Incident>> UpdateIncidentStatusAsync(string incidentId, PagerDutyPerson person, IncidentStatus newStatus)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));

            var data = new PagerDutyIncidentPut
            {
                Incidents = new List<Incident>
                {
                    new Incident {Id = incidentId, Status = newStatus}
                },
                RequesterId = person.Id
            };

            var result = await Instance.GetFromPagerDutyAsync("incidents",
                response => JSON.Deserialize<IncidentResponse>(response.ToString(), JilOptions),
                httpMethod: "PUT",
                data: data);

            Incidents.Poll(true);

            return result?.Incidents ?? new List<Incident>();
        }

        public class PagerDutyIncidentPut
        {
            [DataMember(Name = "requester_id")]
            public string RequesterId { get; set; }
            [DataMember(Name = "incidents")]
            public List<Incident> Incidents { get; set; }
            public bool Refresh { get; set; }
        }
    }
}

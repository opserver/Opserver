using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyAPI
    {
        public async Task<Incident> UpdateIncidentStatusAsync(string incidentId, PagerDutyPerson person, IncidentStatus newStatus)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            var data = new
            {
                incident = new
                {
                    type = "incident_reference",
                    status = newStatus.ToString()
                }
            };

            var headers = new Dictionary<string, string>
            {
                ["From"] = person.Email
            };
            try
            {
               var result = await Instance.GetFromPagerDutyAsync($"incidents/{incidentId}",
               response => JSON.Deserialize<PagerDutyIncidentUpdateResp>(response, JilOptions),
               httpMethod: "PUT",
               data: data,
               extraHeaders: headers).ConfigureAwait(false);
               await Incidents.PollAsync(true).ConfigureAwait(false);

               return result?.Response ?? new Incident();
            }
            catch (DeserializationException de)
            {
                Current.LogException(
                    de.AddLoggedData("Message", de.Message)
                    .AddLoggedData("Snippet After", de.SnippetAfterError)
                    );
                return null;
            }
        }

        public class PagerDutyIncidentUpdateResp
        {
            [DataMember(Name = "incident")]
            public Incident Response { get; set; }
        }
    }
}

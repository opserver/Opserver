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
        public Task<LogEntry> GetEventEntryAsync(string id)
        {
            return GetFromPagerDutyAsync($"log_entries/{id}", getFromJson:
                response => JSON.Deserialize<LogEntry>(response, JilOptions));
        }

        public Task<List<LogEntry>> GetIncidentEntriesAsync(string id)
        {
            return GetFromPagerDutyAsync($"incidents/{id}/log_entries", getFromJson:
                response => JSON.Deserialize<LogEntries>(response, JilOptions).Logs);
        }
    }

    public class LogEntries
    {
        [DataMember(Name="log_entries")]
        public List<LogEntry> Logs { get; set; } 
    }

    public class LogEntry 
    {
        [DataMember(Name="id")]
        public string Id { get; set; }
        [DataMember(Name="created_at")]
        public DateTime? CreationTime { get; set; }
        [DataMember(Name = "type")]
        public string LogType { get; set; }
        [DataMember(Name = "agent")]
        public LogAgent Agent { get; set; }
        public LogDetail Detail {
            get
            {
                var r = new LogDetail();
                if (LogType == "notify")
                {
                    r.Message = NotifyUser.FullName + " via " + NotificationAction.NotifyAddress;
                }
                else
                {
                    switch (Agent.Service)
                    {
                        case "user":
                            r.Message = Agent.Person.FullName + " via " + Channel.ChannelType;
                            break;
                        case "service":
                            r.Message = "Action Performed by: " + Channel.ChannelType;
                            break;
                        default:
                            r.Message = "I have no idea what i'm doing";
                            break;
                    }
                }
                return r;
            } 
        }
        [DataMember(Name = "channel")]
        public LogChannel Channel { get; set; }
        [DataMember(Name = "note")]
        public string Note { get; set; }
        [DataMember(Name="user")]
        public PagerDutyPerson NotifyUser { get; set; }
        [DataMember(Name = "notification")]
        public Notification NotificationAction { get; set; }
    }

    public class LogAgent
    {
        [DataMember(Name="id")]
        public string AgentId { get; set; }
        [DataMember(Name="type")]
        public string Service { get; set; }
        public PagerDutyPerson Person => PagerDutyApi.Instance.AllUsers.Data.FirstOrDefault(u => u.Id == AgentId);
    }
    public class Notification
    {
        [DataMember(Name = "type")]
        public string NotifyType { get; set; }
        [DataMember(Name = "address")]
        public string NotifyAddress { get; set; }
        [DataMember(Name = "success")]
        public string NotifyStatus { get; set; }
    }

    public class LogDetail
    {
        public string Message { get; set; }
    }

    public class LogChannel
    {
        [DataMember(Name = "type")]
        public string ChannelType { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public partial class PagerDutyAPI
    {
        public Task<List<LogEntry>> GetIncidentEntriesAsync(string id)
        {
            try
            {
                return GetFromPagerDutyAsync($"incidents/{id}/log_entries", getFromJson:
                    response => JSON.Deserialize<LogEntries>(response, JilOptions).Logs);
            }
            catch (DeserializationException de)
            {
                de.AddLoggedData("Snippet After", de.SnippetAfterError)
                  .Log();
                return Task.FromResult<List<LogEntry>>(null);
            }
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
        public string LogId { get; set; }
        [DataMember(Name ="summary")]
        public string Summary { get; set; }
        [DataMember(Name = "html_url")]
        public string Url { get; set; }
        [DataMember(Name="created_at")]
        public DateTime? CreationTime { get; set; }
        [DataMember(Name = "type")]
        public string LogType { get; set; }
        [DataMember(Name = "agent")]
        public LogAgent Agent { get; set; }
        [DataMember(Name = "note")]
        public string Note { get; set; }
        [DataMember(Name="user")]
        public PagerDutyPerson NotifyUser { get; set; }
        [DataMember(Name = "channel")]
        public NotificationChannel Channel { get; set; }
    }

    public class LogAgent
    {
        [DataMember(Name="id")]
        public string AgentId { get; set; }
        [DataMember(Name="type")]
        public string Service { get; set; }
        // Temp till we get the AllUsers fixed up
        [DataMember(Name ="summary")]
        public string Person { get; set; }
    }

    public class NotificationChannel
    {
        [DataMember(Name = "type")]
        public string ChannelType { get; set; }
        [DataMember(Name = "notification")]
        public Notification NotificationInfo { get; set; }
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

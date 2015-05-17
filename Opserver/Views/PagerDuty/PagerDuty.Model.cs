using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using StackExchange.Opserver.Data.PagerDuty;

namespace StackExchange.Opserver.Views.PagerDuty
{
    public class PagerDutyModel
    {
        public List<OnCallAssignment> Schedule { get; set; }
        public int OnCallToShow { get; set; }
        public int CachedDays { get; set; }

        public List<PagerDutyIncident> AllIncidents { get; set; }
        public bool Refresh { get; set; }
        public PagerDutyPerson CurrentPagerDutyPerson { get; set; }
    }

    public class PagerDutyIncidentModel
    {
        [DataMember(Name = "requester_id")]
        public string RequesterId { get; set; }
        [DataMember(Name="incidents")]
        public List<PagerDutyEditIncident> Incidents { get; set; }
        public bool Refresh { get; set; }
    }

    public class PagerDutyEditIncident
    {
        [DataMember(Name="id")]
        public string Id { get; set; }
        [DataMember(Name="status")]
        public string Status { get; set; }
        public bool Refresh { get; set; }

        /*
         * These arn't needed right now, but can be useful
         * in the future.
         * I also suspect sending these as null breaks pagerduty
         */
        //[DataMember(Name="escalation_level")]
        //public int? EscalationLevel { get; set; }
        //[DataMember(Name="assigned_to_user")]
        //public string AssignedUser { get; set; }
        //[DataMember(Name="escalation_policy")]
        //public string EscalationPolicy { get; set; }
    }

    public class PagerDutyScheduleOverride
    {
        [DataMember(Name="user_id")]
        public string UserID { get; set; }
        [DataMember(Name="start")]
        public DateTime? StartTime { get; set; }
        [DataMember(Name="end")]
        public DateTime? EndTime { get; set; }
    }
    

    
    
}
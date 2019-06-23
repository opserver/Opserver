namespace StackExchange.Opserver
{
    public class OpserverSettings
    {
        public PagerDutySettings PagerDuty { get; set; } = new PagerDutySettings();
        public CloudFlareSettings CloudFlare { get; set; } = new CloudFlareSettings();
        public DashboardSettings Dashboard { get; set; } = new DashboardSettings();
        public ElasticSettings Elastic { get; set; } = new ElasticSettings();
        public ExceptionsSettings Exceptions { get; set; } = new ExceptionsSettings();
        public HAProxySettings HAProxy { get; set; } = new HAProxySettings();
        public PollingSettings Polling { get; set; } = new PollingSettings();
        public RedisSettings Redis { get; set; } = new RedisSettings();
        public SQLSettings SQL { get; set; } = new SQLSettings();
        public JiraSettings Jira { get; set; } = new JiraSettings();
    }
}

namespace StackExchange.Opserver.Views.CloudFlare
{
    public class DashboardModel
    {
        public virtual Views View { get; set; }

        public enum Views
        {
            Overview,
            Railgun,
            DNS,
            Analytics
        }
    }
}
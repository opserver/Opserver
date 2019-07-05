namespace StackExchange.Opserver.Views.Cloudflare
{
    public class DashboardModel
    {
        public virtual Views View { get; set; }

        public enum Views
        {
            Overview = 0,
            DNS = 1,
            Analytics = 2
        }
    }
}
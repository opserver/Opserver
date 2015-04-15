using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardData
    {
        public static bool HasData
        {
            get { return !(Current is EmptyDataProvider); }
        }

        public static DashboardDataProvider Current { get; private set; }
    
        static DashboardData()
        {
            if (Opserver.Current.Settings.Dashboard.Enabled)
            {
                var provider = Opserver.Current.Settings.Dashboard.Provider;
                switch (provider.Type.IsNullOrEmptyReturn("").ToLower())
                {
                    case "bosun":
                        Current = new BosunDataProvider(provider);
                        break;
                        // moar providers, feed me!
                }
            }
            else
            {
                Current = new EmptyDataProvider("EmptyDashboardDataProvider");
            }

            Current.TryAddToGlobalPollers();
        }
    }
}

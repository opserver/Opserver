namespace StackExchange.Opserver.Data.Dashboard
{
    public static class DashboardMetric
    {
        public const string CPUUsed = "os.cpu";
        public const string MemoryUsed = "os.mem.used";
        public const string NetBytes = "os.net.bytes";
        public const string DiskUsed = "os.disk.fs.space_used";

        public static bool IsCounter(string metric)
        {
            switch (metric)
            {
                case CPUUsed:
                    return true;
                default:
                    return false;
            }
        }
    }

    public static class DashboardTag
    {
        public const string Interface = "iface";
        public const string Direction = "direction";
    }
}

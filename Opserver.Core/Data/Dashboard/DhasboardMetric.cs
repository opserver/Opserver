namespace StackExchange.Opserver.Data.Dashboard
{
    public static class DashboardMetric
    {
        public const string CPUUsed            = "os.cpu";
        public const string MemoryUsed         = "os.mem.used";
        public const string MemoryTotal        = "os.mem.total";
        public const string NetBytes           = "os.net.bytes";
        public const string DiskUsed           = "os.disk.fs.space_used";
        public const string HUError            = "hu.error";
        public const string HUGotExpectedCode  = "hu.http.got_expected_code";
        public const string HUGotExpectedRegex = "hu.http.got_expected_regex";
        public const string HUGotExpectedText  = "hu.http.got_expected_text";
        public const string HUSocketConnected  = "hu.socket_connected";
        public const string HUTimeTotal        = "hu.time_total";

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

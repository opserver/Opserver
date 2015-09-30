namespace StackExchange.Opserver
{
    public class PollingSettings : Settings<PollingSettings>
    {
        public override bool Enabled => Windows != null;

        public WindowsPollingSettings Windows { get; set; }
        public class WindowsPollingSettings
        {
            public WindowsPollingSettings()
            {
                // Defaults
                QueryTimeoutMs = 30*1000;
            }

            /// <summary>
            /// Maximum timeout in milliseconds before giving up on a poll
            /// </summary>
            public int QueryTimeoutMs { get; set; }

            /// <summary>
            /// User to authenticate as, if not present then impersonation will be used
            /// </summary>
            public string AuthUser { get; set; }

            /// <summary>
            /// Password for user to authenticate as, if not present then impersonation will be used
            /// </summary>
            public string AuthPassword { get; set; }
        }
    }
}

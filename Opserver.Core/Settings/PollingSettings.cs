using System;

namespace StackExchange.Opserver
{
    public class PollingSettings : Settings<PollingSettings>
    {
        public override bool Enabled => Windows != null;

        private WindowsPollingSettings _windows;
        public WindowsPollingSettings Windows
        {
            get { return _windows; }
            set
            {
                _windows = value;
                WindowsChanged(this, value);
            }
        }
        public event EventHandler<WindowsPollingSettings> WindowsChanged = delegate { };

        public class WindowsPollingSettings
        {
            public WindowsPollingSettings()
            {
                // Defaults
                QueryTimeout = 30*1000;
            }

            /// <summary>
            /// Maximum timeout in milliseconds before giving up on a poll
            /// </summary>
            public int QueryTimeout { get; set; }

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

using System;

namespace StackExchange.Opserver.Data
{
    public class FetchInfo : IMonitorStatus
    {
        public DateTime DateTime { get; set; }
        public FetchStatus Status { get; set; }
        public Exception Exception { get; set; }
        public string ExceptionMessage { get; set; }

        public static FetchInfo Success() =>
            new FetchInfo
            {
                DateTime = DateTime.UtcNow,
                Status = FetchStatus.Success
            };

        public static FetchInfo Fail(string exceptionMessage, Exception exception) =>
            new FetchInfo
            {
                DateTime = DateTime.UtcNow,
                Status = FetchStatus.Fail,
                ExceptionMessage = exceptionMessage,
                Exception = exception
            };

        public MonitorStatus MonitorStatus
        {
            get
            {
                switch(Status)
                {
                    case FetchStatus.Success:
                        return MonitorStatus.Good;
                    case FetchStatus.Fail:
                        return MonitorStatus.Critical;
                    default:
                        return MonitorStatus.Unknown;
                }
            }
        }
        public string MonitorStatusReason
        {
            get
            {
                switch (Status)
                {
                    case FetchStatus.Fail:
                        return "Fetch Failed";
                    default:
                        return null;
                }
            }
        }
    }

    public enum FetchStatus
    {
        Success = 0,
        Fail = 1
        // Ummmmmmm
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SnmpSharpNet;

namespace StackExchange.Opserver.Monitoring
{
    public class SNMPUtils
    {
        public static Dictionary<Oid, AsnType> Walk(string ip, string oid, int port = 161, int timeout = 2000, int retries = 1)
        {
            var snmp = new SimpleSnmp(ip, port, "secure", timeout, retries);
            return snmp.Walk(SnmpVersion.Ver2, oid);
        }

        public static List<Process> GetRunningProcesses(string ip)
        {
            return new List<Process>();
        }

        public static List<int> GetCPUUtilization(string ip)
        {
            var info = Walk(ip, OIDs.Hardware.hrProcessorLoad);
            return info.Select(i => ((Integer32)i.Value).Value).ToList();
        }
    }

    public class Process
    {
        public int ProcessId { get; private set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Parameters { get; set; }
        public int RunType { get; set; }
        public int RunStatus { get; set; }

    }
    

    public static class OIDs
    {
        public static class SystemInfo
        {
            public const string hrSystem                      = "1.3.6.1.2.1.25.1";

            [Description("The amount of time since this host was last initialized. Note that this is different from sysUpTime in the SNMPv2-MIB [RFC1907] because sysUpTime is the uptime of the network management portion of the system.")]
            public const string hrSystemUptime                = "1.3.6.1.2.1.25.1.1";
            [Description("The hosts notion of the local date and time of day.")]
            public const string hrSystemDate                  = "1.3.6.1.2.1.25.1.2";
            [Description("The index of the hrDeviceEntry for the device from which this host is configured to load its initial operating system configuration (i.e., which operating system code and/or boot parameters). Note that writing to this object just changes the configuration that will be used the next time the operating system is loaded and does not actually cause the reload to occur.")]
            public const string hrSystemInitialLoadDevice     = "1.3.6.1.2.1.25.1.3";
            [Description("This object contains the parameters (e.g. a pathname and parameter) supplied to the load device when requesting the initial operating system configuration from that device. Note that writing to this object just changes the configuration that will be used the next time the operating system is loaded and does not actually cause the reload to occur.")]
            public const string hrSystemInitialLoadParameters = "1.3.6.1.2.1.25.1.4";
            [Description("The number of user sessions for which this host is storing state information. A session is a collection of processes requiring a single act of user authentication and possibly subject to collective job control.")]
            public const string hrSystemNumUsers              = "1.3.6.1.2.1.25.1.5";
            [Description("The number of process contexts currently loaded or running on this system.")]
            public const string hrSystemProcesses             = "1.3.6.1.2.1.25.1.6";
            [Description("The maximum number of process contexts this system can support. If there is no fixed maximum, the value should be zero. On systems that have a fixed maximum, this object can help diagnose failures that occur when this maximum is reached.")]
            public const string hrSystemMaxProcesses          = "1.3.6.1.2.1.25.1.7";
        }

        public static class Hardware
        {
            public const string hrDevice         = "1.3.6.1.2.1.25.3";

            public const string hrProcessorTable = "1.3.6.1.2.1.25.3.3";
            public const string hrProcessorEntry = "1.3.6.1.2.1.25.3.3.1";

            [Description("The product ID of the firmware associated with the processor.")]
            public const string hrProcessorFrwID = "1.3.6.1.2.1.25.3.3.1.1";
            [Description("The average, over the last minute, of the percentage of time that this processor was not idle. Implementations may approximate this one minute smoothing period if necessary.")]
            public const string hrProcessorLoad  = "1.3.6.1.2.1.25.3.3.1.2";
        }

        public static class Processes
        {
            public const string hrSWRunTable       = "1.3.6.1.2.1.25.4.2";
            public const string hrSWRunEntry       = "1.3.6.1.2.1.25.4.2.1";

            [Description("A unique value for each piece of software running on the host. Wherever possible, this should be the systems native, unique identification number.")]
            public const string hrSWRunIndex       = "1.3.6.1.2.1.25.4.2.1.1";
            [Description("A textual description of this running piece of software, including the manufacturer, revision, and the name by which it is commonly known. If this software was installed locally, this should be the same string as used in the corresponding hrSWInstalledName.")]
            public const string hrSWRunName        = "1.3.6.1.2.1.25.4.2.1.2";
            [Description("The product ID of this running piece of software.")]
            public const string hrSWRunID          = "1.3.6.1.2.1.25.4.2.1.3";
            [Description("A description of the location on long-term storage (e.g. a disk drive) from which this software was loaded.")]
            public const string hrSWRunPath        = "1.3.6.1.2.1.25.4.2.1.4";
            [Description("A description of the parameters supplied to this software when it was initially loaded.")]
            public const string hrSWRunParameters  = "1.3.6.1.2.1.25.4.2.1.5";
            [Description("The type of this software.")]
            public const string hrSWRunType        = "1.3.6.1.2.1.25.4.2.1.6";
            [Description("The status of this running piece of software. Setting this value to invalid(4) shall cause this software to stop running and to be unloaded. Sets to other values are not valid.")]
            public const string hrSWRunStatus      = "1.3.6.1.2.1.25.4.2.1.7";
        }

        public static class Performance
        {
            public const string hrSWRunTable     = "1.3.6.1.2.1.25.5.1";
            public const string hrSWRunPerfEntry = "1.3.6.1.2.1.25.5.1.1";

            [Description("The number of centi-seconds of the total systems CPU resources consumed by this process. Note that on a multi-processor system, this value may increment by more than one centi-second in one centi-second of real (wall clock) time.")]
            public const string hrSWRunPerfCPU   = "1.3.6.1.2.1.25.5.1.1.1";
            [Description("The total amount of real system memory allocated to this process.")]
            public const string hrSWRunPerfMem   = "1.3.6.1.2.1.25.5.1.1.2";
        }
    }
}
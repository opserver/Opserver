using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Opserver.Helpers;

namespace Opserver.Data.Dashboard.Providers
{
    internal partial class WmiDataProvider
    {
        private partial class WmiNode
        {
            private static readonly string _machineDomainName;

            static WmiNode()
            {
                try
                {
                    _machineDomainName = Domain.GetComputerDomain().Name;
                }
                catch (ActiveDirectoryObjectNotFoundException) { }
                catch (Exception e)
                {
                    e.Log();
                }
            }

            private Wmi.WmiQuery Query(string query, string wmiNamespace = Wmi.DefaultWmiNamespace) =>
                new Wmi.WmiQuery(Config, Endpoint, query, wmiNamespace);

            private async Task<bool> ClassExists(string className, string wmiNamespace = Wmi.DefaultWmiNamespace)
            {
                // it's much faster trying to query something potentially non existent and catching an exception than to query the "meta_class" table.
                var query = $"SELECT * FROM {className}";

                try
                {
                    using var q = Query(query, wmiNamespace);
                    return (await q.GetFirstResultAsync()) != null;
                }
                catch
                {
                    return false;
                }
            }

            public async Task<Node> PollNodeInfoAsync()
            {
                try
                {
                    var tasks = new[] { UpdateNodeDataAsync(), GetAllInterfacesAsync(), GetAllVolumesAsync(), GetServicesAsync() };
                    await Task.WhenAll(tasks);
                    AfterInitialize();
                    ClearSummaries();

                    // first run, do a follow-up poll for all stats on the first pass
                    if (!_nodeInfoAvailable)
                    {
                        _nodeInfoAvailable = true;
                        await PollStats();
                    }
                }
                // We can get both cases. See comment from Nick Craver at https://github.com/opserver/Opserver/pull/330
                catch (COMException e)
                {
                    e.Log();
                    Status = NodeStatus.Unreachable;
                }
                catch (Exception e) when (e.InnerException is COMException)
                {
                    e.Log();
                    Status = NodeStatus.Unreachable;
                }
                return this;
            }

            public async Task<Node> PollStats()
            {
                if (!_nodeInfoAvailable)
                {
                    return this;
                }

                try
                {
                    var tasks = new[] { PollCpuUtilizationAsync(), PollMemoryUtilizationAsync(),
                        PollNetworkUtilizationAsync(), PollVolumePerformanceUtilizationAsync(),
                        PollProcessUtilizationAsync() };
                    await Task.WhenAll(tasks);
                    ClearSummaries();
                }
                // We can get both cases. See comment from Nick Craver at https://github.com/opserver/Opserver/pull/330
                catch (COMException e)
                {
                    e.Log();
                    Status = NodeStatus.Unreachable;
                }
                catch (Exception e) when (e.InnerException is COMException)
                {
                    e.Log();
                    Status = NodeStatus.Unreachable;
                }
                return this;
            }

            private async Task UpdateNodeDataAsync()
            {
                const string machineQuery = @"SELECT
                DNSHostName,
                Domain,
                Manufacturer,
                Model,
                NumberOfLogicalProcessors
                FROM Win32_ComputerSystem";
                using (var q = Query(machineQuery))
                {
                    var data = await q.GetFirstResultAsync();
                    if (data != null)
                    {
                        Model = data.Model;
                        Manufacturer = data.Manufacturer;
                        // Only use domain if we're on one - not for things like workgroups
                        Name = _machineDomainName.HasValue() && data.Domain != _machineDomainName
                                   ? $"{data.DNSHostName}.{data.Domain}"
                                   : data.DNSHostName;
                        NumberOfLogicalProcessors = data.NumberOfLogicalProcessors;
                    }
                }

                const string query = @"SELECT
                Caption,
                LastBootUpTime,
                Version,
                FreePhysicalMemory,
                TotalVisibleMemorySize,
                Version
                FROM Win32_OperatingSystem";

                using (var q = Query(query))
                {
                    var data = await q.GetFirstResultAsync();
                    if (data != null)
                    {
                        LastBoot = ManagementDateTimeConverter.ToDateTime(data.LastBootUpTime);
                        TotalMemory = data.TotalVisibleMemorySize * 1024;
                        MemoryUsed = TotalMemory - (data.FreePhysicalMemory * 1024);
                        KernelVersion = Version.Parse(data.Version);
                        MachineType = data.Caption.ToString() + " " + data.Version.ToString();
                    }
                }

                const string servicetagquery = @"SELECT
                    SerialNumber
                    FROM Win32_BIOS";

                using (var q = Query(servicetagquery))
                {
                    var data = await q.GetFirstResultAsync();
                    if (data != null)
                    {
                        ServiceTag = data.SerialNumber;
                    }
                }

                LastSync = DateTime.UtcNow;
                Status = NodeStatus.Active;

                IsVMHost = await GetIsVMHost();

                _canQueryAdapterUtilization = await GetCanQueryAdapterUtilization();
                _canQueryTeamingInformation = await ClassExists("MSFT_NetLbfoTeamMember", @"root\standardcimv2");
            }

            private async Task GetAllInterfacesAsync()
            {
                const string query = @"
SELECT Name,
       PNPDeviceID,
       DeviceID,
       NetConnectionID,
       Description,
       MACAddress,
       Speed,
       InterfaceIndex
  FROM Win32_NetworkAdapter
 WHERE NetConnectionStatus = 2"; //connected adapters.
                //'AND PhysicalAdapter = True' causes exceptions with old windows versions.

                var indexMap = new Dictionary<uint, Interface>();
                using (var q = Query(query))
                {
                    foreach (var data in await q.GetDynamicResultAsync())
                    {
                        string id = $"{data.DeviceID}";
                        var i = Interfaces.Find(x => x.Id == id) ?? new Interface();
                        indexMap[data.InterfaceIndex] = i;

                        i.Id = id;
                        i.Alias = "!alias";
                        i.Caption = data.NetConnectionID;
                        i.FullName = data.Description;
                        i.NodeId = Id;
                        i.LastSync = DateTime.UtcNow;
                        i.Name = await GetRealAdapterName(data.PNPDeviceID);
                        i.PhysicalAddress = data.MACAddress;
                        i.Speed = data.Speed;
                        i.Status = NodeStatus.Active;
                        i.TypeDescription = "";
                        i.IPs = new List<IPNet>();
                        i.TeamMembers = new List<string>();

                        if (i.Node == null)
                        {
                            i.Node = this;
                            Interfaces.Add(i);
                        }
                    }
                }

                if (_canQueryTeamingInformation)
                {
                    const string teamsQuery = "SELECT InstanceID, Name FROM MSFT_NetLbfoTeam";
                    var teamNamesToInterfaces = new Dictionary<string, Interface>();

                    using (var q = Query(teamsQuery, @"root\standardcimv2"))
                    {
                        foreach (var data in await q.GetDynamicResultAsync())
                        {
                            var teamInterface = Interfaces.Find(x => x.Caption == data.Name);

                            if (teamInterface == null)
                            {
                                continue;
                            }

                            teamNamesToInterfaces.Add(data.Name, teamInterface);
                        }
                    }

                    const string teamMembersQuery = "SELECT InstanceID, Name, Team FROM MSFT_NetLbfoTeamMember";
                    using (var q = Query(teamMembersQuery, @"root\standardcimv2"))
                    {
                        foreach (var data in await q.GetDynamicResultAsync())
                        {
                            string teamName = data.Team;

                            if (teamNamesToInterfaces.TryGetValue(teamName, out var teamInterface))
                            {
                                var adapterName = data.Name;
                                var memberInterface = Interfaces.Find(x => x.Name == adapterName);

                                if (memberInterface == null)
                                {
                                    continue;
                                }

                                teamInterface.TeamMembers.Add(memberInterface.Id);
                            }
                        }
                    }
                }

                const string ipQuery = @"
SELECT InterfaceIndex, IPAddress, IPSubnet, DHCPEnabled
  FROM WIn32_NetworkAdapterConfiguration
 WHERE IPEnabled = 'True'";

                using (var q = Query(ipQuery))
                {
                    foreach (var data in await q.GetDynamicResultAsync())
                    {
                        uint index = data.InterfaceIndex;
                        if (indexMap.TryGetValue(index, out var i))
                        {
                            i.DHCPEnabled = data.DHCPEnabled;
                            var ips = data.IPAddress as string[];
                            var subnets = data.IPSubnet as string[];

                            if (ips == null
                                || subnets == null)
                            {
                                continue;
                            }

                            for (var j = 0; j < (ips?.Length).GetValueOrDefault(0); j++)
                            {
                                if (byte.TryParse(subnets[j], out var cidr) && IPNet.TryParse(ips[j], cidr, out var net))
                                {
                                    i.IPs.Add(net);
                                }
                                else if (IPNet.TryParse(ips[j], subnets[j], out net))
                                {
                                    i.IPs.Add(net);
                                }
                            }
                        }
                    }
                }
            }

            private async Task GetAllVolumesAsync()
            {
                const string query = @"
SELECT Caption,
       DeviceID,
       Description,
       FreeSpace,
       Name,
       Size,
       VolumeSerialNumber
  FROM Win32_LogicalDisk
 WHERE DriveType = 3"; //fixed disks

                using var q = Query(query);
                foreach (var disk in await q.GetDynamicResultAsync())
                {
                    var id = $"{disk.DeviceID}";
                    var v = Volumes.Find(x => x.Id == id) ?? new Volume();

                    v.Id = $"{disk.DeviceID}";
                    v.Available = disk.FreeSpace;
                    v.Caption = disk.VolumeSerialNumber;
                    v.Description = disk.Name + " - " + disk.Description;
                    v.Name = disk.Name;
                    v.NodeId = Id;
                    v.Size = disk.Size;
                    v.Type = "Fixed Disk";
                    v.Status = NodeStatus.Active;
                    v.Used = v.Size - v.Available;
                    if (v.Size > 0)
                    {
                        v.PercentUsed = 100 * v.Used / v.Size;
                    }
                    if (v.Node == null)
                    {
                        v.Node = this;
                        Volumes.Add(v);
                    }
                }
            }

            private async Task GetServicesAsync()
            {
                const string query = @"
SELECT Caption,
       Description,
       DisplayName,
       Name,
       Started,
       StartMode,
       StartName,
       State
  FROM Win32_Service"; // windows services

                using var q = Query(query);
                foreach (var service in await q.GetDynamicResultAsync())
                {
                    if (ServicesPatternRegEx?.IsMatch(service.Name) ?? true)
                    {
                        var id = service.Name;
                        var s = Services.Find(x => x.Id == id) ?? new NodeService();

                        s.Id = id;
                        s.Caption = service.Caption;
                        s.DisplayName = service.DisplayName;
                        s.Description = service.Description;
                        s.Name = service.Name;
                        s.State = service.State;
                        s.LastSync = DateTime.UtcNow;
                        s.Status = service.State switch
                        {
                            "Running" => NodeStatus.Active,
                            "Stopped" => NodeStatus.Down,
                            _ => NodeStatus.Unknown,
                        };
                        s.Running = service.Started;
                        s.StartMode = service.StartMode;
                        s.StartName = service.StartName;

                        if (s.Node == null)
                        {
                            s.Node = this;
                            Services.Add(s);
                        }
                    }
                }
            }

            private async Task PollCpuUtilizationAsync()
            {
                var query = IsVMHost
                    ? "SELECT Name, Timestamp_Sys100NS, PercentTotalRunTime FROM Win32_PerfRawData_HvStats_HyperVHypervisorLogicalProcessor WHERE Name = '_Total'"
                    : "SELECT Name, Timestamp_Sys100NS, PercentProcessorTime FROM Win32_PerfRawData_PerfOS_Processor WHERE Name = '_Total'";

                var property = IsVMHost
                    ? "PercentTotalRunTime"
                    : "PercentProcessorTime";

                using var q = Query(query);
                var data = await q.GetFirstResultAsync();
                if (data == null)
                    return;

                var perfData = new PerfRawData(this, data);

                if (IsVMHost)
                {
                    CPULoad = (short)(perfData.GetCalculatedValue(property, 100D) / NumberOfLogicalProcessors);
                }
                else
                {
                    CPULoad = (short)Math.Round((1 - perfData.GetCalculatedValue(property)) * 100);
                }

                var cpuUtilization = new CPUUtilization
                {
                    DateEpoch = DateTime.UtcNow.ToEpochTime(),
                    AvgLoad = CPULoad
                };
                UpdateHistoryStorage(CPUHistory, cpuUtilization);

                // KHK 처음에 이상하게 100으로 팍 튄다
                if (CPUHistory.Count == 1 && CPULoad == 100)
                {
                    CPUHistory.RemoveAt(0);
                    CPULoad = 0;
                    return;
                }
            }

            private async Task PollProcessUtilizationAsync()
            {
                foreach(var name in Config.processMonitor)
                {
                    var query = $"SELECT * FROM Win32_PerfFormattedData_PerfProc_Process WHERE Name = '{name}'";

                    var property = "PercentProcessorTime";

                    using var q = Query(query);
                    var data = await q.GetFirstResultAsync();

                    var perfData = new PerfRawData(this, data);
                    // https://medium.com/oldbeedev/c-how-to-monitor-cpu-memory-disk-usage-in-windows-a06fc2f05ad5
                    short? CPUProcessLoad = (short)(perfData.GetCalculatedValue(property) / NumberOfLogicalProcessors);

                    var cpuUtilization = new CPUUtilization
                    {
                        DateEpoch = DateTime.UtcNow.ToEpochTime(),
                        AvgLoad = CPUProcessLoad
                    };

                    var processData = ProcessCPUHistory.GetOrAdd(name, _ => new List<CPUUtilization>());
                    UpdateHistoryStorage(processData, cpuUtilization);

                    // KHK
                    if (ProcessCPUHistory[name].Count == 1 && CPUProcessLoad == 100)
                    {
                        ProcessCPUHistory[name].RemoveAt(0);
                        continue;
                    }

                    var obj = (ManagementObject)data; 
                    var identifier = obj["IDProcess"];
                    var process = System.Diagnostics.Process.GetProcessById(Convert.ToInt32(identifier));

                    var cpuProcessMemoryUtilization = new MemoryUtilization
                    {
                        DateEpoch = DateTime.UtcNow.ToEpochTime(),
                        AvgMemoryUsed = Convert.ToSingle(process.PrivateMemorySize64)
                    };

                    var processMemoryData = ProcessMemoryHistory.GetOrAdd(name, _ => new List<MemoryUtilization>());
                    UpdateHistoryStorage(processMemoryData, cpuProcessMemoryUtilization);
                }
               
            }

            private async Task PollMemoryUtilizationAsync()
            {
                const string query = "SELECT AvailableKBytes FROM Win32_PerfRawData_PerfOS_Memory";

                using var q = Query(query);
                var data = await q.GetFirstResultAsync();
                if (data == null)
                    return;

                var available = data.AvailableKBytes * 1024;
                MemoryUsed = TotalMemory - available;
                var utilization = new MemoryUtilization
                {
                    DateEpoch = DateTime.UtcNow.ToEpochTime(),
                    AvgMemoryUsed = MemoryUsed
                };
                UpdateHistoryStorage(MemoryHistory, utilization);
            }

            private static readonly ConcurrentDictionary<string, string> CounterLookup = new ConcurrentDictionary<string, string>();

            private static string GetCounterName(string original)
            {
                return CounterLookup.GetOrAdd(original,
                    k => StringBuilderCache.Get()
                        .Append(k)
                        .Replace("\\", "_")
                        .Replace("/", "_")
                        .Replace("(", "[")
                        .Replace(")", "]")
                        .Replace("#", "_")
                        .ToStringRecycle());
            }

            private async Task PollNetworkUtilizationAsync()
            {
                var utilizationTable = _canQueryAdapterUtilization
                                           ? "Win32_PerfRawData_Tcpip_NetworkAdapter"
                                           : "Win32_PerfRawData_Tcpip_NetworkInterface";

                var query = $@"
                    SELECT Name,
                           Timestamp_Sys100NS,
                           BytesReceivedPersec,
                           BytesSentPersec,
                           PacketsReceivedPersec,
                           PacketsSentPersec
                      FROM {utilizationTable}";

                var queryTime = DateTime.UtcNow.ToEpochTime();
                var combinedUtil = new Interface.InterfaceUtilization
                {
                    DateEpoch = queryTime,
                    InAvgBitsPerSecond = 0,
                    OutAvgBitsPerSecond = 0
                };

                using (var q = Query(query))
                {
                    foreach (var data in await q.GetDynamicResultAsync())
                    {
                        var perfData = new PerfRawData(this, data);
                        var name = perfData.Identifier;
                        var iface = Interfaces.Find(i => name == GetCounterName(i.Name));
                        if (iface == null) continue;

                        iface.InBitsPerSecond = (float)perfData.GetCalculatedValue("BytesReceivedPersec", 10000000) * 8;
                        iface.OutBitsPerSecond = (float)perfData.GetCalculatedValue("BytesSentPersec", 10000000) * 8;
                        iface.InPacketsPerSecond = (float)perfData.GetCalculatedValue("PacketsReceivedPersec", 10000000);
                        iface.OutPacketsPerSecond = (float)perfData.GetCalculatedValue("PacketsSentPersec", 10000000);

                        var util = new Interface.InterfaceUtilization
                        {
                            DateEpoch = queryTime,
                            InAvgBitsPerSecond = iface.InBitsPerSecond,
                            OutAvgBitsPerSecond = iface.OutBitsPerSecond
                        };

                        var netData = NetHistory.GetOrAdd(iface.Name, _ => new List<Interface.InterfaceUtilization>(1024));
                        UpdateHistoryStorage(netData, util);

                        if (PrimaryInterfaces.Contains(iface))
                        {
                            combinedUtil.InAvgBitsPerSecond += util.InAvgBitsPerSecond;
                            combinedUtil.OutAvgBitsPerSecond += util.OutAvgBitsPerSecond;
                        }
                    }
                }

                UpdateHistoryStorage(CombinedNetHistory, combinedUtil);
            }

            private async Task PollVolumePerformanceUtilizationAsync()
            {
                const string query = @"
                    SELECT Name,
                           Timestamp_Sys100NS,
                           DiskReadBytesPersec,
                           DiskWriteBytesPersec
                      FROM Win32_PerfRawData_PerfDisk_LogicalDisk";

                var queryTime = DateTime.UtcNow.ToEpochTime();
                var combinedUtil = new Volume.VolumePerformanceUtilization
                {
                    DateEpoch = queryTime,
                    ReadAvgBytesPerSecond = 0,
                    WriteAvgBytesPerSecond = 0
                };

                using (var q = Query(query))
                {
                    foreach (var data in await q.GetDynamicResultAsync())
                    {
                        var perfData = new PerfRawData(this, data);

                        var name = perfData.Identifier;
                        var iface = Volumes.Find(i => name == GetCounterName(i.Name));
                        if (iface == null) continue;

                        iface.ReadBytesPerSecond = (float)perfData.GetCalculatedValue("DiskReadBytesPersec", 10000000);
                        iface.WriteBytesPerSecond = (float)perfData.GetCalculatedValue("DiskWriteBytesPersec", 10000000);

                        var util = new Volume.VolumePerformanceUtilization
                        {
                            DateEpoch = queryTime,
                            ReadAvgBytesPerSecond = iface.ReadBytesPerSecond,
                            WriteAvgBytesPerSecond = iface.WriteBytesPerSecond
                        };
                        
                        var netData = VolumePerformanceHistory.GetOrAdd(iface.Name, _ => new List<Volume.VolumePerformanceUtilization>(1024));
                        UpdateHistoryStorage(netData, util);

                        //if (PrimaryInterfaces.Contains(iface))
                        {
                            combinedUtil.ReadAvgBytesPerSecond += util.ReadAvgBytesPerSecond;
                            combinedUtil.WriteAvgBytesPerSecond += util.WriteAvgBytesPerSecond;
                        }
                    }
                }

                UpdateHistoryStorage(CombinedVolumePerformanceHistory, combinedUtil);
            }

            public async Task<ServiceActionResult> UpdateServiceAsync(string serviceName, NodeService.Action action)
            {
                string query = $"SELECT Name FROM Win32_Service WHERE Name = '{serviceName}'";

                uint returnCode = 0;

                using (var q = Query(query))
                {
                    foreach (var service in await q.GetDynamicResultAsync())
                    {
                        returnCode = action switch
                        {
                            NodeService.Action.Stop => service.StopService(),
                            NodeService.Action.Start => service.StartService(),
                            _ => throw new ArgumentOutOfRangeException(nameof(action)),
                        };
                    }
                }

                return new ServiceActionResult(returnCode == 0, Win32ServiceReturnCodes[(int)returnCode]);
            }

            private Task<bool> GetIsVMHost() => ClassExists("Win32_PerfRawData_HvStats_HyperVHypervisorLogicalProcessor");

            private async Task<string> GetRealAdapterName(string pnpDeviceId)
            {
                using var query = Query($"SELECT Name FROM Win32_PnPEntity WHERE DeviceId = '{pnpDeviceId.Replace("\\", "\\\\")}'");
                var data = await query.GetFirstResultAsync();
                return data?.Name;
            }

            private async Task<bool> GetCanQueryAdapterUtilization()
            {
                // it's much faster trying to query something potentially non existent and catching an exception than to query the "meta_class" table.
                const string query = "SELECT name FROM Win32_PerfRawData_Tcpip_NetworkAdapter";

                try
                {
                    using var q = Query(query);
                    await q.GetFirstResultAsync();
                }
                catch
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Possible return codes from service actions
            /// https://msdn.microsoft.com/en-us/library/aa393660(v=vs.85).aspx
            /// </summary>
            private static readonly Dictionary<int, string> Win32ServiceReturnCodes = new Dictionary<int, string> {
                [0] = "The request was accepted.",
                [1] = "The request is not supported.",
                [2] = "The user did not have the necessary access.",
                [3] = "The service cannot be stopped because other services that are running are dependent on it.",
                [4] = "The requested control code is not valid, or it is unacceptable to the service.",
                [5] = "The requested control code cannot be sent to the service because the state of the service (Win32_BaseService.State property) is equal to 0, 1, or 2.",
                [6] = "The service has not been started.",
                [7] = "The service did not respond to the start request in a timely fashion.",
                [8] = "Unknown failure when starting the service.",
                [9] = "The directory path to the service executable file was not found.",
                [10] = "The service is already running.",
                [11] = "The database to add a new service is locked.",
                [12] = "A dependency this service relies on has been removed from the system.",
                [13] = "The service failed to find the service needed from a dependent service.",
                [14] = "The service has been disabled from the system.",
                [15] = "The service does not have the correct authentication to run on the system.",
                [16] = "This service is being removed from the system.",
                [17] = "The service has no execution thread.",
                [18] = "The service has circular dependencies when it starts.",
                [19] = "A service is running under the same name.",
                [20] = "The service name has invalid characters.",
                [21] = "Invalid parameters have been passed to the service.",
                [22] = "The account under which this service runs is either invalid or lacks the permissions to run the service.",
                [23] = "The service exists in the database of services available from the system.",
                [24] = "The service is currently paused in the system."
            };
        }
    }
}

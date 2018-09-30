using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using StackExchange.Opserver.Monitoring;

namespace StackExchange.Opserver.Data.Dashboard.Providers
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
                    Current.LogException(e);
                }
            }

            public async Task<Node> PollNodeInfoAsync()
            {
                try
                {
                    var tasks = new[] { UpdateNodeDataAsync(), GetAllInterfacesAsync(), GetAllVolumesAsync(), GetServicesAsync() };
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    SetReferences();
                    ClearSummaries();

                    // first run, do a follow-up poll for all stats on the first pass
                    if (!_nodeInfoAvailable)
                    {
                        _nodeInfoAvailable = true;
                        await PollStats().ConfigureAwait(false);
                    }
                }
                // We can get both cases. See comment from Nick Craver at https://github.com/opserver/Opserver/pull/330
                catch (COMException e)
                {
                    Current.LogException(e);
                    Status = NodeStatus.Unreachable;
                }
                catch (Exception e) when (e.InnerException is COMException)
                {
                    Current.LogException(e);
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
                    var tasks = new[] { PollCpuUtilizationAsync(), PollMemoryUtilizationAsync(), PollNetworkUtilizationAsync(), PollVolumePerformanceUtilizationAsync() };
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    ClearSummaries();
                }
                // We can get both cases. See comment from Nick Craver at https://github.com/opserver/Opserver/pull/330
                catch (COMException e)
                {
                    Current.LogException(e);
                    Status = NodeStatus.Unreachable;
                }
                catch (Exception e) when (e.InnerException is COMException)
                {
                    Current.LogException(e);
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
                using (var q = Wmi.Query(Endpoint, machineQuery))
                {
                    var data = await q.GetFirstResultAsync().ConfigureAwait(false);
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

                using (var q = Wmi.Query(Endpoint, query))
                {
                    var data = await q.GetFirstResultAsync().ConfigureAwait(false);
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

                using (var q = Wmi.Query(Endpoint, servicetagquery))
                {
                    var data = await q.GetFirstResultAsync().ConfigureAwait(false);
                    if (data != null)
                    {
                        ServiceTag = data.SerialNumber;
                    }
                }

                LastSync = DateTime.UtcNow;
                Status = NodeStatus.Active;

                IsVMHost = await GetIsVMHost().ConfigureAwait(false);

                _canQueryAdapterUtilization = await GetCanQueryAdapterUtilization().ConfigureAwait(false);
                _canQueryTeamingInformation = await Wmi.ClassExists(Endpoint, "MSFT_NetLbfoTeamMember", @"root\standardcimv2").ConfigureAwait(false);
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
                using (var q = Wmi.Query(Endpoint, query))
                {
                    foreach (var data in await q.GetDynamicResultAsync().ConfigureAwait(false))
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
                        i.Name = await GetRealAdapterName(data.PNPDeviceID).ConfigureAwait(false);
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

                    using (var q = Wmi.Query(Endpoint, teamsQuery, @"root\standardcimv2"))
                    {
                        foreach (var data in await q.GetDynamicResultAsync().ConfigureAwait(false))
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
                    using (var q = Wmi.Query(Endpoint, teamMembersQuery, @"root\standardcimv2"))
                    {
                        foreach (var data in await q.GetDynamicResultAsync().ConfigureAwait(false))
                        {
                            var teamName = data.Team;

                            if (teamNamesToInterfaces.TryGetValue(teamName, out Interface teamInterface))
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

                using (var q = Wmi.Query(Endpoint, ipQuery))
                {
                    foreach (var data in await q.GetDynamicResultAsync().ConfigureAwait(false))
                    {
                        if (indexMap.TryGetValue(data.InterfaceIndex, out Interface i))
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
                                if (int.TryParse(subnets[j], out int cidr) && IPNet.TryParse(ips[j], cidr, out IPNet net))
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

                using (var q = Wmi.Query(Endpoint, query))
                {
                    foreach (var disk in await q.GetDynamicResultAsync().ConfigureAwait(false))
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

                using (var q = Wmi.Query(Endpoint, query))
                {
                    foreach (var service in await q.GetDynamicResultAsync().ConfigureAwait(false))
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
                            switch (service.State)
                            {
                                case "Running":
                                    s.Status = NodeStatus.Active;
                                    break;
                                case "Stopped":
                                    s.Status = NodeStatus.Down;
                                    break;
                                default:
                                    s.Status = NodeStatus.Unknown;
                                    break;
                            }
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
            }

            private async Task PollCpuUtilizationAsync()
            {
                var query = IsVMHost
                    ? "SELECT Name, Timestamp_Sys100NS, PercentTotalRunTime FROM Win32_PerfRawData_HvStats_HyperVHypervisorLogicalProcessor WHERE Name = '_Total'"
                    : "SELECT Name, Timestamp_Sys100NS, PercentProcessorTime FROM Win32_PerfRawData_PerfOS_Processor WHERE Name = '_Total'";

                var property = IsVMHost
                    ? "PercentTotalRunTime"
                    : "PercentProcessorTime";

                using (var q = Wmi.Query(Endpoint, query))
                {
                    var data = await q.GetFirstResultAsync().ConfigureAwait(false);
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
                }
            }

            private async Task PollMemoryUtilizationAsync()
            {
                const string query = "SELECT AvailableKBytes FROM Win32_PerfRawData_PerfOS_Memory";

                using (var q = Wmi.Query(Endpoint, query))
                {
                    var data = await q.GetFirstResultAsync().ConfigureAwait(false);
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
                    InAvgBps = 0,
                    OutAvgBps = 0
                };

                using (var q = Wmi.Query(Endpoint, query))
                {
                    foreach (var data in await q.GetDynamicResultAsync().ConfigureAwait(false))
                    {
                        var perfData = new PerfRawData(this, data);
                        var name = perfData.Identifier;
                        var iface = Interfaces.Find(i => name == GetCounterName(i.Name));
                        if (iface == null) continue;

                        iface.InBps = (float)perfData.GetCalculatedValue("BytesReceivedPersec", 10000000);
                        iface.OutBps = (float)perfData.GetCalculatedValue("BytesSentPersec", 10000000);
                        iface.InPps = (float)perfData.GetCalculatedValue("PacketsReceivedPersec", 10000000);
                        iface.OutPps = (float)perfData.GetCalculatedValue("PacketsSentPersec", 10000000);

                        var util = new Interface.InterfaceUtilization
                        {
                            DateEpoch = queryTime,
                            InAvgBps = iface.InBps,
                            OutAvgBps = iface.OutBps
                        };

                        var netData = NetHistory.GetOrAdd(iface.Name, k => new List<Interface.InterfaceUtilization>(1024));
                        UpdateHistoryStorage(netData, util);

                        if (PrimaryInterfaces.Contains(iface))
                        {
                            combinedUtil.InAvgBps += util.InAvgBps;
                            combinedUtil.OutAvgBps += util.OutAvgBps;
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
                    ReadAvgBps = 0,
                    WriteAvgBps = 0
                };

                using (var q = Wmi.Query(Endpoint, query))
                {
                    foreach (var data in await q.GetDynamicResultAsync().ConfigureAwait(false))
                    {
                        var perfData = new PerfRawData(this, data);

                        var name = perfData.Identifier;
                        var iface = Volumes.Find(i => name == GetCounterName(i.Name));
                        if (iface == null) continue;

                        iface.ReadBps = (float)perfData.GetCalculatedValue("DiskReadBytesPersec", 10000000);
                        iface.WriteBps = (float)perfData.GetCalculatedValue("DiskWriteBytesPersec", 10000000);

                        var util = new Volume.VolumePerformanceUtilization
                        {
                            DateEpoch = queryTime,
                            ReadAvgBps = iface.ReadBps,
                            WriteAvgBps = iface.WriteBps
                        };

                        var netData = VolumePerformanceHistory.GetOrAdd(iface.Name, k => new List<Volume.VolumePerformanceUtilization>(1024));
                        UpdateHistoryStorage(netData, util);

                        //if (PrimaryInterfaces.Contains(iface))
                        {
                            combinedUtil.ReadAvgBps += util.ReadAvgBps;
                            combinedUtil.WriteAvgBps += util.WriteAvgBps;
                        }
                    }
                }

                UpdateHistoryStorage(CombinedVolumePerformanceHistory, combinedUtil);
            }

            public async Task<ServiceActionResult> UpdateServiceAsync(string serviceName, NodeService.Action action)
            {
                string query = $"SELECT Name FROM Win32_Service WHERE Name = '{serviceName}'";

                uint returnCode = 0;

                using (var q = Wmi.Query(Endpoint, query))
                {
                    foreach (var service in await q.GetDynamicResultAsync().ConfigureAwait(false))
                    {
                        switch (action)
                        {
                            case NodeService.Action.Stop:
                                returnCode = service.StopService();
                                break;
                            case NodeService.Action.Start:
                                returnCode = service.StartService();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(action));
                        }
                    }
                }

                return new ServiceActionResult(returnCode == 0, Win32ServiceReturnCodes[(int)returnCode]);
            }

            #region private helpers

            private Task<bool> GetIsVMHost()
                => Wmi.ClassExists(Endpoint, "Win32_PerfRawData_HvStats_HyperVHypervisorLogicalProcessor");

            private async Task<string> GetRealAdapterName(string pnpDeviceId)
            {
                var query = $"SELECT Name FROM Win32_PnPEntity WHERE DeviceId = '{pnpDeviceId.Replace("\\", "\\\\")}'";
                var data = await Wmi.Query(Endpoint, query).GetFirstResultAsync().ConfigureAwait(false);

                return data?.Name;
            }

            private async Task<bool> GetCanQueryAdapterUtilization()
            {
                // it's much faster trying to query something potentially non existent and catching an exception than to query the "meta_class" table.
                const string query = "SELECT name FROM Win32_PerfRawData_Tcpip_NetworkAdapter";

                try
                {
                    using (var q = Wmi.Query(Endpoint, query))
                    {
                        await q.GetFirstResultAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                    return false;
                }

                return true;
            }

            #endregion

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

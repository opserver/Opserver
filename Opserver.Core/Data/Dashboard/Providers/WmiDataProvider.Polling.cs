using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
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
                    var tasks = new[] { UpdateNodeDataAsync(), GetAllInterfacesAsync(), GetAllVolumesAsync() };
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    SetReferences();

                    // first run, do a follow-up poll for all stats on the first pass
                    if (_nodeInfoAvailable == false)
                    {
                        _nodeInfoAvailable = true;
                        await PollStats();
                    }
                }
                catch (COMException e)
                {
                    Current.LogException(e);
                    Status = NodeStatus.Unreachable;
                }
                return this;
            }

            public async Task<Node> PollStats()
            {
                if (_nodeInfoAvailable == false)
                {
                    return this;
                }

                try
                {
                    var tasks = new[] { PollCpuUtilizationAsync(), PollMemoryUtilizationAsync(), PollNetworkUtilizationAsync(), PollVolumePerformanceUtilizationAsync() };
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (COMException e)
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
                        MemoryUsed = TotalMemory - data.FreePhysicalMemory * 1024;
                        KernelVersion = Version.Parse(data.Version);
                        MachineType = data.Caption.ToString() + " " + data.Version.ToString();
                    }
                }
                if (this.Manufacturer.ToLower().Contains("dell"))
                {
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
                        var i = Interfaces.FirstOrDefault(x => x.Id == id) ?? new Interface();
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
                            var teamInterface = Interfaces.FirstOrDefault(x => x.Caption == data.Name);
                            //var teamInterface = Interfaces.FirstOrDefault(x => x.Id == data.InstanceID);

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

                            Interface teamInterface;
                            if (teamNamesToInterfaces.TryGetValue(teamName, out teamInterface))
                            {
                                var adapterName = data.Name;
                                var memberInterface = Interfaces.FirstOrDefault(x => x.Name == adapterName);
                                //var adapterId = data.InstanceID;
                                //var memberInterface = Interfaces.FirstOrDefault(x => x.Id == adapterId);

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
                        Interface i;
                        if (indexMap.TryGetValue(data.InterfaceIndex, out i))
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
                                IPNet net;
                                int cidr;
                                if (int.TryParse(subnets[j], out cidr) && IPNet.TryParse(ips[j], cidr, out net))
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
                        var v = Volumes.FirstOrDefault(x => x.Id == id) ?? new Volume();

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

            private async Task PollCpuUtilizationAsync()
            {
                var query = IsVMHost
                    ? @"SELECT Name, Timestamp_Sys100NS, PercentTotalRunTime FROM Win32_PerfRawData_HvStats_HyperVHypervisorLogicalProcessor WHERE Name = '_Total'"
                    : @"SELECT Name, Timestamp_Sys100NS, PercentProcessorTime FROM Win32_PerfRawData_PerfOS_Processor WHERE Name = '_Total'";

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
                const string query = @"SELECT AvailableKBytes FROM Win32_PerfRawData_PerfOS_Memory";

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
                        var iface = Interfaces.FirstOrDefault(i => name == GetCounterName(i.Name));
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
                var query = $@"
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
                        var iface = Volumes.FirstOrDefault(i => name == GetCounterName(i.Name));
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

            #region private helpers

            private async Task<bool> GetIsVMHost()
            {
                const string query = "SELECT Name FROM Win32_OptionalFeature WHERE (Name = 'Microsoft-Hyper-V' OR Name = 'Microsoft-Hyper-V-Hypervisor') AND InstallState = 1";

                using (var q = Wmi.Query(Endpoint, query))
                {
                    var data = await q.GetFirstResultAsync().ConfigureAwait(false);
                    return data != null;
                }
            }

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
        }
    }
}
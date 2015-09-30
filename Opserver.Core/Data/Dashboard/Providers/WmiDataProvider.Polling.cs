using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Monitoring;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class WmiDataProvider
    {
        private partial class WmiNode
        {
            public async Task<Node> PollNodeInfo()
            {
                try
                {
                    // TODO: Check concurrency options for a Task.WaitAll
                    await UpdateNodeData();
                    await GetAllInterfaces();
                    await GetAllVolumes();
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
                try
                {
                    // TODO: Check concurrency options for a Task.WaitAll
                    await PollCpuUtilization();
                    await PollMemoryUtilization();
                    await PollNetworkUtilization();
                }
                catch (COMException e)
                {
                    Current.LogException(e);
                    Status = NodeStatus.Unreachable;
                }
                return this;
            }

            private async Task UpdateNodeData()
            {
                const string machineQuery = @"select 
                DNSHostName,
                Manufacturer,
                Model
                from Win32_ComputerSystem";
                using (var q = Wmi.Query(Name, machineQuery))
                {
                    var data = await q.GetFirstResult();
                    if (data == null)
                        return;
                    Model = data.Model;
                    Manufacturer = data.Manufacturer;
                    Name = data.DNSHostName;
                }

                const string query = @"select 
                Caption,
                LastBootUpTime,
                Version,
                FreePhysicalMemory,
                TotalVisibleMemorySize,
                Version
                from Win32_OperatingSystem";

                using (var q = Wmi.Query(Name, query))
                {
                    var data = await q.GetFirstResult();
                    if (data == null)
                        return;
                    LastBoot = ManagementDateTimeConverter.ToDateTime(data.LastBootUpTime);
                    TotalMemory = data.TotalVisibleMemorySize * 1024;
                    MemoryUsed = TotalMemory - data.FreePhysicalMemory * 1024;
                    KernelVersion = Version.Parse(data.Version);
                    MachineType = data.Caption.ToString() + " " + data.Version.ToString();
                }

                LastSync = DateTime.UtcNow;
                Status = NodeStatus.Active;
            }

            private async Task GetAllInterfaces()
            {
                if (KernelVersion > WindowsKernelVersions.Windows2012And8)
                {
                    //ActiveMaximumTransmissionUnit
                    //MtuSize
                    //
                    //Speed
                }
                else
                {
                    
                }

                const string query = @"SELECT 
                NetConnectionID,
                Description,
                Name,
                MACAddress,
                Speed
                FROM Win32_NetworkAdapter
                WHERE NetConnectionStatus = 2"; //connected adapters.
                //'AND PhysicalAdapter = True' causes exceptions with old windows versions.

                using (var q = Wmi.Query(Name, query))
                {
                    foreach (var data in await q.GetDynamicResult())
                    {
                        string name = data.Name,
                               caption = data.NetConnectionID;
                        if (caption == "Ethernet") caption = name;

                        var i = Interfaces.FirstOrDefault(x => x.Name == name && x.Caption == caption);
                        if (i == null)
                        {
                            i = new Interface();
                            Interfaces.Add(i);
                        }

                        i.Alias = "!alias";
                        i.Caption = caption;
                        i.FullName = data.Description;
                        i.IfName = data.Name;
                        i.Id = $"{Id}-Int-{Interfaces.Count + 1}";
                        i.NodeId = Id;
                        i.Index = 0;
                        i.IsTeam = false; //TODO: Fix
                        i.LastSync = DateTime.UtcNow;
                        i.Name = name;
                        i.PhysicalAddress = data.MACAddress;
                        i.Speed = data.Speed;
                        i.Status = NodeStatus.Active;
                        i.TypeDescription = "";
                    }
                }
            }

            private async Task GetAllVolumes()
            {
                const string query = @"SELECT 
                Caption,
                Description,
                FreeSpace,
                Name,
                Size,
                VolumeSerialNumber
                FROM Win32_LogicalDisk
            WHERE  DriveType = 3"; //fixed disks

                using (var q = Wmi.Query(Name, query))
                {
                    foreach (var disk in await q.GetDynamicResult())
                    {
                        var serial = disk.VolumeSerialNumber;
                        var v = Volumes.FirstOrDefault(x => x.Caption == serial);
                        if (v == null)
                        {
                            v = new Volume();
                            Volumes.Add(v);
                        }

                        v.Id = $"{Id}-Vol-{Volumes.Count + 1}";
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
                            v.PercentUsed = (float)(100 * v.Used / v.Size);
                        }
                    }
                }
            }
            
            private async Task PollCpuUtilization()
            {
                const string query = @"select 
                    PercentProcessorTime 
                    from Win32_PerfFormattedData_PerfOS_Processor
                    where Name = '_Total'";

                using (var q = Wmi.Query(Name, query))
                {
                    var data = await q.GetFirstResult();
                    if (data == null)
                        return;
                
                    CPULoad = (short)data.PercentProcessorTime;
                    var cpuUtilization = new CPUUtilization
                    {
                        DateTime = DateTime.UtcNow,
                        MaxLoad = CPULoad,
                        AvgLoad = CPULoad
                    };
                    AddCpuUtilization(cpuUtilization);
                }
            }

            private async Task PollMemoryUtilization()
            {
                const string query = @"select 
                    AvailableKBytes 
                    from Win32_PerfFormattedData_PerfOS_Memory";

                using (var q = Wmi.Query(Name, query))
                {
                    var data = await q.GetFirstResult();
                    if (data == null)
                        return;

                    var available = data.AvailableKBytes * 1024;
                    MemoryUsed = TotalMemory - available;
                    var utilization = new MemoryUtilization
                    {
                        DateTime = DateTime.UtcNow,
                        MaxMemoryUsed = MemoryUsed,
                        AvgMemoryUsed = MemoryUsed
                    };
                    AddMemoryUtilization(utilization);
                }
            }

            private async Task PollNetworkUtilization()
            {
                const string queryTemplate = @"select 
                    BytesReceivedPersec,
                    BytesSentPersec,
                    PacketsReceivedPersec,
                    PacketsSentPersec
                    FROM Win32_PerfFormattedData_Tcpip_NetworkInterface where name = '{name}'";

                foreach (var iface in Interfaces)
                {
                    var perfCounterName = iface.Name;
                    //adjust performance counter special symbols for instance name.
                    perfCounterName = perfCounterName.Replace("\\", "_");
                    perfCounterName = perfCounterName.Replace("/", "_");
                    perfCounterName = perfCounterName.Replace("(", "[");
                    perfCounterName = perfCounterName.Replace(")", "]");
                    perfCounterName = perfCounterName.Replace("#", "_");

                    var query = queryTemplate.Replace("{name}", perfCounterName);
                    using (var q = Wmi.Query(Name, query))
                    {
                        var data = await q.GetFirstResult();
                        if (data == null)
                            continue;

                        iface.InBps = data.BytesReceivedPersec;
                        iface.OutBps = data.BytesSentPersec;
                        iface.InPps = data.PacketsReceivedPersec;
                        iface.OutPps = data.PacketsSentPersec;

                        AddNetworkUtilization(iface, new Interface.InterfaceUtilization
                        {
                            DateTime = DateTime.UtcNow,
                            InMaxBps = iface.InBps,
                            OutMaxBps = iface.OutBps
                        });
                    }
                }
            }
        
        }
    }
}
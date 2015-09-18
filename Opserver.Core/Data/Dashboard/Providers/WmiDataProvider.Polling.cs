using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using StackExchange.Opserver.Monitoring;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class WmiDataProvider
    {
        private Node GetDynamicData(WmiNode wmiNode)
        {
            try
            {
                PollCpuUtilization(wmiNode);
                PollMemoryUtilization(wmiNode);
                PollNetworkUtilization(wmiNode);
            }
            catch (COMException e)
            {
                Current.LogException(e);
                wmiNode.Node.Status = NodeStatus.Unreachable;
            }
            return wmiNode.Node;
        }

        private Node GetStaticData(WmiNode wmiNode)
        {
            try
            {
                UpdateNodeData(wmiNode.Node);
                GetAllVolumes(wmiNode);
                GetAllInterfaces(wmiNode);
            }
            catch (COMException e)
            {
                Current.LogException(e);
                wmiNode.Node.Status = NodeStatus.Unreachable;
            }
            return wmiNode.Node;
        }

        private void PollMemoryUtilization(WmiNode wmiNode)
        {
            var node = wmiNode.Node;

            const string query = @"select 
                AvailableKBytes 
                from Win32_PerfFormattedData_PerfOS_Memory";

            using (var q = Wmi.Query(node.Name, query))
            {
                var data = q.GetFirstResult();
                if (data == null)
                    return;

                var available = data.AvailableKBytes * 1024;
                node.MemoryUsed = node.TotalMemory - available;
                var utilization = new Node.MemoryUtilization
                {
                    DateTime = DateTime.UtcNow,
                    MaxMemoryUsed = node.MemoryUsed,
                    AvgMemoryUsed = node.MemoryUsed
                };
                wmiNode.AddMemoryUtilization(utilization);
            }
        }

        private void PollCpuUtilization(WmiNode wmiNode)
        {
            var node = wmiNode.Node;

            const string query = @"select 
                PercentProcessorTime 
                from Win32_PerfFormattedData_PerfOS_Processor
                where Name = '_Total'";

            using (var q = Wmi.Query(node.Name, query))
            {
                var data = q.GetFirstResult();
                if (data == null)
                    return;

                node.CPULoad = (short)data.PercentProcessorTime;
                var cpuUtilization = new Node.CPUUtilization
                {
                    DateTime = DateTime.UtcNow,
                    MaxLoad = node.CPULoad,
                    AvgLoad = node.CPULoad
                };
                wmiNode.AddCpuUtilization(cpuUtilization);
            }
        }

        private void UpdateNodeData(Node node)
        {
            UpdateOsData(node);
            UpdateComputerData(node);

            node.LastSync = DateTime.UtcNow;
            node.Status = NodeStatus.Active;
        }

        private static void UpdateComputerData(Node node)
        {
            const string machineQuery = @"select 
                DNSHostName,
                Manufacturer,
                Model
                from Win32_ComputerSystem";
            using (var q = Wmi.Query(node.Name, machineQuery))
            {
                var data = q.GetFirstResult();
                if (data == null)
                    return;
                node.Model = data.Model;
                node.Manufacturer = data.Manufacturer;
                node.Name = data.DNSHostName;
            }
        }

        private static void UpdateOsData(Node node)
        {
            const string query = @"select 
                Caption,
                LastBootUpTime,
                Version,
                FreePhysicalMemory,
                TotalVisibleMemorySize
                from Win32_OperatingSystem";

            using (var q = Wmi.Query(node.Name, query))
            {
                var data = q.GetFirstResult();
                if (data == null)
                    return;
                node.LastBoot = ManagementDateTimeConverter.ToDateTime(data.LastBootUpTime);
                node.TotalMemory = data.TotalVisibleMemorySize * 1024;
                node.MemoryUsed = node.TotalMemory - data.FreePhysicalMemory * 1024;
                node.MachineType = data.Caption.ToString() + " " + data.Version.ToString();
            }
        }

        private void PollNetworkUtilization(WmiNode node)
        {
            const string queryTemplate = @"select 
                BytesReceivedPersec,
                BytesSentPersec,
                PacketsReceivedPersec,
                PacketsSentPersec
                FROM Win32_PerfFormattedData_Tcpip_NetworkInterface where name = '{name}'";

            foreach (var iface in node.Interfaces)
            {
                var perfCounterName = iface.Name;
                //adjust performance counter special symbols for instance name.
                perfCounterName = perfCounterName.Replace("\\", "_");
                perfCounterName = perfCounterName.Replace("/", "_");
                perfCounterName = perfCounterName.Replace("(", "[");
                perfCounterName = perfCounterName.Replace(")", "]");
                perfCounterName = perfCounterName.Replace("#", "_");

                var query = queryTemplate.Replace("{name}", perfCounterName);
                using (var q = Wmi.Query(node.Node.Name, query))
                {
                    var data = q.GetFirstResult();
                    if (data == null)
                        continue;

                    iface.InBps = data.BytesReceivedPersec;
                    iface.OutBps = data.BytesSentPersec;
                    iface.InPps = data.PacketsReceivedPersec;
                    iface.OutPps = data.PacketsSentPersec;

                    node.AddNetworkUtilization(iface, new Interface.InterfaceUtilization
                    {
                        DateTime = DateTime.UtcNow,
                        InMaxBps = iface.InBps,
                        OutMaxBps = iface.OutBps
                    });
                }
            }
        }

        private void GetAllInterfaces(WmiNode node)
        {
            const string query = @"SELECT 
                NetConnectionID,
                Description,
                Name,
                MACAddress,
                Speed
                FROM Win32_NetworkAdapter
                WHERE NetConnectionStatus = 2"; //connected adapters.
            //'AND PhysicalAdapter = True' causes exceptions with old windows versions.

            using (var q = Wmi.Query(node.Node.Name, query))
            {
                foreach (var data in q.GetDynamicResult())
                {
                    string name = data.Name;
                    var i = node.Interfaces.FirstOrDefault(x => x.Name == name);
                    if (i == null)
                    {
                        i = new Interface();
                        node.Interfaces.Add(i);
                    }

                    i.Alias = "!alias";
                    i.Caption = data.NetConnectionID;
                    i.DataProvider = this;
                    i.FullName = data.Description;
                    i.IfName = data.Name;
                    i.Id = node.Id*10000 + node.Interfaces.Count + 1;
                    i.NodeId = node.Id;
                    i.Index = 0;
                    i.IsTeam = false;
                    i.LastSync = DateTime.UtcNow;
                    i.Name = data.Name;
                    i.NodeId = node.Id;
                    i.PhysicalAddress = data.MACAddress;
                    i.Speed = data.Speed;
                    i.Status = NodeStatus.Active;
                    i.TypeDescription = "";
                }
            }
        }

        private void GetAllVolumes(WmiNode node)
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

            using (var q = Wmi.Query(node.Node.Name, query))
            {
                foreach (var disk in q.GetDynamicResult())
                {
                    var serial = disk.VolumeSerialNumber;
                    var v = node.Volumes.FirstOrDefault(x => x.Caption == serial);
                    if (v == null)
                    {
                        v = new Volume();
                        node.Volumes.Add(v);
                    }

                    v.Id = node.Id * 20000 + node.Volumes.Count + 1;
                    v.Available = disk.FreeSpace;
                    v.Caption = disk.VolumeSerialNumber;
                    v.Description = disk.Name + " - " + disk.Description;
                    v.DataProvider = this;
                    v.Name = disk.Name;
                    v.NodeId = node.Id;
                    v.Size = disk.Size;
                    v.Type = "Fixed Disk";
                    v.Status = NodeStatus.Active;
                    v.Used = v.Size - v.Available;
                    if (v.Size > 0)
                    {
                        v.PercentUsed = (float) (100*v.Used/v.Size);
                    }
                }
            }
        }
    }
}
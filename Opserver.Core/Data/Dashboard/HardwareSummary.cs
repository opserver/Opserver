using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public HardwareSummary Hardware { get; internal set; }
    }

    public class HardwareSummary
    {
        public List<ProcessorInfo> Processors { get; internal set; } = new List<ProcessorInfo>(); 
        public List<MemoryModuleInfo> MemoryModules { get; internal set; } = new List<MemoryModuleInfo>();
        public List<ComponentInfo> Components { get; internal set; } = new List<ComponentInfo>();
        public StorageInfo Storage { get; internal set; } = new StorageInfo();
        public List<TemperatureInfo> Temps { get; internal set; } = new List<TemperatureInfo>();
        public List<PowerSupplyInfo> PowerSupplies { get; internal set; } = new List<PowerSupplyInfo>();
        public BoardPowerInfo BoardPowerReading { get; internal set; } = new BoardPowerInfo();
        
        public class ComponentInfo : IMonitorStatus
        {
            public string Name { get; internal set; }
            public string Status { get; internal set; }

            public MonitorStatus MonitorStatus => Status == "Ok" ? MonitorStatus.Good : MonitorStatus.Warning;
            public string MonitorStatusReason => "Status is " + Status;
        }

        public class ProcessorInfo
        {
            public string Name { get; internal set; }
            public string Description { get; internal set; }
        }

        public class MemoryModuleInfo : ComponentInfo
        {
            public string Size { get; internal set; }
            public string PrettyName => Name?.Replace("DIMM_", "");
            private string _bank;
            public string Bank => _bank ?? (_bank = Name?.TrimEnd(StringSplits.Numbers));
            private int? _label;
            public int? Label
            {
                get
                {
                    if (!_label.HasValue)
                    {
                        if (Bank == null) return null;
                        if (Name.Length > Bank.Length)
                        {
                            int position;
                            if (int.TryParse(Name.Substring(Bank.Length), out position))
                            {
                                _label = position;
                            }
                        }
                    }
                    return _label;
                }
            } 
        }

        public class StorageInfo
        {
            public List<ControllerInfo> Controllers { get; internal set; } = new List<ControllerInfo>();
            public List<PhysicalDiskInfo> PhysicalDisks { get; internal set; } = new List<PhysicalDiskInfo>();
            public List<VirtualDiskInfo> VirtualDisks { get; internal set; } = new List<VirtualDiskInfo>();
            public List<ComponentInfo> Batteries { get; internal set; } = new List<ComponentInfo>();

            public class ControllerInfo : ComponentInfo
            {
                public string SlotId { get; internal set; }
                public string State { get; internal set; }
                public string FirmwareVersion { get; internal set; }
                public string DriverVersion { get; internal set; }
            }

            public class PhysicalDiskInfo : ComponentInfo
            {
                public string Media { get; internal set; }
                public string Capacity { get; internal set; }
                public string VendorId { get; internal set; }
                public string ProductId { get; internal set; }
                public string Serial { get; internal set; }
                public string Part { get; internal set; }
                public string NegotatiedSpeed { get; internal set; }
                public string CapableSpeed { get; internal set; }
                public string SectorSize { get; internal set; }
            }

            public class VirtualDiskInfo : ComponentInfo
            {
                public long Size { get; internal set; }
            }
        }

        public class TemperatureInfo : ComponentInfo
        {
            public double Celsius { get; internal set; }
        }

        public class PowerSupplyInfo : ComponentInfo
        {
            public double Amps { get; internal set; }
            public double Volts { get; internal set; }
        }

        public class BoardPowerInfo
        {
            public double Watts { get; internal set; }
        }
    }
}

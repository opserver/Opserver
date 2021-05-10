using System.Collections.Generic;
using Opserver.Data;

namespace Opserver.Views.Shared.Guages
{
    public class CircleModel
    {
        public string Label { get; set; }
        public IEnumerable<IMonitorStatus> Items { get; set; }

        public CircleModel(string label, IEnumerable<IMonitorStatus> items)
        {
            Label = label;
            Items = items;
        }
    }
}

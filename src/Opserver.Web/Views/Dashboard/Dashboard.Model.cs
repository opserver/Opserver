using System.Collections.Generic;
using Opserver.Data.Dashboard;

namespace Opserver.Views.Dashboard
{
    public class DashboardModel
    {
        public string Filter { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<Node> Nodes { get; set; }
        public bool IsStartingUp { get; set; }
    }
}
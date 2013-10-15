using System.Collections.Generic;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Views.Dashboard
{
    public class DashboardModel
    {
        public string Filter { get; set; }
        public IEnumerable<string> ErrorMessages { get; set; }

        private List<Node> _nodes;
        public List<Node> Nodes
        {
            get { return _nodes ?? DashboardData.AllNodes; }
            set { _nodes = value; }
        }
    }
}
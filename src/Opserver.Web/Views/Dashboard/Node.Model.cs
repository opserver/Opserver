using Opserver.Data.Dashboard;

namespace Opserver.Views.Dashboard
{
    public class NodeModel
    {
        public Node CurrentNode { get; set; }
        public CurrentStatusTypes? CurrentStatusType { get; set; }
    }
}

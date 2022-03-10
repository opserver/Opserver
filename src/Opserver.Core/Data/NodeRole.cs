using System.Collections.Generic;
using System.Threading.Tasks;

namespace Opserver.Data
{
    public class NodeRole
    {
        public string Node { get; set; }
        public string Service { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public int? SiblingsActive { get; set; }
        public int? SiblingsInactive { get; set; }
    }

    public interface INodeRoleProvider
    {
        IEnumerable<NodeRole> GetRoles(string node);
        Task<bool> EnableAsync(string node);
        Task<bool> DisableAsync(string node);
    }
}

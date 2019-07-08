using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data
{
    public class NodeRole
    {
        public string Node { get; set; }
        public string Service { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public int? SiblingsActive { get; set; }
        public int? SiblingsInactive { get; set; }

        internal static ConcurrentBag<INodeRoleProvider> Providers { get; } = new ConcurrentBag<INodeRoleProvider>();

        public static IEnumerable<NodeRole> Get(string node)
        {
            foreach (var p in Providers)
            {
                foreach (var r in p.GetRoles(node))
                {
                    yield return r;
                }
            }
        }

        public static async Task<bool> EnableAllAsync(string node)
        {
            var tasks = new List<Task<bool>>();
            foreach (var p in Providers)
            {
                tasks.Add(p.EnableAsync(node));
            }
            await Task.WhenAll(tasks);
            return tasks.TrueForAll(b => b.Result);
        }

        public static async Task<bool> DisableAllAsync(string node)
        {
            var tasks = new List<Task<bool>>();
            foreach (var p in Providers)
            {
                tasks.Add(p.DisableAsync(node));
            }
            await Task.WhenAll(tasks);
            return tasks.TrueForAll(b => b.Result);
        }
    }

    public static class NodeRoleExtensions
    {
        public static void Register(this INodeRoleProvider provider)
        {
            NodeRole.Providers.Add(provider);
        }
    }

    public interface INodeRoleProvider
    {
        IEnumerable<NodeRole> GetRoles(string node);
        Task<bool> EnableAsync(string node);
        Task<bool> DisableAsync(string node);
    }
}

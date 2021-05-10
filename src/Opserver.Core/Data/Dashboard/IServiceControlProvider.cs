using System.Threading.Tasks;

namespace Opserver.Data.Dashboard
{
    public interface IServiceControlProvider
    {
        Task<ServiceActionResult> UpdateServiceAsync(Node node, string serviceName, NodeService.Action action);
    }

    public static class ServiceControlProviderExtensions
    {
        public static bool HasServiceControl(this Node node) => node?.DataProvider is IServiceControlProvider;
    }
}

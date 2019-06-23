using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard
{
    public interface IServiceControlProvider
    {
        Task<ServiceActionResult> UpdateServiceAsync(Node node, string serviceName, Data.Dashboard.NodeService.Action action);
    }

    public static class ServiceControlProviderExtensions
    {
        public static bool HasServiceControl(this Node node) => node?.DataProvider is IServiceControlProvider;
    }
}

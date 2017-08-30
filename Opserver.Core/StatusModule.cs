using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver
{
    /// <summary>
    /// The base of all plugins on the data side
    /// </summary>
    public abstract class StatusModule
    {
        // TODO: Top tab registration, but that needs monitor status...
        // TODO: public abstract MonitorStatus MoniorStatus { get; }
        public abstract bool IsMember(string node);
    }

    public static class StatusModuleExtensions
    {
        public static bool IsMember<T>(this Node node) where T : StatusModule, new() => Singleton<T>.Instance.IsMember(node.PrettyName);
    }
}

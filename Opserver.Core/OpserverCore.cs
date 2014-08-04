using StackExchange.Elastic;

namespace StackExchange.Opserver
{
    public class OpserverCore
    {
        // Initializes various bits in OpserverCore like exception logging and such so that projects using the core need not load up all references to do so.
        public static void Init()
        {
            try
            {
                ElasticException.ExceptionDataPrefix = ExtensionMethods.ExceptionLogPrefix;
                // We're going to get errors - that's kinda the point of monitoring
                // No need to log every one here unless for crazy debugging
                //ElasticException.ExceptionOccurred += e => Current.LogException(e);
            }
            catch { }
        }
    }
}

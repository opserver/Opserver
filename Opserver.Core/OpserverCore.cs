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
                ElasticException.ExceptionOccurred += e => Current.LogException(e);
            }
            catch { }
        }
    }
}

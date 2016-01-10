using System.Net;

namespace StackExchange.Opserver
{
    public class OpserverCore
    {
        // Initializes various bits in OpserverCore like exception logging and such so that projects using the core need not load up all references to do so.
        public static void Init()
        {
            // Enable TLS only for SSL negotiation, SSL has been broken for some time.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }
    }
}

using System;
using System.Collections.Generic;
using Nest;

namespace StackExchange.Opserver.Data.Elastic
{
    public abstract class ElasticDataObject
    {
        public bool IsValid { get; set; }

        public void PopulateFromConnections(IEnumerable<string> settingNodes)
        {
            string lastErrorMessage = null;
            Exception lastException = null;
            foreach (var n in settingNodes)
            {
                var uri = new Uri(string.Format("http://{0}", n));
                var cs = new ConnectionSettings(uri).SetTimeout(2000);
                var cli = new ElasticClient(cs);
                IResponse response = RefreshFromConnection(cli);

                // Some implementations are raw
                if (response == null) return;

                // All's well with the world
                if (response.IsValid) return;

                if (response.ConnectionStatus.Error == null) continue;
                lastErrorMessage = response.ConnectionStatus.Error.ExceptionMessage;
                lastException = response.ConnectionStatus.Error.OriginalException;
            }
            // Failed to poll all nodes
            if (lastErrorMessage.HasValue())
            {
                throw new Exception("Failed to poll all elastic nodes for " + GetType().Name + ": " + lastErrorMessage, lastException);
            }
            throw new Exception("Failed to poll all elastic nodes for " + GetType().Name);
        }

        public abstract IResponse RefreshFromConnection(ElasticClient cli);
    }
}

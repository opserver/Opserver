using System;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public abstract class ElasticDataObject
    {
        public void PopulateFromConnections(SearchClient client)
        {
            var response = RefreshFromConnection(client);

            // Some implementations are raw
            if (response?.Exception == null) return;
            string lastErrorMessage = response.Exception.Message;
            Exception lastException = response.Exception;

            // Failed to poll all nodes
            if (lastErrorMessage.HasValue())
            {
                throw new Exception($"Failed to poll all elastic nodes for {GetType().Name}: {lastErrorMessage}", lastException);
            }
            throw new Exception($"Failed to poll all elastic nodes for {GetType().Name}");
        }

        public abstract ElasticResponse RefreshFromConnection(SearchClient cli);
    }
}

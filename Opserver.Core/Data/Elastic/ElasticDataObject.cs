using System;
using System.Threading.Tasks;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public abstract class ElasticDataObject
    {
        public async Task PopulateFromConnections(SearchClient client)
        {
            var response = await RefreshFromConnectionAsync(client);

            // Some implementations are raw
            if (response?.Exception == null) return;
            var lastErrorMessage = response.Exception.Message;
            var lastException = response.Exception;

            // Failed to poll all nodes
            if (lastErrorMessage.HasValue())
            {
                throw new Exception($"Failed to poll all elastic nodes for {GetType().Name}: {lastErrorMessage}", lastException);
            }
            throw new Exception($"Failed to poll all elastic nodes for {GetType().Name}");
        }

        public abstract Task<ElasticResponse> RefreshFromConnectionAsync(SearchClient cli);
    }
}

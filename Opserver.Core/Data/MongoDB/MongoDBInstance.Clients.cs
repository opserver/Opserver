using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace StackExchange.Opserver.Data.MongoDB
{
    public partial class MongoDBInstance
    {
        private Cache<ConcurrentBag<MongoConnectedClient>> _clients;
        public Cache<ConcurrentBag<MongoConnectedClient>> Clients
        {
            get
            {
                return _clients ?? (_clients = new Cache<ConcurrentBag<MongoConnectedClient>>
                {
                    CacheForSeconds = 60,
                    UpdateCache = GetFromMongoDBAsync(nameof(Clients), async rc =>
                    {
                        var server = rc.GetSingleServer().MongoClient;
                        var db = server.GetDatabase("admin");

                        var collection = db.GetCollection<BsonDocument>("$cmd.sys.inprog");
                        var filter = new BsonDocument();
                        filter["$all"] = true;

                        var clients = new ConcurrentBag<MongoConnectedClient>();

                        using (var cursor = await collection.FindAsync(filter))
                        {
                            var doc = await cursor.FirstAsync();
                            {
                                var c = doc["inprog"].AsBsonArray;
                                foreach (var item in c)
                                {
                                    var b = item.AsBsonDocument;
                                    if (b.Contains("client"))
                                    {
                                        string ip = b["client"].AsString;
                                        bool active = b["active"].AsBoolean;

                                        clients.Add(new MongoConnectedClient() {Ip = ip, Active = active });
                                    }
                                }
                            }
                        }

                        return clients;
                    })
                });
            }
        }
    }

    public class MongoConnectedClient
    {
        public string Ip { get; set; }

        public bool Active { get; set; }

        public IMongoClient MongoClient { get; set; }
    }
}
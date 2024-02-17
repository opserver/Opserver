using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Opserver.Data.SQL
{
    public class SQLAzureServer : SQLInstance
    {
        private readonly ConcurrentDictionary<string, SQLInstance> _instancesByKey = new ConcurrentDictionary<string, SQLInstance>();

        private Cache<List<SQLInstance>> _instanceCache;
        public Cache<List<SQLInstance>> Instances =>
            _instanceCache ??= GetSqlCache(
                nameof(Instances), async conn =>
                {
                    var instances = new List<SQLInstance>();
                    // grab the list of databases in the SQL Azure instance
                    // and generate a SQLInstance for each one
                    var databases = await conn.QueryAsync<AzureDatabaseInfo>(@"
Select db.name Name, 
	   dbso.edition Edition,
	   dbso.service_objective SKU,
	   dbso.elastic_pool_name ElasticPoolName
  From sys.databases db
	   Join sys.database_service_objectives dbso On db.database_id = dbso.database_id");
                    foreach (var database in databases)
                    {
                        // is there an existing instance?
                        var key = Settings.Name + ":" + database.Name;
                        var instance = _instancesByKey.GetOrAdd(
                            key,
                            key => new SQLInstance(
                                    Module,
                                    new SQLSettings.Instance
                                    {
                                        Name = key,
                                        ConnectionString = new SqlConnectionStringBuilder(ConnectionString)
                                        {
                                            InitialCatalog = database.Name
                                        }.ConnectionString,
                                        RefreshIntervalSeconds = Settings.RefreshIntervalSeconds,
                                    }
                                )
                            {
                                SKU = database.SKU,
                                Edition = database.Edition,
                                ElasticPoolName = database.ElasticPoolName,
                            }
                        );

                        instances.Add(instance);
                        // make sure we're monitoring this instance
                        instance.TryAddToGlobalPollers();
                    }
                    return instances;
                });

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return Instances;
            }
        }

        public SQLAzureServer(SQLModule module, SQLSettings.Instance settings) : base(module, settings)
        {
        }

        public class AzureDatabaseInfo
        {
            public string Name { get; set; }
            public string Edition { get; set; }
            public string SKU { get; set; }
            public string ElasticPoolName { get; set; }
        }
    }
}

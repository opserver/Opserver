using System.Collections.Generic;
using System.Linq;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<IndexAliasInfo> _aliases;
        public Cache<IndexAliasInfo> Aliases
        {
            get { return _aliases ?? (_aliases = GetCache<IndexAliasInfo>(5*60)); }
        }

        public string GetIndexAliasedName(string index)
        {
            if (Aliases.Data == null || Aliases.Data.Aliases == null)
                return index;

            List<string> aliases;
            return Aliases.Data.Aliases.TryGetValue(index, out aliases)
                       ? aliases.First().IsNullOrEmptyReturn(index)
                       : index;
        }

        public class IndexAliasInfo : ElasticDataObject
        {
            public Dictionary<string, List<string>> Aliases { get; private set; }

            public override ElasticResponse RefreshFromConnection(SearchClient cli)
            {
                var rawAliases = cli.GetAliases();
                if (rawAliases.HasData)
                {
                    var result = new Dictionary<string, List<string>>();
                    foreach (var a in rawAliases.Data)
                    {
                        if (a.Value != null && a.Value.Aliases != null && a.Value.Aliases.Count > 0)
                            result.Add(a.Key, a.Value.Aliases.Keys.ToList());
                    }
                    Aliases = result;
                }

                return rawAliases;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<IndexAliasInfo> _aliases;
        public Cache<IndexAliasInfo> Aliases =>
            _aliases ?? (_aliases = GetElasticCache(async () =>
            {
                var aliases = await GetAsync<Dictionary<string, IndexAliasList>>("_aliases");
                return new IndexAliasInfo
                {
                    Aliases = aliases?.Where(a => a.Value?.Aliases != null && a.Value.Aliases.Count > 0)
                        .ToDictionary(a => a.Key, a => a.Value.Aliases.Keys.ToList())
                              ?? new Dictionary<string, List<string>>()
                };
            }));

        public string GetIndexAliasedName(string index)
        {
            if (Aliases.Data?.Aliases == null)
                return index;

            return Aliases.Data.Aliases.TryGetValue(index, out List<string> aliases)
                       ? aliases[0].IsNullOrEmptyReturn(index)
                       : index;
        }

        public class IndexAliasInfo
        {
            public Dictionary<string, List<string>> Aliases { get; internal set; }
        }

        public class IndexAliasList
        {
            [DataMember(Name = "aliases")]
            public Dictionary<string, object> Aliases { get; internal set; }
        }
    }
}

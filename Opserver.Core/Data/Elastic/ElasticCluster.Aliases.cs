using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<IndexAliasInfo> _aliases;
        public Cache<IndexAliasInfo> Aliases => _aliases ?? (_aliases = new Cache<IndexAliasInfo>
        {
            CacheForSeconds = RefreshInterval,
            UpdateCache = UpdateFromElastic(nameof(Aliases), async () =>
            {
                var aliases = await GetAsync<Dictionary<string, IndexAliasList>>("_aliases").ConfigureAwait(false);
                return new IndexAliasInfo
                {
                    Aliases = aliases?.Where(a => a.Value?.Aliases != null && a.Value.Aliases.Count > 0)
                        .ToDictionary(a => a.Key, a => a.Value.Aliases.Keys.ToList())
                              ?? new Dictionary<string, List<string>>()
                };
            })
        });

        public string GetIndexAliasedName(string index)
        {
            if (Aliases.Data?.Aliases == null)
                return index;

            List<string> aliases;
            return Aliases.Data.Aliases.TryGetValue(index, out aliases)
                       ? aliases.First().IsNullOrEmptyReturn(index)
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

using System.Collections.Generic;
using System.Linq;
using Nest;

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

            public override IResponse RefreshFromConnection(ElasticClient cli)
            {
                var result = new Dictionary<string, List<string>>();
                var aliases = cli.GetAllIndexAliases();
                foreach (var a in aliases)
                {
                    if (a.Value != null && a.Value.Count > 0)
                        result.Add(a.Key, a.Value);
                }
                Aliases = result;
                IsValid = true;

                return null;
            }
        }
    }
}

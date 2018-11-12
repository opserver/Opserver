using System.Collections.Generic;
using System.Linq;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.Elastic;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Data.Redis;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.Dashboard
{
    public class NodeModel
    {
        public Node CurrentNode { get; set; }

        private CurrentStatusTypes? _currentStatusTypes;
        public CurrentStatusTypes CurrentStatusType
        {
            get
            {
                if (!_currentStatusTypes.HasValue) return CurrentStatusTypes.Stats;
                return StatusTypes.Contains(_currentStatusTypes.Value) ? _currentStatusTypes.Value : CurrentStatusTypes.Stats;
            }
            set { _currentStatusTypes = value; }
        }

        public IEnumerable<CurrentStatusTypes> StatusTypes
        {
            get
            {
                if (CurrentNode == null) yield break;

                yield return CurrentStatusTypes.Stats;

                // TODO: Redis, SQL, etc node recognition - more pluggable?
                // Need to nuke the enum here...
                if (CurrentNode.IsMember<SQLModule>())
                {
                    yield return CurrentStatusTypes.SQLInstance;
                    yield return CurrentStatusTypes.SQLTop;
                    yield return CurrentStatusTypes.SQLActive;
                }
                if (CurrentNode.IsMember<RedisModule>())
                {
                    yield return CurrentStatusTypes.Redis;
                }
                if (CurrentNode.IsMember<ElasticModule>())
                {
                    yield return CurrentStatusTypes.Elastic;
                }
                if (CurrentNode.IsMember<HAProxyModule>())
                {
                    yield return CurrentStatusTypes.HAProxy;
                }
                if (CurrentNode.IsVMHost)
                    yield return CurrentStatusTypes.VMHost;
                //if (CurrentNode.Interfaces.Any())
                //    yield return CurrentStatusTypes.Interfaces;
            }
        }
    }
}
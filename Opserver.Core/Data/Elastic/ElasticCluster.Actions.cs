using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        public bool AllocateShard(string index, int shard, string node)
        {
            // TODO: NEST Implementation
            // if node is null - take a stab at a random one!
            // for real though, algorithms (space probably?)
            //return PostAction("/_cluster/reroute", GetRerouteCommand(new { allocate = new { index, shard, node } }));
            return false;
        }

        public bool MoveShard(string index, int shard, string fromNode, string toNode)
        {
            // TODO: NEST Implementation
            return false;
            //return PostAction("/_clusterreroute", GetRerouteCommand(new { move = new {index, shard, from_node = fromNode, to_node = toNode} }));
        }

        public bool CancelInitialization(string index, int shard, string node)
        {
            // TODO: NEST Implementation
            return false;
            //return PostAction("/_cluster/reroute", GetRerouteCommand(new {  cancel = new {index, shard, node} }));
        }
    }
}

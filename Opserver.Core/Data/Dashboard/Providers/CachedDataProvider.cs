using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public abstract class CachedDataProvider : DashboardDataProvider
    {
        protected virtual int CacheSeconds { get { return 10; } }

        private Cache<List<Node>> _nodes;
        private Cache<List<Volume>> _volumes;
        private Cache<List<Application>> _applications;
        private Cache<List<Interface>> _interfaces;
        private Cache<List<Tuple<int, IPAddress>>> _node_ips;

        public CachedDataProvider (DashboardSettings.Provider provider) : base (provider)
        {
        }

        public override bool HasData { get { return NodeCache.HasData (); } }

        private Cache<List<Node>> NodeCache { get { return _nodes ?? (_nodes = ProviderCache (GetAllNodes, CacheSeconds)); } }
        private Cache<List<Volume>> VolumeCache { get { return _volumes ?? (_volumes = ProviderCache (GetAllVolumes, CacheSeconds)); } }
        private Cache<List<Application>> AppCache { get { return _applications ?? (_applications = ProviderCache (GetAllApplications, CacheSeconds)); } }
        private Cache<List<Interface>> InterfaceCache { get { return _interfaces ?? (_interfaces = ProviderCache (GetAllInterfaces, CacheSeconds)); } }
        private Cache<List<Tuple<int, IPAddress>>> NodeIPCache { get { return _node_ips ?? (_node_ips = ProviderCache (GetNodeToIPMap, CacheSeconds)); } }

        public override List<Node> AllNodes { get { return NodeCache.Data ?? new List<Node> (); } }
        public override List<Volume> AllVolumes { get { return VolumeCache.Data ?? new List<Volume> (); } }
        public override List<Application> AllApplications { get { return AppCache.Data ?? new List<Application> (); } }
        public override List<Interface> AllInterfaces { get { return InterfaceCache.Data ?? new List<Interface> (); } }

        // Override and implement these functions to create a data provider
        protected abstract List<Node> GetAllNodes ();
        protected abstract List<Volume> GetAllVolumes ();
        protected abstract List<Interface> GetAllInterfaces ();
        protected abstract List<Application> GetAllApplications ();
        protected abstract List<Tuple<int, IPAddress>> GetNodeToIPMap ();

        // Override and return true for the data the provider implements
        protected virtual bool ProvidesNodes { get { return false; } }
        protected virtual bool ProvidesVolumes { get { return false; } }
        protected virtual bool ProvidesInterfaces { get { return false; } }
        protected virtual bool ProvidesApplications { get { return false; } }
        protected virtual bool ProvidesNodeIPs { get { return false; } }

        public override IEnumerable<Cache> DataPollers {
            get {
                if (ProvidesNodes)
                    yield return NodeCache;

                if (ProvidesVolumes)
                    yield return VolumeCache;

                if (ProvidesInterfaces)
                    yield return InterfaceCache;

                if (ProvidesApplications)
                    yield return AppCache;

                if (ProvidesNodeIPs)
                    yield return NodeIPCache;
            }
        }

        public override IEnumerable<IPAddress> GetIPsForNode (Node node)
        {
            if (NodeIPCache.Data == null)
                return null;

            return NodeIPCache.Data.Where (p => p.Item1 == node.Id).Select (p => p.Item2);
        }

        public override IEnumerable<Node> GetNodesByIP (IPAddress ip)
        {
            if (NodeIPCache.Data == null)
                return null;

            var nodeIds = NodeIPCache.Data.Where (p => p.Item2.ToString () == ip.ToString ()).Select (p => p.Item1);
            return nodeIds.Select (p => NodeCache.Data.FirstOrDefault (q => q.Id == p)).Where (p => p != null);
        }
    }
}

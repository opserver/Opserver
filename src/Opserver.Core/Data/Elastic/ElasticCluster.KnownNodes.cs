using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Opserver.Helpers;
using StackExchange.Utils;

namespace Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        public List<ElasticNode> KnownNodes { get; set; }

        public class ElasticNode
        {
            private const int DefaultElasticPort = 9200;

            public string Host { get; set; }
            public int Port { get; set; }
            public string Url { get; set; }

            public string Name => LastStatus?.Name;
            public string ClusterName => LastStatus?.ClusterName;
            public NodeHomeInfo.VersionInfo Version => LastStatus?.Version;
            public ElasticSettings.Cluster ClusterSettings { get; }

            public DateTime? LastSeen { get; private set; }
            public Exception LastException { get; private set; }
            public NodeHomeInfo LastStatus { get; set; }

            public ElasticNode(string hostAndPort, ElasticSettings.Cluster clusterSettings)
            {
                ClusterSettings = clusterSettings;
                if (Uri.TryCreate(hostAndPort, UriKind.Absolute, out var uri))
                {
                    Url = uri.ToString();
                    Host = uri.Host;
                    Port = uri.Port;
                    return;
                }

                var parts = hostAndPort.Split(StringSplits.Colon);
                if (parts.Length == 2)
                {
                    Host = parts[0];
                    if (int.TryParse(parts[1], out int port))
                    {
                        Port = port;
                    }
                    else
                    {
                        new OpserverConfigException($"Invalid port specified for {parts[0]}: '{parts[1]}'")
                            .AddLoggedData("Config Value", hostAndPort)
                            .Log();
                        Port = DefaultElasticPort;
                    }
                }
                else
                {
                    Host = hostAndPort;
                    Port = DefaultElasticPort;
                }
                Url = $"http://{Host}:{Port}/";
            }

            public override string ToString() => Host;

            public async Task<T> GetAsync<T>(string path) where T : class
            {
                var request = Http.Request(Url + path);
                if (ClusterSettings?.AuthorizationHeader.HasValue() == true)
                {
                    request.AddHeader("Authorization", ClusterSettings.AuthorizationHeader);
                }

                var result = await request
                                   .ExpectJson<T>()
                                   .GetAsync();

                if (result.Success)
                {
                    LastSeen = DateTime.UtcNow;
                }

                LastException = result.Error;
                return result.Data;
            }
        }

        public class NodeHomeInfo
        {
            [DataMember(Name = "status")]
            public int Status { get; internal set; }
            [DataMember(Name = "name")]
            public string Name { get; internal set; }
            [DataMember(Name = "cluster_name")]
            public string ClusterName { get; internal set; }
            [DataMember(Name = "version")]
            public VersionInfo Version { get; internal set; }

            public class VersionInfo
            {
                [DataMember(Name = "number")]
                public string Number { get; internal set; }
                [DataMember(Name = "build_hash")]
                public string BuildHash { get; internal set; }
                [DataMember(Name = "lucene_version")]
                public string LuceneVersion { get; internal set; }
            }
        }
    }
}

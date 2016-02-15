using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.Elastic
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

            public DateTime? LastSeen { get; private set; }
            public Exception LastException { get; private set; }
            public NodeHomeInfo LastStatus { get; private set; }

            public ElasticNode(string hostAndPort)
            {
                Uri uri;
                if (Uri.TryCreate(hostAndPort, UriKind.Absolute, out uri))
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
                    int port;
                    if (int.TryParse(parts[1], out port))
                    {
                        Port = port;
                    }
                    else
                    {
                        Current.LogException(new ConfigurationErrorsException(
                            $"Invalid port specified for {parts[0]}: '{parts[1]}'"));
                        Port = DefaultElasticPort;
                    }
                }
                else
                {
                    Host = hostAndPort;
                    Port = DefaultElasticPort;
                }
                Url = $"http://{Host}:{Port.ToString()}/";
            }

            public async Task<T> GetAsync<T>(string path) where T : class
            {
                var wc = new WebClient();
                try
                {
                    using (var rs = await wc.OpenReadTaskAsync(Url + path).ConfigureAwait(false))
                    using (var sr = new StreamReader(rs))
                    {
                        LastSeen = DateTime.UtcNow;
                        var result = JSON.Deserialize<T>(sr);
                        LastException = null;
                        return result;
                    }
                }
                catch (SocketException e)
                {
                    LastException = e;
                    // nothing - we failed to reach a downed node which is to be expected
                }
                catch (WebException e)
                {
                    LastException = e;
                    // nothing - we failed to reach a downed node which is to be expected
                }
                catch (Exception e)
                {
                    LastException = e;
                    Current.LogException(e);
                    // In the case of a 404, 500, etc - carry on to the next node
                }
                return null;
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

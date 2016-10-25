using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class AliyunDataProvider
    {
        private partial class AliyunNode : Node
        {
            public string AccessKeyId { get; set; }

            public string AccessKeySecret { get; set; }

            public string RegionId { get; set; }

            public string InstanceId { get; set; }
        }

        public override async Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = default(int?))
        {
            var aliyunNode = node as AliyunNode;
            using (AliyunRequest request = new AliyunRequest(new AliyunSettingNode() { AccessKeyId = aliyunNode.AccessKeyId, AccessKeySecret = aliyunNode.AccessKeySecret })) {
                var response = await request.GetCPUUtilization(aliyunNode.InstanceId, start, end);

                return response.Datapoints
                    .Select(ut => new GraphPoint() {
                        DateEpoch = ut.timestamp,
                        Value = ut.Average
                    })
                    .ToList();
            }
        }

        public override async Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = default(int?))
        {
            var aliyunNode = node as AliyunNode;
            using (AliyunRequest request = new AliyunRequest(new AliyunSettingNode() { AccessKeyId = aliyunNode.AccessKeyId, AccessKeySecret = aliyunNode.AccessKeySecret })) {
                var response = await request.GetMemoryUtilization(aliyunNode.InstanceId, start, end);

                return response.Datapoints
                    .Select(ut => new GraphPoint() {
                        DateEpoch = ut.timestamp,
                        Value = aliyunNode.TotalMemory * (ut.value / 100)
                    })
                    .ToList();
            }
        }

        public override async Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = default(int?))
        {
            var aliyunNode = node as AliyunNode;
            using (AliyunRequest request = new AliyunRequest(new AliyunSettingNode() { AccessKeyId = aliyunNode.AccessKeyId, AccessKeySecret = aliyunNode.AccessKeySecret })) {
                var inNewResponse = await request.GetInternetInRateNew(aliyunNode.InstanceId, start, end);
                var outNewResponse = await request.GetIntranetOutRateNew(aliyunNode.InstanceId, start, end);

                return inNewResponse.Datapoints.Join(
                        outNewResponse.Datapoints,
                        inNew => inNew.timestamp,
                        outNew => outNew.timestamp,
                        (inNew, outNew) => new Interface.InterfaceUtilization() {
                            DateEpoch = inNew.timestamp,
                            InAvgBps = (float)inNew.Average,
                            OutAvgBps = (float)outNew.Average
                        })
                        .ToList<DoubleGraphPoint>();
            }
        }

        public override Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = default(int?))
        {
            return Task.FromResult(new List<GraphPoint>());
        }

        public override async Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = default(int?))
        {
            return await this.GetNetworkUtilizationAsync(iface.Node, start, end);
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            yield break;
        }

        protected override string GetMonitorStatusReason()
        {
            throw new NotImplementedException();
        }

        private async Task<List<Node>> GetAllAliyunNodesAsync()
        {
            List<Node> nodes = new List<Node>();
            using (AliyunRequest request = new AliyunRequest()) {
                foreach (var setting in Settings.Nodes) {
                    request.Set(setting);

                    foreach (var r in (await request.GetRegion()).Regions.Region) {
                        foreach (var i in (await request.GetInstance(r.RegionId)).Instances.Instance) {
                            var instanceMonitorData = await request.GetInstanceMonitorData(i.InstanceId);
                            var memoryCloudMonitor = await request.GetMemoryUtilization(i.InstanceId);
                            var disks = await request.GetDisks(r.RegionId, i.InstanceId);
                            var inBps = await request.GetInternetInRateNew(i.InstanceId, length: 1);
                            var outBps = await request.GetIntranetOutRateNew(i.InstanceId, length: 1);

                            var n = new AliyunNode() {
                                AccessKeyId = setting.AccessKeyId,
                                AccessKeySecret = setting.AccessKeySecret,
                                InstanceId = i.InstanceId,
                                RegionId = r.RegionId,
                                Id = i.InstanceId,
                                Name = i.HostName,
                                MachineOSVersion = "Unknown",
                                MachineType = i.ImageId,
                                Ip = i.PublicIpAddress.IpAddress.FirstOrDefault() ?? string.Empty,
                                DataProvider = this,
                                Status = GetNodeStatus(i.Status),
                                TotalMemory = i.TotalMemory,
                                MemoryUsed = ((float)memoryCloudMonitor.Datapoints.First().Average / 100 * i.TotalMemory),
                                TotalCPU = i.CPU,
                                CPULoad = (short)instanceMonitorData.MonitorData.InstanceMonitorData.First().CPU,
                                Manufacturer = "aliyun",
                                ServiceTag = i.InstanceType,

                                Interfaces = new List<Interface>() {
                                    new Interface() {
                                        Id = i.InstanceNetworkType,
                                        Name = i.InstanceNetworkType,
                                        NodeId =  i.InstanceName,
                                        Status = NodeStatus.Active,
                                        IPs =  i.PublicIpAddress.IpAddress.Select(ip => {
                                            IPNet result;
                                            return IPNet.TryParse(ip, out result) ? result : null;
                                        }).ToList(),
                                        Speed = i.InternetMaxBandwidthOut * 1024 * 1024,
                                        InBps = (float?)inBps.Datapoints.FirstOrDefault()?.Average,
                                        OutBps = (float?)outBps.Datapoints.FirstOrDefault()?.Average,

                                    }
                                },
                                Volumes = disks.Disks.Disk.Select(disk => new Volume {
                                    Id = disk.DiskId,
                                    Name = disk.DiskName,
                                    NodeId = i.InstanceName,
                                    Caption = disk.Category,
                                    Description = disk.Description,
                                    LastSync = DateTime.UtcNow,
                                    Size = disk.TotalBytes,
                                    Used = disk.UsedBytes,
                                    Available = disk.TotalBytes - disk.UsedBytes,
                                    PercentUsed = 100 * (disk.UsedBytes / disk.TotalBytes),
                                }).ToList(),
                                LastSync = DateTime.UtcNow
                            };

                            n.SetReferences();
                            nodes.Add(n);
                        }
                    }
                }
            }

            return nodes;
        }

        private NodeStatus GetNodeStatus(string status)
        {
            switch (status) {
                case "Running":
                    return NodeStatus.Active;
                case "Pending":
                    return NodeStatus.Up;
                case "Stopped":
                case "Stopping":
                    return NodeStatus.Shutdown;
                case "Starting":
                    return NodeStatus.Up;
                case "Deleted":
                    return NodeStatus.Disabled;
                default:
                    return NodeStatus.Unknown;
            }
        }

        private class AliyunRequest : IDisposable
        {
            private readonly WebClient wc;
            private AliyunSettingNode setting;

            public AliyunRequest(AliyunSettingNode setting)
            {
                this.setting = setting;
                this.wc = GetWebClient();
            }

            public AliyunRequest()
            {
                this.wc = GetWebClient();
            }

            public void Set(AliyunSettingNode setting)
            {
                this.setting = setting;
            }

            private WebClient GetWebClient(string contentType = "application/json") =>
            new WebClient() {
                Headers =
                {
                    ["Content-Type"] = contentType
                },
                Encoding = Encoding.UTF8
            };

            private Uri JoinEscUrl(string accessKeyId, string accessKeySecret, Dictionary<string, string> otherParas, string apibaseUrl = ECSAPIBaseUrl)
            {
                Dictionary<string, string> imutableMap = new Dictionary<string, string>();
                imutableMap.Add("Version", "2014-05-26");
                imutableMap.Add("TimeStamp", DateTime.UtcNow.ToIos86());
                imutableMap.Add("SignatureMethod", "HMAC-SHA1");
                imutableMap.Add("SignatureVersion", "1.0");
                imutableMap.Add("SignatureNonce", Guid.NewGuid().ToString());
                imutableMap.Add("Format", "JSON");
                imutableMap.Add("AccessKeyId", accessKeyId);

                foreach (var para in otherParas) {
                    imutableMap[para.Key] = para.Value;
                }

                string strToSign = ComposeStringToSign("GET", null, imutableMap, null, null);
                string signature = CaclSignature(strToSign, accessKeySecret + "&");
                imutableMap.Add("Signature", signature);

                return new Uri(string.Format("{0}?{1}", apibaseUrl, ConcatQueryString(imutableMap)));
            }

            private Uri JoinCMUrl(
                string accessKeyId,
                string accessKeySecret,
                string metric,
                string dimensions,
                DateTime? startTime = null,
                DateTime? endTime = null,
                int? length = null)
            {
                return JoinEscUrl(
                            accessKeyId,
                            accessKeySecret,
                            new Dictionary<string, string>() {
                                { "Version","2015-10-20"},
                                { "RegionId","cn-hangzhou"},
                                { "Action","QueryMetricList"},
                                { "Project","acs_ecs"},
                                { "Metric",metric },
                                { "Period","300" },
                                { "Timestamp", DateTime.UtcNow.ToIos86()},
                                { "StartTime",startTime?.ToIos86() ?? DateTime.UtcNow.AddDays(-1).ToIos86()},
                                { "EndTime",endTime?.ToIos86() ?? DateTime.UtcNow.ToIos86()},
                                { "Dimensions",dimensions },
                                { "Length",(length ?? 1000).ToString()}
                            },
                            CMAPIBaseUrl);
            }

            private string CaclSignature(string source, string accessKeySecret)
            {
                using (var algorithm = KeyedHashAlgorithm.Create("HMACSHA1")) {
                    algorithm.Key = Encoding.UTF8.GetBytes(accessKeySecret.ToCharArray());
                    return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(source.ToCharArray())));
                }
            }

            private string UrlEncode(string value)
            {
                return string.IsNullOrEmpty(value)
                    ? string.Empty
                    : UpperCaseUrlEncode(value).Replace("+", "%20").Replace("*", "%2A").Replace("%7E", "~");
            }

            private string UpperCaseUrlEncode(string value)
            {
                char[] temp = HttpUtility.UrlEncode(value, Encoding.UTF8).ToCharArray();
                for (int i = 0; i < temp.Length - 2; i++) {
                    if (temp[i] == '%') {
                        temp[i + 1] = char.ToUpper(temp[i + 1]);
                        temp[i + 2] = char.ToUpper(temp[i + 2]);
                    }
                }
                return new string(temp);
            }

            private string ComposeStringToSign(string method, string uriPattern,
                Dictionary<string, string> queries, Dictionary<string, string> headers, Dictionary<string, string> paths)
            {
                var sortedDictionary = SortDictionary(queries);

                StringBuilder canonicalizedQueryString = new StringBuilder();
                foreach (var p in sortedDictionary) {
                    canonicalizedQueryString.Append("&")
                    .Append(UrlEncode(p.Key)).Append("=")
                    .Append(UrlEncode(p.Value));
                }

                StringBuilder stringToSign = new StringBuilder();
                stringToSign.Append(method.ToString());
                stringToSign.Append("&");
                stringToSign.Append(UrlEncode("/"));
                stringToSign.Append("&");
                stringToSign.Append(UrlEncode(
                        canonicalizedQueryString.ToString().Substring(1)));

                return stringToSign.ToString();
            }

            private static IDictionary<string, string> SortDictionary(Dictionary<string, string> dic)
            {
                IDictionary<string, string> sortedDictionary = new SortedDictionary<string, string>(dic, StringComparer.Ordinal);
                return sortedDictionary;
            }

            private string ConcatQueryString(Dictionary<string, string> parameters)
            {
                if (null == parameters) {
                    return null;
                }
                StringBuilder sb = new StringBuilder();

                foreach (var entry in parameters) {
                    String key = entry.Key;
                    String val = entry.Value;

                    sb.Append(UrlEncode(key));
                    if (val != null) {
                        sb.Append("=").Append(UrlEncode(val));
                    }
                    sb.Append("&");
                }

                int strIndex = sb.Length;
                if (parameters.Count > 0)
                    sb.Remove(strIndex - 1, 1);

                return sb.ToString();
            }

            public void Dispose()
            {
                this.wc.Dispose();
            }

            public async Task<AliyunRegion> GetRegion()
            {
                var regionApiUri = JoinEscUrl(
                        setting.AccessKeyId,
                        setting.AccessKeySecret,
                        new Dictionary<string, string>() {
                            { "Action","DescribeRegions" }
                        });

                var rawResult = await wc.DownloadStringTaskAsync(regionApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunRegion>(rawResult);
            }

            public async Task<AliyunInstance> GetInstance(string regionId)
            {
                var instancesApiUri = JoinEscUrl(
                            setting.AccessKeyId,
                            setting.AccessKeySecret,
                            new Dictionary<string, string>() {
                                    { "Action","DescribeInstances" },
                                    { "RegionId",regionId }
                            });

                var rawResult = await wc.DownloadStringTaskAsync(instancesApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunInstance>(rawResult);
            }

            public async Task<AliyunInstanceMonitorData> GetInstanceMonitorData(string instanceId)
            {
                var instanceMonitorDataApiUri = JoinEscUrl(
                                setting.AccessKeyId,
                                setting.AccessKeySecret,
                                new Dictionary<string, string>() {
                                    { "Action","DescribeInstanceMonitorData"},
                                    { "InstanceId",instanceId},
                                    { "StartTime",DateTime.UtcNow.AddMinutes(-2).ToIos86()},
                                    { "EndTime",DateTime.UtcNow.ToIos86()},
                                    { "Period","60"}
                                });

                var rawResult = await wc.DownloadStringTaskAsync(instanceMonitorDataApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunInstanceMonitorData>(rawResult);
            }

            public async Task<AliyunDisks> GetDisks(string regionId, string instanceId)
            {
                var disksApiUri = JoinEscUrl(
                                setting.AccessKeyId,
                                setting.AccessKeySecret,
                                new Dictionary<string, string>() {
                                    { "Action","DescribeDisks"},
                                    { "RegionId",regionId}
                                });

                var rawResult = await wc.DownloadStringTaskAsync(disksApiUri).ConfigureAwait(false);
                var disks = Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunDisks>(rawResult);

                foreach (var d in disks.Disks.Disk) {
                    decimal usedBytes = 0;
                    foreach (var mountpoint in new string[] { "C:", "D:", "E:", "F:", "G:" }) {
                        var value = ((await GetDiskUtilization(instanceId, mountpoint)).Datapoints.FirstOrDefault()?.Average ?? 0);
                        usedBytes += Convert.ToDecimal(value) / 100 * d.TotalBytes;
                    }

                    d.UsedBytes = usedBytes;
                }

                return disks;
            }

            public async Task<AliyunMetric> GetMemoryUtilization(string instanceId, DateTime? startTime = null, DateTime? endTime = null)
            {
                var memoryCloudMonitorApiUri = JoinCMUrl(
                                setting.AccessKeyId,
                                setting.AccessKeySecret,
                                "vm.MemoryUtilization",
                                "{'instanceId':'" + instanceId + "'}",
                                startTime,
                                endTime);

                var rawResult = await wc.DownloadStringTaskAsync(memoryCloudMonitorApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunMetric>(rawResult);
            }

            public async Task<AliyunMetric> GetCPUUtilization(string instanceId, DateTime? startTime = null, DateTime? endTime = null)
            {
                var monitorApiUri = JoinCMUrl(
                                setting.AccessKeyId,
                                setting.AccessKeySecret,
                                "CPUUtilization",
                                "{'instanceId':'" + instanceId + "'}",
                                startTime,
                                endTime);

                var rawResult = await wc.DownloadStringTaskAsync(monitorApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunMetric>(rawResult);
            }

            public async Task<AliyunMetric> GetInternetInRateNew(string instanceId, DateTime? startTime = null, DateTime? endTime = null, int? length = null)
            {
                var monitorApiUri = JoinCMUrl(
                                setting.AccessKeyId,
                                setting.AccessKeySecret,
                                "InternetInNew",
                                "{'instanceId':'" + instanceId + "'}",
                                startTime,
                                endTime,
                                length);

                var rawResult = await wc.DownloadStringTaskAsync(monitorApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunMetric>(rawResult);
            }

            public async Task<AliyunMetric> GetIntranetOutRateNew(string instanceId, DateTime? startTime = null, DateTime? endTime = null, int? length = null)
            {
                var monitorApiUri = JoinCMUrl(
                                setting.AccessKeyId,
                                setting.AccessKeySecret,
                                "InternetOutNew",
                                "{'instanceId':'" + instanceId + "'}",
                                startTime,
                                endTime,
                                length);

                var rawResult = await wc.DownloadStringTaskAsync(monitorApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunMetric>(rawResult);
            }

            public async Task<AliyunMetric> GetDiskUtilization(string instanceId, string mountpoint)
            {
                var diskCloudMonitorApiUri = JoinCMUrl(
                                setting.AccessKeyId,
                                setting.AccessKeySecret,
                                "vm.DiskUtilization",
                                "{\"instanceId\":\"" + instanceId + "\",\"mountpoint\":\"" + mountpoint + "\"}");

                var rawResult = await wc.DownloadStringTaskAsync(diskCloudMonitorApiUri).ConfigureAwait(false);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AliyunMetric>(rawResult);
            }

            public class AliyunRegion
            {
                public string RequestId { get; set; }

                public RegionsList Regions { get; set; }

                public class RegionsList
                {
                    public IEnumerable<RegionsId> Region { get; set; }
                }

                public class RegionsId
                {
                    public string RegionId { get; set; }

                    public string LocalName { get; set; }
                }
            }

            public class AliyunInstance
            {
                public InstanceList Instances { get; set; }

                public class InstanceList
                {
                    public IEnumerable<Instance> Instance { get; set; }
                }

                public class Instance
                {
                    public string InstanceId { get; set; }

                    public string InstanceName { get; set; }

                    public string Description { get; set; }

                    public int CPU { get; set; }

                    public int Memory { get; set; }

                    public float TotalMemory
                    {
                        get { return ((float)Memory) * 1024 * 1024; }
                    }

                    public string HostName { get; set; }

                    public int InternetMaxBandwidthIn { get; set; }

                    public int InternetMaxBandwidthOut { get; set; }

                    public string CreationTime { get; set; }

                    public string Status { get; set; }

                    public string InstanceType { get; set; }

                    public string InstanceNetworkType { get; set; }

                    public string ImageId { get; set; }

                    public PublicIpAddress PublicIpAddress { get; set; }
                }

                public class PublicIpAddress
                {
                    public IEnumerable<string> IpAddress { get; set; }
                }
            }

            public class AliyunInstanceMonitorData
            {
                public string RequestId { get; set; }

                public MonitorDataList MonitorData { get; set; }

                public class MonitorDataList
                {
                    public IEnumerable<InstanceMonitorData> InstanceMonitorData { get; set; }
                }

                public class InstanceMonitorData
                {
                    /// <summary>
                    /// CPU的使用比例，单位：百分比（%）
                    /// </summary>
                    public int CPU { get; set; }

                    /// <summary>
                    /// 云服务器实例接收到的数据流量，单位：kbits
                    /// </summary>
                    public int InternetRX { get; set; }

                    /// <summary>
                    /// 云服务器实例接发送的数据流量，单位：kbits
                    /// </summary>
                    public int InternetTX { get; set; }

                    /// <summary>
                    /// 云服务器实例的带宽（单位时间内的网络流量），单位为kbits/s
                    /// </summary>
                    public int InternetBandwidth { get; set; }

                    /// <summary>
                    /// 系统盘IO读操作，单位：次/s
                    /// </summary>
                    public int IOPSRead { get; set; }

                    /// <summary>
                    /// 系统盘IO写操作，单位：次/s
                    /// </summary>
                    public int IOPSWrite { get; set; }

                    /// <summary>
                    /// 系统盘磁盘读带宽，单位：Byte/s
                    /// </summary>
                    public int BPSRead { get; set; }

                    /// <summary>
                    /// 系统盘磁盘写带宽，单位：Byte/s
                    /// </summary>
                    public int BPSWrite { get; set; }
                }
            }

            public class AliyunMetric
            {
                public string Code { get; set; }

                public string Msg { get; set; }

                public bool Success { get; set; }

                public int Size { get; set; }

                public string RequestId { get; set; }

                public IEnumerable<Datapoint> Datapoints { get; set; }

                public string Cursor { get; set; }
            }


            public class Datapoint
            {
                private long _timestamp { get; set; }

                public double Sum { get; set; }

                public long timestamp
                {
                    get { return _timestamp; }
                    set { _timestamp = value / 1000; }
                }

                public double Maximum { get; set; }

                public double Minimum { get; set; }

                public double? value { get; set; }

                public double Average { get; set; }

                public int SampleCount { get; set; }
            }

            public class AliyunDisks
            {
                public string RegionId { get; set; }

                public DisksList Disks { get; set; }

                public class DisksList
                {
                    public IEnumerable<Disk> Disk { get; set; }
                }

                public class Disk
                {
                    public string DiskId { get; set; }

                    public string DiskName { get; set; }

                    public string Description { get; set; }

                    /// <summary>
                    /// 磁盘类型 可选值：system: 系统盘data: 数据盘
                    /// </summary>
                    public string Type { get; set; }

                    /// <summary>
                    /// 磁盘种类可选值：cloud: 普通云盘cloud_efficiency：高效云盘cloud_ssd：SSD云盘ephemeral_ssd: 本地 SSD 盘ephemeral: 本地磁盘
                    /// </summary>
                    public string Category { get; set; }

                    public string Device { get; set; }

                    /// <summary>
                    /// GB
                    /// </summary>
                    public decimal Size { get; set; }

                    public decimal TotalBytes
                    {
                        get
                        {
                            return Size * 1024 * 1024 * 1024;
                        }
                    }

                    public decimal UsedBytes { get; set; }

                    /// <summary>
                    /// 磁盘状态 In_use | Available | Attaching | Detaching | Creating | ReIniting
                    /// </summary>
                    public string Status { get; set; }
                }
            }
        }
    }
}

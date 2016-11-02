using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver
{
    public class AliyunSettings : IProviderSettings
    {
        public bool Enabled => Nodes.Any();

        public string Name => "Aliyun";

        public List<AliyunSettingNode> Nodes { get; set; }

        public void Normalize()
        { }
    }

    public class AliyunSettingNode
    {
        public string AccessKeyId { get; set; }

        public string AccessKeySecret { get; set; }
    }
}

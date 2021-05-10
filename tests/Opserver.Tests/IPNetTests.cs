using System.Net;
using Opserver.Data;
using Xunit;
using Xunit.Abstractions;

namespace Opserver.Tests
{
    public class IPNetTests
    {
        private ITestOutputHelper log { get; }
        public IPNetTests(ITestOutputHelper output) => log = output;

        // TODO: IPV6 tests
        [Theory]
        [InlineData("127.1", true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.1", true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.0.1", true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.1/32", true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.1/32", true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.0.1/32", true, "127.0.0.1", "127.0.0.1", true)]

        [InlineData("127.0.0.1/0", true, "0.0.0.0", "255.255.255.255", false)]
        [InlineData("127.0.0.1/33", false, null, null, true)]
        [InlineData("127.0.0.1/64", false, null, null, true)]

        [InlineData("1.1.1.1/16", true, "1.1.0.0", "1.1.255.255", false)]
        [InlineData("1.1.0.0/16", true, "1.1.0.0", "1.1.255.255", false)]
        [InlineData("1.1.0.0/23", true, "1.1.0.0", "1.1.1.255", false)]
        [InlineData("1.1.1.1/24", true, "1.1.1.0", "1.1.1.255", false)]
        [InlineData("1.1.1.1/31", true, "1.1.1.0", "1.1.1.1", false)]

        [InlineData("::1", true, "::1", "::1", true)]
        [InlineData("::2", true, "::2", "::2", false)]
        [InlineData("::ffff:ffff", true, "::ffff:ffff", "::ffff:ffff", false)]
        [InlineData("::0/32", true, "::0", "0:0:ffff:ffff:ffff:ffff:ffff:ffff", false)]
        [InlineData("::0/32", true, "0:0:0:0:0:0:0:0", "0:0:ffff:ffff:ffff:ffff:ffff:ffff", false)]
        [InlineData("::0/64", true, "::0", "::ffff:ffff:ffff:ffff", false)]
        [InlineData("::0/96", true, "::0", "0:0:0:0:0:0:ffff:ffff", false)]
        [InlineData("::0/96", true, "0:0:0:0:0:0:0:0", "0:0:0:0:0:0:ffff:ffff", false)]

        [InlineData("0.0.0.0/0", true, "0.0.0.0", "255.255.255.255", false)]
        [InlineData("0.0.0.0/32", true, "0.0.0.0", "0.0.0.0", false)]
        [InlineData("localhost", false, null, null, false)]
        [InlineData("", false, null, null, false)]
        [InlineData(null, false, null, null, false)]
        public void TryParse(string input, bool success, string firstIpString, string lastIpString, bool isPrivate)
        {
            var parsed = IPNet.TryParse(input, out var ipNet);
            Assert.Equal(success, parsed);
            if (parsed)
            {
                var firstIp = IPAddress.Parse(firstIpString);
                var lastIp = IPAddress.Parse(lastIpString);
                log.WriteLine("Subnet: " + ipNet.Subnet);
                Assert.Equal(firstIp, ipNet.FirstAddressInSubnet);
                Assert.Equal(lastIp, ipNet.LastAddressInSubnet);
                Assert.Equal(isPrivate, ipNet.IsPrivate);
            }
        }

        [Theory]
        [InlineData("127.1", 32, true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.1", 32, true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.0.1", 32, true, "127.0.0.1", "127.0.0.1", true)]

        [InlineData("127.0.0.1", 0, true, "0.0.0.0", "255.255.255.255", false)]
        [InlineData("127.0.0.1", 33, false, null, null, true)]
        [InlineData("127.0.0.1", 64, false, null, null, true)]

        [InlineData("1.1.1.1", 16, true, "1.1.0.0", "1.1.255.255", false)]
        [InlineData("1.1.0.0", 16, true, "1.1.0.0", "1.1.255.255", false)]
        [InlineData("1.1.0.0", 23, true, "1.1.0.0", "1.1.1.255", false)]
        [InlineData("1.1.1.1", 24, true, "1.1.1.0", "1.1.1.255", false)]
        [InlineData("1.1.1.1", 31, true, "1.1.1.0", "1.1.1.1", false)]

        [InlineData("::1", 128, true, "::1", "::1", true)]
        [InlineData("::2", 128, true, "::2", "::2", false)]
        [InlineData("::ffff:ffff", 128, true, "::ffff:ffff", "::ffff:ffff", false)]
        [InlineData("::0", 32, true, "::0", "0:0:ffff:ffff:ffff:ffff:ffff:ffff", false)]
        [InlineData("::0", 32, true, "0:0:0:0:0:0:0:0", "0:0:ffff:ffff:ffff:ffff:ffff:ffff", false)]
        [InlineData("::0", 64, true, "::0", "::ffff:ffff:ffff:ffff", false)]
        [InlineData("::0", 96, true, "::0", "0:0:0:0:0:0:ffff:ffff", false)]
        [InlineData("::0", 96, true, "0:0:0:0:0:0:0:0", "0:0:0:0:0:0:ffff:ffff", false)]

        [InlineData("0.0.0.0", 0, true, "0.0.0.0", "255.255.255.255", false)]
        [InlineData("0.0.0.0", 32, true, "0.0.0.0", "0.0.0.0", false)]
        [InlineData("localhost", 32, false, null, null, false)]
        [InlineData("", 32, false, null, null, false)]
        [InlineData(null, 32, false, null, null, false)]
        public void TryParseCidrArg(string input, byte cidr, bool success, string firstIpString, string lastIpString, bool isPrivate)
        {
            var parsed = IPNet.TryParse(input, cidr, out var ipNet);
            Assert.Equal(success, parsed);
            if (parsed)
            {
                var firstIp = IPAddress.Parse(firstIpString);
                var lastIp = IPAddress.Parse(lastIpString);
                Assert.Equal(firstIp, ipNet.FirstAddressInSubnet);
                Assert.Equal(lastIp, ipNet.LastAddressInSubnet);
                Assert.Equal(isPrivate, ipNet.IsPrivate);
            }
        }

        [Theory]
        [InlineData("127.1", "255.255.255.255", true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.1", "255.255.255.255", true, "127.0.0.1", "127.0.0.1", true)]
        [InlineData("127.0.0.1", "255.255.255.255", true, "127.0.0.1", "127.0.0.1", true)]

        [InlineData("127.0.0.1", "0.0.0.0", true, "0.0.0.0", "255.255.255.255", false)]
        [InlineData("127.0.0.1", "255.255.255.256", false, null, null, true)]

        [InlineData("1.1.1.1", "255.255.0.0", true, "1.1.0.0", "1.1.255.255", false)]
        [InlineData("1.1.0.0", "255.255.0.0", true, "1.1.0.0", "1.1.255.255", false)]
        [InlineData("1.1.0.0", "255.255.254.0", true, "1.1.0.0", "1.1.1.255", false)]
        [InlineData("1.1.1.1", "255.255.255.0", true, "1.1.1.0", "1.1.1.255", false)]
        [InlineData("1.1.1.1", "255.255.255.254", true, "1.1.1.0", "1.1.1.1", false)]

        [InlineData("0.0.0.0", "0.0.0.0", true, "0.0.0.0", "255.255.255.255", false)]
        [InlineData("0.0.0.0", "255.255.255.255", true, "0.0.0.0", "0.0.0.0", false)]
        [InlineData("localhost", "255.255.255.255", false, null, null, false)]
        [InlineData("", "255.255.255.255", false, null, null, false)]
        [InlineData(null, "255.255.255.255", false, null, null, false)]
        public void TryParseSubnetArg(string input, string subnet, bool success, string firstIpString, string lastIpString, bool isPrivate)
        {
            var parsed = IPNet.TryParse(input, subnet, out var ipNet);
            Assert.Equal(success, parsed);
            if (parsed)
            {
                var firstIp = IPAddress.Parse(firstIpString);
                var lastIp = IPAddress.Parse(lastIpString);
                Assert.Equal(firstIp, ipNet.FirstAddressInSubnet);
                Assert.Equal(lastIp, ipNet.LastAddressInSubnet);
                Assert.Equal(isPrivate, ipNet.IsPrivate);
            }
        }

        [Theory]
        [InlineData("127.0.0.0/16", "127.0.0.1", true)]
        [InlineData("127.0.0.0/16", "127.1.0.1", false)]
        [InlineData("::1", "::1", true)]
        [InlineData("::2", "::2", true)]
        [InlineData("::1", "::2", false)]
        // TODO: MOAR CASES!
        public void ContainsNet(string inputStr, string childInputStr, bool shouldContain)
        {
            var ipNet = IPNet.Parse(inputStr);
            var childNet = IPNet.Parse(childInputStr);
            Assert.Equal(shouldContain, ipNet.Contains(childNet));
        }

        [Theory]
        [InlineData("127.0.0.0/16", "127.0.0.1", true)]
        [InlineData("127.0.0.0/16", "127.1.0.1", false)]
        [InlineData("::1", "::1", true)]
        [InlineData("::2", "::2", true)]
        [InlineData("::1", "::2", false)]
        // TODO: MOAR CASES!
        public void ContainsIP(string inputStr, string childInputStr, bool shouldContain)
        {
            var ipNet = IPNet.Parse(inputStr);
            var childNet = IPAddress.Parse(childInputStr);
            Assert.Equal(shouldContain, ipNet.Contains(childNet));
        }
    }
}

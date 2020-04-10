using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace Opserver.Data
{
    public class IPNet
    {
        private static IPAddress[] KnownIPv4Subsets { get; } = CalcSubnets(AddressFamily.InterNetwork);
        private static IPAddress[] KnownIPv6Subsets { get; } = CalcSubnets(AddressFamily.InterNetworkV6);

        private static IPAddress[] CalcSubnets(AddressFamily family)
        {
            var bitLength = GetBitLength(family);
            var subnets = new IPAddress[bitLength + 1];
#if NETCOREAPP
            Span<byte> ipByteArray = stackalloc byte[bitLength / 8];
#else
            var ipByteArray = new byte[bitLength / 8];
#endif
            subnets[0] = new IPAddress(ipByteArray);
            // Loop through and set bits left to right (shifting the high bit per iteration)
            for (byte position = 0; position < bitLength; position++)
            {
                ipByteArray[Math.DivRem(position, 8, out var remainder)] |= (byte)(0x80 >> remainder);
                subnets[position + 1] = new IPAddress(ipByteArray);
            }
            return subnets;
        }

        public IPAddress IPAddress { get; }
        public IPAddress Subnet { get; }
        public byte CIDR { get; }
        public AddressFamily AddressFamily => IPAddress.AddressFamily;

        public string AddressFamilyDescription =>
            IPAddress.AddressFamily == AddressFamily.InterNetwork
                ? "IPv4"
                : IPAddress.AddressFamily == AddressFamily.InterNetworkV6
                    ? "IPv6"
                    : "";

        private TinyIPAddress? _tinyIPAddress;
        internal TinyIPAddress TIPAddress =>
            (_tinyIPAddress ??= TinyIPAddress.FromIPAddress(IPAddress)).Value;

        private TinyIPAddress? _tinySubnet;
        internal TinyIPAddress TSubnet =>
            (_tinySubnet ??= TinyIPAddress.FromIPAddress(Subnet ?? IPAddress)).Value;

        public IPAddress FirstAddressInSubnet => TinyFirstAddressInSubnet.ToIPAddress();
        public IPAddress LastAddressInSubnet => TinyLastAddressInSubnet.ToIPAddress();
        public IPAddress Broadcast => AddressFamily == AddressFamily.InterNetwork ? TinyBroadcast.ToIPAddress() : null;

        private TinyIPAddress TinyFirstAddressInSubnet => TIPAddress & TSubnet;
        private TinyIPAddress TinyLastAddressInSubnet => Subnet == null ? TIPAddress : (TIPAddress | ~TSubnet);
        private TinyIPAddress TinyBroadcast => Subnet == null ? TIPAddress : (TIPAddress | ~TSubnet);

        public bool IsPrivate => IsPrivateNetwork(this);
        public bool IsMulticast => IsMulticastNetwork(this);
        public bool IsLinkLocal => IsLinkLocalNetwork(this);
        public bool IsDocumentation => IsDocumentationNetwork(this);

        public override string ToString() => IPAddress + "/" + CIDR.ToString();

        public bool Contains(IPAddress ip)
        {
            if (AddressFamily != ip.AddressFamily)
            {
                return false;
            }
            var tip = TinyIPAddress.FromIPAddress(ip);
            return tip.HasValue && Contains(tip.Value);
        }

        public bool Contains(IPNet network) =>
            network != null
            && AddressFamily == network.AddressFamily
            && TinyFirstAddressInSubnet <= network.TinyFirstAddressInSubnet
            && network.TinyLastAddressInSubnet <= TinyLastAddressInSubnet;

        private bool Contains(TinyIPAddress tip) =>
            AddressFamily == tip.AddressFamily && (TSubnet & TIPAddress) == (TSubnet & tip);

        public static bool IsPrivateNetwork(IPNet network) =>
            ReservedPrivateRanges.Any(r => r.Contains(network));

        public static bool IsMulticastNetwork(IPNet network) =>
            ReservedMulticastRanges.Any(r => r.Contains(network));

        public static bool IsLinkLocalNetwork(IPNet network) =>
            ReservedLinkLocalRanges.Any(r => r.Contains(network));

        public static bool IsDocumentationNetwork(IPNet network) =>
            ReservedDocumentationRanges.Any(r => r.Contains(network));

        public IPNet(IPAddress ip, byte? cidr = null) : this(ip, null, cidr, false) { }

        public IPNet(IPAddress ip, IPAddress subnet, byte? cidr = null) : this(ip, subnet, cidr, false) { }

        private IPNet(IPAddress ip, IPAddress subnet, byte? cidr, bool subnetKnownValid = false)
        {
            IPAddress = ip;
            Subnet = subnet;
            if (!subnetKnownValid && subnet != null && !TSubnet.IsValidSubnet)
            {
                throw new IPNetParseException("Error: subnet mask '{0}' is not a valid subnet", subnet);
            }
            CIDR = cidr ?? (byte)(subnet == null ? GetBitLength(ip.AddressFamily) : TSubnet.NumberOfSetBits);
        }

        public static bool TryParse(string ipOrCidr, out IPNet net)
        {
            try
            {
                net = Parse(ipOrCidr);
                return true;
            }
            catch
            {
                net = null;
                return false;
            }
        }

#if !NETCOREAPP
        private static readonly char[] _forwardSlash = new[] { '/' };
#endif

        public static IPNet Parse(string ipOrCidr)
        {
#if NETCOREAPP
            ReadOnlySpan<char> span = ipOrCidr;
            var slashPos = span.IndexOf('/');
            if (slashPos > -1)
            {
                if (byte.TryParse(span.Slice(slashPos + 1), out var cidr))
                {
                    return Parse(span.Slice(0, slashPos), cidr);
                }
                throw new IPNetParseException("Error parsing CIDR from IP: '" + ipOrCidr + "'");
            }
#else
            var parts = ipOrCidr.Split(_forwardSlash);
            if (parts.Length == 2)
            {
                if (byte.TryParse(parts[1], out var cidr))
                {
                    return Parse(parts[0], cidr);
                }
                throw new IPNetParseException("Error parsing CIDR from IP: '{0}'", parts[1]);
            }
#endif
            if (IPAddress.TryParse(ipOrCidr, out var ip))
            {
                return new IPNet(ip, null, null);
            }
            throw new IPNetParseException("Error parsing IP Address from IP: '{0}'", ipOrCidr);
        }

        public static bool TryParse(string ip, byte cidr, out IPNet net)
        {
            try
            {
                net = Parse(ip, cidr);
                return true;
            }
            catch
            {
                net = null;
                return false;
            }
        }

        public static IPNet Parse(string ip, byte cidr)
        {
            if (IPAddress.TryParse(ip, out var ipAddr))
            {
                var subnet = IPAddressFromCIDR(ipAddr.AddressFamily, cidr);
                return new IPNet(ipAddr, subnet, cidr);
            }
            throw new IPNetParseException("Error parsing IP Address from IP: '{0}'", ip);
        }
#if NETCOREAPP
        public static IPNet Parse(ReadOnlySpan<char> ip, byte cidr)
        {
            if (IPAddress.TryParse(ip, out var ipAddr))
            {
                var subnet = IPAddressFromCIDR(ipAddr.AddressFamily, cidr);
                return new IPNet(ipAddr, subnet, cidr, subnetKnownValid: true);
            }
            throw new IPNetParseException("Error parsing IP Address from IP: '" + ip.ToString() + "'");
        }
#endif

        public static bool TryParse(string ip, string subnet, out IPNet net)
        {
            try
            {
                net = Parse(ip, subnet);
                return true;
            }
            catch
            {
                net = null;
                return false;
            }
        }

        public static IPNet Parse(string ip, string subnet)
        {
            if (IPAddress.TryParse(ip, out var ipAddr))
            {
                if (IPAddress.TryParse(subnet, out var subnetAddr))
                {
                    if (!TinyIPAddress.FromIPAddress(subnetAddr).Value.IsValidSubnet)
                    {
                        throw new IPNetParseException("Error parsing subnet mask Address from IP: '" + subnet + "' is not a valid subnet");
                    }
                    return new IPNet(ipAddr, subnetAddr, null, subnetKnownValid: true);
                }
                throw new IPNetParseException("Error parsing subnet mask from IP: '" + subnet + "'");
            }
            throw new IPNetParseException("Error parsing IP Address from IP: '" + ip + "'");
        }

        public static IPAddress ToNetmask(AddressFamily addressFamily, int cidr) =>
            IPAddressFromCIDR(addressFamily, cidr);

        private static int GetBitLength(AddressFamily family) =>
            family switch
            {
                AddressFamily.InterNetwork => 32,
                AddressFamily.InterNetworkV6 => 128,
                _ => throw new ArgumentOutOfRangeException(nameof(family), "You're probably from the future, they added more IPs, fix me."),
            };

        // This is a much faster version thanks to Marc Gravell
        private static IPAddress IPAddressFromCIDR(AddressFamily family, int cidr)
        {
            switch (family)
            {
                case AddressFamily.InterNetwork:
                    if (cidr <= KnownIPv4Subsets.Length)
                    {
                        return KnownIPv4Subsets[cidr];
                    }
                    break;
                case AddressFamily.InterNetworkV6:
                    if (cidr <= KnownIPv6Subsets.Length)
                    {
                        return KnownIPv6Subsets[cidr];
                    }
                    break;
            }
            throw new Exception("Invalid subnet CIDR length: " + cidr);
        }

        /// <summary>
        /// Private IP Ranges reserved for internal use by ARIN
        /// These networks should not route on the global Internet
        /// </summary>
        private static readonly List<IPNet> ReservedPrivateRanges = new List<IPNet>
            {
                Parse("10.0.0.0/8"),
                Parse("127.0.0.0/8"),
                Parse("100.64.0.0/10"),
                Parse("172.16.0.0/12"),
                Parse("192.0.0.0/24"),
                Parse("192.168.0.0/16"),
                Parse("198.18.0.0/15"),
                Parse("::1/128"),
                Parse("fc00::/7"),
            };

        /// <summary>
        /// Multicast IP Ranges reserved for use by ARIN
        /// </summary>
        private static readonly List<IPNet> ReservedMulticastRanges = new List<IPNet>
            {
                Parse("224.0.0.0/4"),
                Parse("ff00::/8"),
            };

        /// <summary>
        /// Link-local IP Ranges reserved for use by ARIN
        /// </summary>
        private static readonly List<IPNet> ReservedLinkLocalRanges = new List<IPNet>
            {
                Parse("169.254.0.0/16"),
                Parse("fe80::/10"),
            };


        /// <summary>
        /// Documentation IP Ranges reserved for use by ARIN
        /// </summary>
        private static readonly List<IPNet> ReservedDocumentationRanges = new List<IPNet>
            {
                Parse("192.0.2.0/24"),    // TEST-NET-1
                Parse("198.51.100.0/24"), // TEST-NET-2
                Parse("203.0.113.0/24"),  // TEST-NET-3
                Parse("2001:db8::/32"),
            };

        public class IPNetParseException : Exception
        {
            public IPNetParseException() { }
            public IPNetParseException(string message) : base(message) { }
            public IPNetParseException(string message, params object[] format) : base(string.Format(message, format)) { }
            public IPNetParseException(string message, Exception innerException) : base(message, innerException) { }
        }

        [DataContract]
        public struct TinyIPAddress : IEquatable<TinyIPAddress>, IComparable<TinyIPAddress>
        {
            [DataMember(Order = 1)]
            private readonly bool IsV4;
            [DataMember(Order = 2)]
            private readonly bool IsV6;
            [DataMember(Order = 3)]
            private readonly uint IPv4Address;
            [DataMember(Order = 4)]
            private readonly ulong FirstV6Leg;
            [DataMember(Order = 5)]
            private readonly ulong LastV6Leg;

            public AddressFamily AddressFamily => IsV4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;

            public string BitString
            {
                get
                {
                    StringBuilder sb;
                    if (IsV4)
                    {
                        sb = StringBuilderCache.Get(32);
                        for (var i = 0; i < 32; i++)
                        {
                            sb.Append((IPv4Address >> (31 - i)) & 1);
                        }
                    }
                    else
                    {
                        sb = StringBuilderCache.Get(128);
                        for (var i = 0; i < 64; i++)
                        {
                            sb.Append((FirstV6Leg >> (63 - i)) & 1);
                        }
                        for (var i = 0; i < 64; i++)
                        {
                            sb.Append((LastV6Leg >> (63 - i)) & 1);
                        }
                    }
                    return sb.ToStringRecycle();
                }
            }

            public IPAddress ToIPAddress()
            {
                if (IsV4)
                {
                    return new IPAddress(new[] {
                        (byte)(IPv4Address >> 24),
                        (byte)(IPv4Address >> 16),
                        (byte)(IPv4Address >> 8),
                        (byte)(IPv4Address)
                    });
                }
                return new IPAddress(new[] {
                    (byte)(FirstV6Leg >> 56),
                    (byte)(FirstV6Leg >> 48),
                    (byte)(FirstV6Leg >> 40),
                    (byte)(FirstV6Leg >> 32),
                    (byte)(FirstV6Leg >> 24),
                    (byte)(FirstV6Leg >> 16),
                    (byte)(FirstV6Leg >> 8),
                    (byte)(FirstV6Leg),
                    (byte)(LastV6Leg >> 56),
                    (byte)(LastV6Leg >> 48),
                    (byte)(LastV6Leg >> 40),
                    (byte)(LastV6Leg >> 32),
                    (byte)(LastV6Leg >> 24),
                    (byte)(LastV6Leg >> 16),
                    (byte)(LastV6Leg >> 8),
                    (byte)(LastV6Leg)
                });
            }

            public TinyIPAddress(uint ipv4Address)
            {
                IsV4 = true;
                IsV6 = false;
                IPv4Address = ipv4Address;
                FirstV6Leg = 0;
                LastV6Leg = 0;
            }

            public TinyIPAddress(ulong ipv6FirstLeg, ulong ipv6LastLeg)
            {
                IsV4 = false;
                IsV6 = true;
                IPv4Address = 0;
                FirstV6Leg = ipv6FirstLeg;
                LastV6Leg = ipv6LastLeg;
            }

            public static TinyIPAddress? FromIPAddress(IPAddress addr)
            {
#if NETCOREAPP
                Span<byte> source = stackalloc byte[GetBitLength(addr.AddressFamily) / 8];
                addr.TryWriteBytes(source, out var len);
#else
                var source = addr.GetAddressBytes();
                var len = source.Length;
#endif
                if (len == 4)
                {
                    return new TinyIPAddress(((uint)source[0] << 24)
                                           | ((uint)source[1] << 16)
                                           | ((uint)source[2] << 8)
                                           | ((uint)source[3]));
                }
                if (len == 16)
                {
#if NETCOREAPP
                    return new TinyIPAddress(FromBytes(source), FromBytes(source.Slice(8)));
#else
                    return new TinyIPAddress(FromBytes(source, 0), FromBytes(source, 0));
#endif
                }
                return null;
            }

#if NETCOREAPP
            private static ulong FromBytes(ReadOnlySpan<byte> source)
            {
                return ((ulong)source[0] << 56)
                      | ((ulong)source[1] << 48)
                      | ((ulong)source[2] << 40)
                      | ((ulong)source[3] << 32)
                      | ((ulong)source[4] << 24)
                      | ((ulong)source[5] << 16)
                      | ((ulong)source[6] << 8)
                      | ((ulong)source[7]);
            }
#endif

            private static ulong FromBytes(byte[] source, int start)
            {
                return ((ulong)source[start++] << 56)
                      | ((ulong)source[start++] << 48)
                      | ((ulong)source[start++] << 40)
                      | ((ulong)source[start++] << 32)
                      | ((ulong)source[start++] << 24)
                      | ((ulong)source[start++] << 16)
                      | ((ulong)source[start++] << 8)
                      | ((ulong)source[start]);
            }

            public int NumberOfSetBits
            {
                get
                {
                    if (IsV4)
                    {
                        return NumberOfSetBitsImpl(IPv4Address);
                    }
                    if (IsV6)
                    {
                        return NumberOfSetBitsImpl(FirstV6Leg) + NumberOfSetBitsImpl(LastV6Leg);
                    }
                    return 0;
                }
            }

            ///<summary>
            /// Gets the number of bits set in a <see cref="uint"/>, taken from 
            /// https://stackoverflow.com/questions/109023/how-to-count-the-number-of-set-bits-in-a-32-bit-integer
            /// </summary>
            /// <param name="i">The value to check</param>
            private static int NumberOfSetBitsImpl(uint i)
            {
                i -= (i >> 1) & 0x55555555U;
                i = (i & 0x33333333U) + ((i >> 2) & 0x33333333U);
                return (int)((((i + (i >> 4)) & 0x0F0F0F0FU) * 0x01010101U) >> 24);
            }

            ///<summary>
            /// Gets the number of bits set in a <see cref="ulong"/>, taken from 
            /// https://stackoverflow.com/questions/2709430/count-number-of-bits-in-a-64-bit-long-big-integer
            /// </summary>
            /// <param name="i">The value to check</param>
            private static int NumberOfSetBitsImpl(ulong i)
            {
                i -= (i >> 1) & 0x5555555555555555UL;
                i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
                return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
            }

            public bool IsValidSubnet
            {
                get
                {
                    if (IsV4)
                    {
                        return IsValidSubnetImpl(IPv4Address);
                    }
                    if (IsV6)
                    {
                        return (FirstV6Leg == ulong.MaxValue && IsValidSubnetImpl(LastV6Leg))
                           || (LastV6Leg == ulong.MinValue && IsValidSubnetImpl(FirstV6Leg));
                    }
                    return false;
                }
            }

            private static bool IsValidSubnetImpl(uint i)
            {
                bool inZeros = false;
                for (var j = 31; j >= 0; j--)
                {
                    if (((i >> j) & 1) != 1)
                    {
                        inZeros = true;
                    }
                    else if (inZeros)
                    {
                        return false;
                    }
                }
                return true;
            }

            private static bool IsValidSubnetImpl(ulong l)
            {
                bool inZeros = false;
                for (var j = 63; j >= 0; j--)
                {
                    if (((l >> j) & 1) != 1)
                    {
                        inZeros = true;
                    }
                    else if (inZeros)
                    {
                        return false;
                    }
                }
                return true;
            }

            public static TinyIPAddress operator &(TinyIPAddress a, TinyIPAddress b) =>
                a.IsV4 && b.IsV4
                    ? new TinyIPAddress(a.IPv4Address & b.IPv4Address)
                    : new TinyIPAddress(a.FirstV6Leg & b.FirstV6Leg, a.LastV6Leg & b.LastV6Leg);

            public static TinyIPAddress operator |(TinyIPAddress a, TinyIPAddress b) =>
                a.IsV4 && b.IsV4
                    ? new TinyIPAddress(a.IPv4Address | b.IPv4Address)
                    : new TinyIPAddress(a.FirstV6Leg | b.FirstV6Leg, a.LastV6Leg | b.LastV6Leg);

            public static TinyIPAddress operator +(TinyIPAddress a, TinyIPAddress b) =>
                a.IsV4 && b.IsV4
                    ? new TinyIPAddress(a.IPv4Address + b.IPv4Address)
                    : new TinyIPAddress(a.FirstV6Leg + b.FirstV6Leg, a.LastV6Leg + b.LastV6Leg);

            public static TinyIPAddress operator ~(TinyIPAddress a) =>
                a.IsV4
                    ? new TinyIPAddress(~a.IPv4Address)
                    : new TinyIPAddress(~a.FirstV6Leg, ~a.LastV6Leg);

            public static bool operator ==(TinyIPAddress a, TinyIPAddress b) =>
                (a.IsV4 && b.IsV4 && a.IPv4Address == b.IPv4Address)
                || (a.IsV6 && b.IsV6 && a.FirstV6Leg == b.FirstV6Leg && a.LastV6Leg == b.LastV6Leg);

            public static bool operator !=(TinyIPAddress a, TinyIPAddress b) =>
                (a.IsV4 && b.IsV4 && a.IPv4Address != b.IPv4Address)
                || (a.IsV6 && b.IsV6 && (a.FirstV6Leg != b.FirstV6Leg || a.LastV6Leg != b.LastV6Leg));

            public static bool operator <(TinyIPAddress a, TinyIPAddress b) => Compare(a, b) < 0;
            public static bool operator >(TinyIPAddress a, TinyIPAddress b) => Compare(a, b) > 0;
            public static bool operator <=(TinyIPAddress a, TinyIPAddress b) => Compare(a, b) <= 0;
            public static bool operator >=(TinyIPAddress a, TinyIPAddress b) => Compare(a, b) >= 0;

            public override int GetHashCode()
            {
                if (IsV4)
                {
                    return IPv4Address.GetHashCode();
                }
                int hash = 13;
                hash = (hash * -47) + FirstV6Leg.GetHashCode();
                hash = (hash * -47) + LastV6Leg.GetHashCode();
                return hash;
            }

            public bool Equals(TinyIPAddress other) =>
                IPv4Address == other.IPv4Address
                && FirstV6Leg == other.FirstV6Leg
                && LastV6Leg == other.LastV6Leg;

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }
                return obj is TinyIPAddress tinyIPAddress && Equals(tinyIPAddress);
            }

            public int CompareTo(TinyIPAddress other) => Compare(this, other);

            private static int Compare(TinyIPAddress a, TinyIPAddress b)
            {
                if (a.IsV4 && b.IsV4)
                {
                    return a.IPv4Address.CompareTo(b.IPv4Address);
                }
                var flc = a.FirstV6Leg.CompareTo(b.FirstV6Leg);
                if (flc != 0)
                {
                    return flc;
                }
                return a.LastV6Leg.CompareTo(b.LastV6Leg);
            }
        }
    }
}

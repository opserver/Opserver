﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace StackExchange.Opserver.Data
{
    public class IPNet
    {
        public IPAddress IPAddress { get; }
        public IPAddress Subnet { get; }
        public int CIDR { get; }
        public AddressFamily AddressFamily => IPAddress.AddressFamily;

        public string AddressFamilyDescription =>
            IPAddress.AddressFamily == AddressFamily.InterNetwork
                ? "IPv4"
                : IPAddress.AddressFamily == AddressFamily.InterNetworkV6
                    ? "IPv6"
                    : "";

        private TinyIPAddress? _tinyIPAddress { get; set; }
        private TinyIPAddress? _tinySubnet { get; set; }

        internal TinyIPAddress TIPAddress =>
            (_tinyIPAddress ?? (_tinyIPAddress = TinyIPAddress.FromIPAddress(IPAddress))).Value;

        internal TinyIPAddress TSubnet =>
            (_tinySubnet ?? (_tinySubnet = TinyIPAddress.FromIPAddress(Subnet ?? IPAddress))).Value;

        public IPAddress FirstAddressInSubnet => TinyFirstAddressInSubnet.ToIPAddress();
        public IPAddress LastAddressInSubnet => TinyLastAddressInSubnet.ToIPAddress();
        public IPAddress Broadcast => AddressFamily == AddressFamily.InterNetwork ? TinyBroadcast.ToIPAddress() : null;

        private TinyIPAddress TinyFirstAddressInSubnet => TIPAddress & TSubnet;
        private TinyIPAddress TinyLastAddressInSubnet => Subnet == null ? TIPAddress : (TIPAddress | ~TSubnet);
        private TinyIPAddress TinyBroadcast => Subnet == null ? TIPAddress : (TIPAddress | ~TSubnet);

        public bool IsPrivate => IsPrivateNetwork(this);

        public override string ToString() => IPAddress + "/" + CIDR.ToString();

        public bool Contains(IPAddress ip)
        {
            if (AddressFamily != ip.AddressFamily) return false;
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

        public bool IsPrivateNetwork(IPNet network) =>
            ReservedPrivateRanges.Any(r => r.Contains(network));

        public IPNet(IPAddress ip, IPAddress subnet, int? cidr = null)
        {
            IPAddress = ip;
            Subnet = subnet;
            if (subnet != null && !TSubnet.IsValidSubnet)
                throw new IPNetParseException("Error: subnet mask '{0}' is not a valid subnet", subnet);
            CIDR = cidr ?? (subnet == null ? GetBitLength(ip.AddressFamily) : TSubnet.NumberOfSetBits);
        }

        public static bool TryParse(string ipOrCidr, out IPNet net)
        {
            try { net = Parse(ipOrCidr); return true; }
            catch { net = null; return false; }
        }

        public static IPNet Parse(string ipOrCidr)
        {
            var parts = ipOrCidr.Split(StringSplits.ForwardSlash);
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[1], out int cidr))
                {
                    return Parse(parts[0], cidr);
                }
                throw new IPNetParseException("Error parsing CIDR from IP: '{0}'", parts[1]);
            }
            if (IPAddress.TryParse(parts[0], out IPAddress ip))
            {
                return new IPNet(ip, null);
            }
            throw new IPNetParseException("Error parsing IP Address from IP: '{0}'", parts[0]);
        }

        public static bool TryParse(string ip, int cidr, out IPNet net)
        {
            try { net = Parse(ip, cidr); return true; }
            catch { net = null; return false; }
        }

        public static IPNet Parse(string ip, int cidr)
        {
            if (IPAddress.TryParse(ip, out IPAddress ipAddr))
            {
                var bits = GetBitLength(ipAddr.AddressFamily);
                var subnet = IPAddressFromCIDR(bits, cidr);
                return new IPNet(ipAddr, subnet, cidr);
            }
            throw new IPNetParseException("Error parsing IP Address from IP: '{0}'", ip);
        }

        public static bool TryParse(string ip, string subnet, out IPNet net)
        {
            try { net = Parse(ip, subnet); return true; }
            catch { net = null; return false; }
        }

        public static IPNet Parse(string ip, string subnet)
        {
            if (IPAddress.TryParse(ip, out IPAddress ipAddr))
            {
                if (IPAddress.TryParse(subnet, out IPAddress subnetAddr))
                {
                    if (!TinyIPAddress.FromIPAddress(subnetAddr).Value.IsValidSubnet)
                        throw new IPNetParseException("Error parsing subnet mask Address from IP: '" + subnet + "' is not a valid subnet");
                    return new IPNet(ipAddr, subnetAddr);
                }
                throw new IPNetParseException("Error parsing subnet mask from IP: '" + subnet + "'");
            }
            throw new IPNetParseException("Error parsing IP Address from IP: '" + ip + "'");
        }

        public static IPAddress ToNetmask(AddressFamily addressFamily, int cidr) =>
            IPAddressFromCIDR(GetBitLength(addressFamily), cidr);

        private static int GetBitLength(AddressFamily family)
        {
            switch (family)
            {
                case AddressFamily.InterNetwork: return 32;
                case AddressFamily.InterNetworkV6: return 128;
                default: throw new ArgumentOutOfRangeException(nameof(family), "You're probably from the future, they added another more IPs, fix me.");
            }
        }

        // This is a much faster version thanks to Marc Gravell
        private static IPAddress IPAddressFromCIDR(int bitLength, int cidr)
        {
            var ipByteArray = new byte[bitLength / 8];
            int fullBytes = cidr / 8, lastBits = cidr % 8;
            for (var i = 0; i < fullBytes; i++)
            {
                ipByteArray[i] = 0xff;
            }
            if (lastBits != 0)
            {
                ipByteArray[fullBytes] = (byte)~((byte)0xFF >> lastBits);
            }
            return new IPAddress(ipByteArray);
        }

        /// <summary>
        /// Private IP Ranges reserved for internal use by ARIN
        /// These networks should not route on the global Internet
        /// </summary>
        private static readonly List<IPNet> ReservedPrivateRanges = new List<IPNet>
            {
                Parse("10.0.0.0/8"),
                Parse("172.16.0.0/12"),
                Parse("192.168.0.0/16"),
                Parse("fc00::/7")
            };

        public class IPNetParseException : Exception
        {
            public IPNetParseException(string msg, params object[] format) : base(string.Format(msg, format)) { }
        }

        [DataContract]
        public struct TinyIPAddress : IEquatable<TinyIPAddress>, IComparable<TinyIPAddress>
        {
            [DataMember(Order = 1)]
            private readonly uint? IPv4Address;
            [DataMember(Order = 2)]
            private readonly ulong? FirstV6Leg;
            [DataMember(Order = 3)]
            private readonly ulong? LastV6Leg;

            public AddressFamily AddressFamily => IPv4Address.HasValue ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;

            public string BitString
            {
                get
                {
                    StringBuilder sb;
                    if (IPv4Address.HasValue)
                    {
                        sb = StringBuilderCache.Get(32);
                        for (var i = 0; i < 32; i++)
                        {
                            sb.Append((IPv4Address.Value >> (31 - i)) & 1);
                        }
                    }
                    else
                    {
                        sb = StringBuilderCache.Get(128);
                        for (var i = 0; i < 64; i++)
                        {
                            sb.Append((FirstV6Leg.Value >> (63 - i)) & 1);
                        }
                        for (var i = 0; i < 64; i++)
                        {
                            sb.Append((LastV6Leg.Value >> (63 - i)) & 1);
                        }
                    }
                    return sb.ToStringRecycle();
                }
            }

            public IPAddress ToIPAddress()
            {
                if (IPv4Address.HasValue)
                {
                    return new IPAddress(new[] {
					(byte)(IPv4Address.Value >> 24),
					(byte)(IPv4Address.Value >> 16),
					(byte)(IPv4Address.Value >> 8),
					(byte)(IPv4Address.Value)
				});
                }
                if (FirstV6Leg.HasValue && LastV6Leg.HasValue)
                {
                    return new IPAddress(new[] {
					(byte)(FirstV6Leg.Value >> 56),
					(byte)(FirstV6Leg.Value >> 48),
					(byte)(FirstV6Leg.Value >> 40),
					(byte)(FirstV6Leg.Value >> 32),
					(byte)(FirstV6Leg.Value >> 24),
					(byte)(FirstV6Leg.Value >> 16),
					(byte)(FirstV6Leg.Value >> 8),
					(byte)(FirstV6Leg.Value),
					(byte)(LastV6Leg.Value >> 56),
					(byte)(LastV6Leg.Value >> 48),
					(byte)(LastV6Leg.Value >> 40),
					(byte)(LastV6Leg.Value >> 32),
					(byte)(LastV6Leg.Value >> 24),
					(byte)(LastV6Leg.Value >> 16),
					(byte)(LastV6Leg.Value >> 8),
					(byte)(LastV6Leg.Value)
				});
                }
                return null;
            }

            public TinyIPAddress(uint? ipv4Address)
            {
                IPv4Address = ipv4Address;
                FirstV6Leg = null;
                LastV6Leg = null;
            }

            public TinyIPAddress(ulong? ipv6FirstLeg, ulong? ipv6LastLeg)
            {
                IPv4Address = null;
                FirstV6Leg = ipv6FirstLeg;
                LastV6Leg = ipv6LastLeg;
            }

            public static TinyIPAddress? FromIPAddress(IPAddress addr)
            {
                var source = addr.GetAddressBytes();
                if (source.Length == 4)
                {
                    return new TinyIPAddress(((uint)source[0] << 24)
                                             | ((uint)source[1] << 16)
                                             | ((uint)source[2] << 8)
                                             | ((uint)source[3]));
                }
                if (source.Length == 16)
                {
                    return new TinyIPAddress(FromBytes(source, 0), FromBytes(source, 8));
                }
                return null;
            }

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
                    if (IPv4Address.HasValue) return NumberOfSetBitsImpl(IPv4Address.Value);
                    if (FirstV6Leg.HasValue && LastV6Leg.HasValue) return NumberOfSetBitsImpl(FirstV6Leg.Value) + NumberOfSetBitsImpl(LastV6Leg.Value);
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
                    if (IPv4Address.HasValue) return IsValidSubnetImpl(IPv4Address.Value);
                    if (FirstV6Leg.HasValue && LastV6Leg.HasValue)
                    {
                        return (FirstV6Leg == ulong.MaxValue && IsValidSubnetImpl(LastV6Leg.Value))
                           || (LastV6Leg == ulong.MinValue && IsValidSubnetImpl(FirstV6Leg.Value));
                    }
                    return false;
                }
            }

            private static bool IsValidSubnetImpl(uint i)
            {
                bool inZeros = false;
                for (var j = 31; j >= 0; j--)
                {
                    if (((i >> j) & 1) != 1) inZeros = true;
                    else if (inZeros) return false;
                }
                return true;
            }

            private static bool IsValidSubnetImpl(ulong l)
            {
                bool inZeros = false;
                for (var j = 63; j >= 0; j--)
                {
                    if (((l >> j) & 1) != 1) inZeros = true;
                    else if (inZeros) return false;
                }
                return true;
            }

            public static TinyIPAddress operator &(TinyIPAddress a, TinyIPAddress b) =>
                a.IPv4Address.HasValue && b.IPv4Address.HasValue
                    ? new TinyIPAddress(a.IPv4Address & b.IPv4Address)
                    : new TinyIPAddress(a.FirstV6Leg & b.FirstV6Leg, a.LastV6Leg & b.LastV6Leg);

            public static TinyIPAddress operator |(TinyIPAddress a, TinyIPAddress b) =>
                a.IPv4Address.HasValue && b.IPv4Address.HasValue
                    ? new TinyIPAddress(a.IPv4Address | b.IPv4Address)
                    : new TinyIPAddress(a.FirstV6Leg | b.FirstV6Leg, a.LastV6Leg | b.LastV6Leg);

            public static TinyIPAddress operator +(TinyIPAddress a, TinyIPAddress b) =>
                a.IPv4Address.HasValue && b.IPv4Address.HasValue
                    ? new TinyIPAddress(a.IPv4Address + b.IPv4Address)
                    : new TinyIPAddress(a.FirstV6Leg + b.FirstV6Leg, a.LastV6Leg + b.LastV6Leg);

            public static TinyIPAddress operator ~(TinyIPAddress a) =>
                a.IPv4Address.HasValue
                    ? new TinyIPAddress(~a.IPv4Address)
                    : new TinyIPAddress(~a.FirstV6Leg, ~a.LastV6Leg);

            public static bool operator ==(TinyIPAddress a, TinyIPAddress b) =>
                (a.IPv4Address.HasValue && b.IPv4Address.HasValue && a.IPv4Address == b.IPv4Address)
                || (a.FirstV6Leg.HasValue && b.FirstV6Leg.HasValue && a.FirstV6Leg == b.FirstV6Leg && a.LastV6Leg == b.LastV6Leg);

            public static bool operator !=(TinyIPAddress a, TinyIPAddress b) =>
                (a.IPv4Address.HasValue && b.IPv4Address.HasValue && a.IPv4Address != b.IPv4Address)
                || (a.FirstV6Leg.HasValue && b.FirstV6Leg.HasValue && (a.FirstV6Leg != b.FirstV6Leg || a.LastV6Leg != b.LastV6Leg));

            public static bool operator <=(TinyIPAddress a, TinyIPAddress b) => Compare(a, b) <= 0;
            public static bool operator >=(TinyIPAddress a, TinyIPAddress b) => Compare(a, b) >= 0;

            public override int GetHashCode()
            {
                if (IPv4Address.HasValue) return IPv4Address.GetHashCode();
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
                if (obj is null) return false;
                return obj is TinyIPAddress && Equals((TinyIPAddress)obj);
            }

            public int CompareTo(TinyIPAddress other) => Compare(this, other);

            private static int Compare(TinyIPAddress a, TinyIPAddress b)
            {
                if (a.IPv4Address.HasValue && b.IPv4Address.HasValue)
                    return a.IPv4Address.Value.CompareTo(b.IPv4Address.Value);
                var flc = a.FirstV6Leg.Value.CompareTo(b.FirstV6Leg.Value);
                if (flc != 0) return flc;
                return a.LastV6Leg.Value.CompareTo(b.LastV6Leg.Value);
            }
        }
    }
}

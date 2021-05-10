using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Opserver.Helpers
{
    /// <summary>
    /// Just a "safe" random that looks like System.Random.
    ///
    /// Most of the time, it doesn't matter what random we use.
    ///
    /// But rather than try and _justify_ that for auditing purposes,
    ///   let's just use the "no, it's really secure the NIST promises"-version.
    /// </summary>
    public class CryptoRandom
    {
        private static CryptoRandom _instance;
        public static CryptoRandom Instance => _instance ??= new CryptoRandom();

        private readonly RandomNumberGenerator _rng;

        private CryptoRandom()
        {
            _rng = RandomNumberGenerator.Create();
        }

        private int NextPositiveOrNegativeInt()
        {
            Span<byte> intSizedBuffer = stackalloc byte[sizeof(int)];
            NextBytes(intSizedBuffer);
            return BinaryPrimitives.ReadInt32LittleEndian(intSizedBuffer);
        }

        public void NextBytes(Span<byte> buffer) => _rng.GetBytes(buffer);

        public int Next()
        {
            const int MSB = 1 << 31;
            // by dropping the MSB, everything has a 2-in-4-billion(ish)
            // chance; contrast to using Abs etc, when we'd need to think
            // about int.MinValue (can't use Math.Abs) and making sure
            // that zero still has 2-in-4-billion chance
            return NextPositiveOrNegativeInt() & ~MSB;
        }

        public double NextDouble()
        {
            const double SCALE = 1.0 / int.MaxValue;

            var randomPosInt = Next();
            var ret = randomPosInt * SCALE;

            return ret;
        }

        public int Next(int exclusiveMax)
        {
            if (exclusiveMax < 0)
            {
                throw new ArgumentOutOfRangeException($"Must be >= 0, was {exclusiveMax}", nameof(exclusiveMax));
            }

            var randomZeroToOneExclusiveDouble = NextDouble();
            var ret = (int) (randomZeroToOneExclusiveDouble * exclusiveMax);

            return ret;
        }

        public int Next(int inclusiveMin, int exclusiveMax)
        {
            if (inclusiveMin > exclusiveMax)
            {
                throw new ArgumentOutOfRangeException(
                    $"Must be <= {nameof(exclusiveMax)}, was {inclusiveMin} ({nameof(exclusiveMax)} = {exclusiveMax})",
                    nameof(inclusiveMin));
            }

            // based on actual Random:
            // https://github.com/dotnet/corefx/blob/d3911035f2ba3eb5c44310342cc1d654e42aa316/src/Common/src/CoreLib/System/Random.cs

            long range = (long) exclusiveMax - (long) inclusiveMin;
            if (range <= int.MaxValue)
            {
                var offset = (int) (NextDouble() * range);
                return inclusiveMin + offset;
            }
            else
            {
                const int SHIFT = int.MaxValue - 1;
                const uint SCALE = (2 * (uint) int.MaxValue) - 1;

                var part1 = NextPositiveOrNegativeInt();
                var makeNegative = NextPositiveOrNegativeInt() % 2 == 0;
                part1 = makeNegative ? -part1 : part1;

                double rangeScale = part1;
                rangeScale += SHIFT;
                rangeScale /= SCALE;

                var offset = (long) (rangeScale * range);

                return (int) (inclusiveMin + offset);
            }
        }
    }
}

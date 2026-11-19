using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    sealed partial class JsonObject : IDynamicMetaObjectProvider
    {
        bool FastNumberToDouble(out double result)
        {
            double ret = FastNumberPart1;
            if (FastNumberPart2 != 0)
            {
                double frac = FastNumberPart2;
                var divideBy = Math.Pow(10, FastNumberPart2Length);
                frac /= divideBy;

                ret += frac;
            }

            if (FastNumberPart3 != 0)
            {
                double power = FastNumberPart3;
                ret *= Math.Pow(10, power);
            }

            if (FastNumberNegative)
            {
                result = -ret;
            }
            else
            {
                result = ret;
            }
            return true;
        }

        bool FastNumberToFloat(out float result)
        {
            double res;
            var ret = FastNumberToDouble(out res);
            result = (float)res;
            return ret;
        }

        bool FastNumberToDecimal(out decimal result)
        {
            double res;
            var ret = FastNumberToDouble(out res);
            result = (decimal)res;
            return ret;
        }

        bool FastNumberIsInteger()
        {
            return FastNumberPart2Length == 0 && FastNumberPart3 == 0;
        }

        bool FastNumberToByte(out byte result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                if (!FastNumberIsInteger() || FastNumberNegative)
                {
                    result = 0;
                    return false;
                }

                if (FastNumberPart1 > (int)byte.MaxValue)
                {
                    result = 0;
                    return false;
                }

                result = (byte)FastNumberPart1;

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < byte.MinValue || res > byte.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (byte)res;
            return true;
        }

        bool FastNumberToSByte(out sbyte result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                const ulong MaxNegativeMagnitude = (ulong)(-(long)sbyte.MinValue);

                if (!FastNumberIsInteger())
                {
                    result = 0;
                    return false;
                }

                if (!FastNumberNegative && FastNumberPart1 > (int)sbyte.MaxValue)
                {
                    result = 0;
                    return false;
                }

                if (FastNumberNegative && FastNumberPart1 > MaxNegativeMagnitude)
                {
                    result = 0;
                    return false;
                }

                result = (sbyte)FastNumberPart1;
                if (FastNumberNegative)
                {
                    result = (sbyte)-result;
                }

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < sbyte.MinValue || res > sbyte.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (sbyte)res;
            return true;
        }

        bool FastNumberToShort(out short result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                const ulong MaxNegativeMagnitude = (ulong)(-(long)short.MinValue);

                if (!FastNumberIsInteger())
                {
                    result = 0;
                    return false;
                }

                if (!FastNumberNegative && FastNumberPart1 > (int)short.MaxValue)
                {
                    result = 0;
                    return false;
                }

                if (FastNumberNegative && FastNumberPart1 > MaxNegativeMagnitude)
                {
                    result = 0;
                    return false;
                }

                result = (short)FastNumberPart1;
                if (FastNumberNegative)
                {
                    result = (short)-result;
                }

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < short.MinValue || res > short.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (short)res;
            return true;
        }

        bool FastNumberToUShort(out ushort result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                if (!FastNumberIsInteger() || FastNumberNegative)
                {
                    result = 0;
                    return false;
                }

                if (FastNumberPart1 > (int)ushort.MaxValue)
                {
                    result = 0;
                    return false;
                }

                result = (ushort)FastNumberPart1;

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < ushort.MinValue || res > ushort.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (ushort)res;
            return true;
        }

        bool FastNumberToInt(out int result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                const ulong MaxNegativeMagnitude = (ulong)(-(long)int.MinValue);

                if (!FastNumberIsInteger())
                {
                    result = 0;
                    return false;
                }

                if (!FastNumberNegative && FastNumberPart1 > int.MaxValue)
                {
                    result = 0;
                    return false;
                }

                if (FastNumberNegative && FastNumberPart1 > MaxNegativeMagnitude)
                {
                    result = 0;
                    return false;
                }

                result = (int)FastNumberPart1;
                if (FastNumberNegative)
                {
                    result = -result;
                }

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < int.MinValue || res > int.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (int)res;
            return true;
        }

        bool FastNumberToUInt(out uint result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                if (!FastNumberIsInteger() || FastNumberNegative)
                {
                    result = 0;
                    return false;
                }

                if (FastNumberPart1 > uint.MaxValue)
                {
                    result = 0;
                    return false;
                }

                result = (uint)FastNumberPart1;

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < uint.MinValue || res > uint.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (uint)res;
            return true;
        }

        bool FastNumberToLong(out long result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                const ulong MaxNegativeMagnitude = unchecked((ulong)(-long.MinValue));

                if (!FastNumberIsInteger())
                {
                    result = 0;
                    return false;
                }

                if (!FastNumberNegative && FastNumberPart1 > long.MaxValue)
                {
                    result = 0;
                    return false;
                }

                if (FastNumberNegative && FastNumberPart1 > MaxNegativeMagnitude)
                {
                    result = 0;
                    return false;
                }

                result = (long)FastNumberPart1;
                if (FastNumberNegative)
                {
                    result = -result;
                }

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < long.MinValue || res > long.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (long)res;
            return true;
        }

        bool FastNumberToULong(out ulong result)
        {
            if (DynamicDeserializer.UseFastIntegerConversion)
            {
                if (!FastNumberIsInteger() || FastNumberNegative)
                {
                    result = 0;
                    return false;
                }

                result = FastNumberPart1;

                return true;
            }

            double res;
            if (!FastNumberToDouble(out res))
            {
                result = 0;
                return false;
            }

            if (res < ulong.MinValue || res > ulong.MaxValue)
            {
                result = 0;
                return false;
            }

            result = (ulong)res;
            return true;
        }
    }
}

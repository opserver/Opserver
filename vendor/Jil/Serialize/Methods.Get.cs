using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Serialize
{
    static partial class Methods
    {
        public static MethodInfo GetValidateDouble()
        {
            // no difference depending on thunk writer here
            return ValidateDouble;
        }

        public static MethodInfo GetValidateFloat()
        {
            // no difference depending on thunk writer here
            return ValidateFloat;
        }

        public static MethodInfo GetCustomRFC1123(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomRFC1123 :
                    CustomRFC1123_ThunkWriter;
        }

        public static MethodInfo GetCustomISO8601ToString(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomISO8601ToString :
                    CustomISO8601ToString_ThunkWriter;
        }

        public static MethodInfo GetCustomWriteIntUnrolledSigned(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomWriteIntUnrolledSigned :
                    CustomWriteIntUnrolledSigned_ThunkWriter;
        }

        public static MethodInfo GetCustomWriteInt(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomWriteInt :
                    CustomWriteInt_ThunkWriter;
        }

        public static MethodInfo GetCustomWriteUIntUnrolled(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomWriteUIntUnrolled :
                    CustomWriteUIntUnrolled_ThunkWriter;
        }

        public static MethodInfo GetCustomWriteUInt(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomWriteUInt :
                    CustomWriteUInt_ThunkWriter;
        }

        public static MethodInfo GetCustomWriteLong(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomWriteLong :
                    CustomWriteLong_ThunkWriter;
        }

        public static MethodInfo GetCustomWriteULong(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomWriteULong :
                    CustomWriteULong_ThunkWriter;
        }

        public static MethodInfo GetProxyFloat(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    ProxyFloat :
                    ProxyFloat_ThunkWriter;
        }

        public static MethodInfo GetProxyDouble(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    ProxyDouble :
                    ProxyDouble_ThunkWriter;
        }

        public static MethodInfo GetProxyDecimal(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    ProxyDecimal :
                    ProxyDecimal_ThunkWriter;
        }

        public static MethodInfo GetWriteGuid(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    WriteGuid :
                    WriteGuid_ThunkWriter;
        }

        public static MethodInfo GetWriteEncodedStringWithQuotesWithNullsInlineJSONPUnsafe(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    WriteEncodedStringWithQuotesWithNullsInlineJSONPUnsafe :
                    WriteEncodedStringWithQuotesWithNullsInlineJSONPUnsafe_ThunkWriter;
        }

        public static MethodInfo GetWriteEncodedStringWithQuotesWithNullsInlineUnsafe(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    WriteEncodedStringWithQuotesWithNullsInlineUnsafe :
                    WriteEncodedStringWithQuotesWithNullsInlineUnsafe_ThunkWriter;
        }

        public static MethodInfo GetWriteEncodedStringWithNullsInlineJSONPUnsafe(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    WriteEncodedStringWithNullsInlineJSONPUnsafe :
                    WriteEncodedStringWithNullsInlineJSONPUnsafe_ThunkWriter;
        }

        public static MethodInfo GetWriteEncodedStringWithNullsInlineUnsafe(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    WriteEncodedStringWithNullsInlineUnsafe :
                    WriteEncodedStringWithNullsInlineUnsafe_ThunkWriter;
        }

        public static MethodInfo GetWriteTimeSpanISO8601(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    WriteTimeSpanISO8601 :
                    WriteTimeSpanISO8601_ThunkWriter;
        }

        public static MethodInfo GetWriteTimeSpanMicrosoft(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    WriteTimeSpanMicrosoft :
                    WriteTimeSpanMicrosoft_ThunkWriter;
        }

        public static MethodInfo GetCustomISO8601WithOffsetToString(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomISO8601WithOffsetToString :
                    CustomISO8601WithOffsetToString_ThunkWriter;
        }

        public static MethodInfo GetCustomWriteMicrosoftStyleWithOffset(bool needThunkWriter)
        {
            return
                !needThunkWriter ?
                    CustomWriteMicrosoftStyleWithOffset :
                    CustomWriteMicrosoftStyleWithOffset_ThunkWriter;
        }
    }
}

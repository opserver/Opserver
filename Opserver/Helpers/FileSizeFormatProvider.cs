using System;

namespace StackExchange.Opserver.Helpers
{
    //No need to reinvent the wheel, this is from http://stackoverflow.com/questions/128618/c-file-size-format-provider
    //Originally from: http://flimflan.com/blog/FileSizeFormatProvider.aspx
    public class FileSizeFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }

        private const string _fileSizeFormat = "fs";
        private const Decimal _oneKiloByte = 1024M;
        private const Decimal _oneMegaByte = _oneKiloByte * 1024M;
        private const Decimal _oneGigaByte = _oneMegaByte * 1024M;

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (format == null || !format.StartsWith(_fileSizeFormat))
            {
                return defaultFormat(format, arg, formatProvider);
            }

            if (arg is string)
            {
                return defaultFormat(format, arg, formatProvider);
            }

            Decimal size;

            try
            {
                size = Convert.ToDecimal(arg);
            }
            catch (InvalidCastException)
            {
                return defaultFormat(format, arg, formatProvider);
            }

            string suffix;
            if (size > _oneGigaByte)
            {
                size /= _oneGigaByte;
                suffix = "GB";
            }
            else if (size > _oneMegaByte)
            {
                size /= _oneMegaByte;
                suffix = "MB";
            }
            else if (size > _oneKiloByte)
            {
                size /= _oneKiloByte;
                suffix = "kB";
            }
            else
            {
                suffix = " B";
            }

            string precision = format.Substring(2);
            if (String.IsNullOrEmpty(precision)) precision = "2";
            return String.Format("{0:N" + precision + "}{1}", size, suffix);

        }

        private static string defaultFormat(string format, object arg, IFormatProvider formatProvider)
        {
            var formattableArg = arg as IFormattable;
            return formattableArg != null
                       ? formattableArg.ToString(format, formatProvider)
                       : arg.ToString();
        }

    }
}
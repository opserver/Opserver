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

        private const string fileSizeFormat = "fs";
        private const decimal OneKiloByte = 1024M;
        private const decimal OneMegaByte = OneKiloByte * 1024M;
        private const decimal OneGigaByte = OneMegaByte * 1024M;

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (format == null || !format.StartsWith(fileSizeFormat))
            {
                return defaultFormat(format, arg, formatProvider);
            }

            if (arg is string)
            {
                return defaultFormat(format, arg, formatProvider);
            }

            decimal size;

            try
            {
                size = Convert.ToDecimal(arg);
            }
            catch (InvalidCastException)
            {
                return defaultFormat(format, arg, formatProvider);
            }

            string suffix;
            if (size >= OneGigaByte)
            {
                size /= OneGigaByte;
                suffix = "GB";
            }
            else if (size >= OneMegaByte)
            {
                size /= OneMegaByte;
                suffix = "MB";
            }
            else if (size >= OneKiloByte)
            {
                size /= OneKiloByte;
                suffix = "kB";
            }
            else
            {
                suffix = " B";
            }

            string precision = format.Substring(2);
            if (string.IsNullOrEmpty(precision)) precision = "2";
            return string.Format("{0:N" + precision + "}{1}", size, suffix);

        }

        private static string defaultFormat(string format, object arg, IFormatProvider formatProvider)
        {
            IFormattable formattableArg = arg as IFormattable;
            if (formattableArg != null)
            {
                return formattableArg.ToString(format, formatProvider);
            }
            return arg.ToString();
        }

    }
}
using System;

namespace Opserver.Helpers
{
    //No need to reinvent the wheel, this is from https://stackoverflow.com/questions/128618/c-file-size-format-provider
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
            string defaultFormat() => (arg is IFormattable formattableArg) ? formattableArg.ToString(format, formatProvider) : arg.ToString();

            if (format == null || !format.StartsWith(fileSizeFormat) || arg is string)
            {
                return defaultFormat();
            }

            decimal size;

            try
            {
                size = Convert.ToDecimal(arg);
            }
            catch (InvalidCastException)
            {
                return defaultFormat();
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
            if (precision.IsNullOrEmpty()) precision = "2";
            return string.Format("{0:N" + precision + "}{1}", size, suffix);
        }
    }
}
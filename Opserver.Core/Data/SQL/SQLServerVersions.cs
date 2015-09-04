using System;

namespace StackExchange.Opserver.Data.SQL
{
    public static class SQLServerVersions
    {
        /// <summary>
        /// Sphinx
        /// </summary>
        public static class SQL7
        {
            public static readonly Version RTM = new Version(7, 0, 623);
            public static readonly Version SP1 = new Version(7, 0, 699);
            public static readonly Version SP2 = new Version(7, 0, 842);
            public static readonly Version SP3 = new Version(7, 0, 961);
            public static readonly Version SP4 = new Version(7, 0, 1063);
        }
        /// <summary>
        /// Shiloh
        /// </summary>
        public static class SQL2000
        {
            public static readonly Version RTM = new Version(8, 0, 194);
            public static readonly Version SP1 = new Version(8, 0, 384);
            public static readonly Version SP2 = new Version(8, 0, 532);
            public static readonly Version SP3 = new Version(8, 0, 760);
            public static readonly Version SP4 = new Version(8, 0, 2039);
        }
        /// <summary>
        /// Yukon
        /// </summary>
        public static class SQL2005
        {
            public static readonly Version RTM = new Version(9, 0, 1399); //.06 technicaly, meh
            public static readonly Version SP1 = new Version(9, 0, 2047);
            public static readonly Version SP2 = new Version(9, 0, 3042);
            public static readonly Version SP3 = new Version(9, 0, 4035);
            public static readonly Version SP4 = new Version(9, 0, 5000);
        }
        /// <summary>
        /// Katmai
        /// </summary>
        public static class SQL2008
        {
            public static readonly Version RTM = new Version(10, 0, 1600); //.22
            public static readonly Version SP1 = new Version(10, 0, 2531);
            public static readonly Version SP2 = new Version(10, 0, 4000);
            public static readonly Version SP3 = new Version(10, 0, 5500);
        }
        /// <summary>
        /// Kilimanjaro
        /// </summary>
        public static class SQL2008R2
        {
            public static readonly Version RTM = new Version(10, 50, 1600);
            public static readonly Version SP1 = new Version(10, 50, 2500);
            public static readonly Version SP2 = new Version(10, 50, 4000);
        }
        /// <summary>
        /// Denali
        /// </summary>
        public static class SQL2012
        {
            public static readonly Version RTM = new Version(11, 0, 2100);
            public static readonly Version SP1 = new Version(11, 0, 3000);
            public static readonly Version SP2 = new Version(11, 0, 5058);
        }
        /// <summary>
        /// Hekaton
        /// </summary>
        public static class SQL2014
        {
            public static readonly Version RTM = new Version(12, 0);
            public static readonly Version SP1 = new Version(12, 0, 4100);
        }

        public static class SQL2016
        {
            public static readonly Version RTM = new Version(13, 0);
        }
    }
}

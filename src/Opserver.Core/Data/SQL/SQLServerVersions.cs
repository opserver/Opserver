﻿using System;
using System.Collections.Immutable;

namespace Opserver.Data.SQL
{
    public enum SQLServerEdition
    {
        Personal = 1,
        Standard = 2,
        Enterprise = 3,
        Express = 4,
        Azure = 5,
    }

    public readonly struct SQLServerEngine
    {
        public SQLServerEngine(Version version, SQLServerEdition edition)
        {
            Version = version;
            Edition = edition;
        }

        public Version Version { get; }
        public SQLServerEdition Edition { get; }
    }

    public static class SQLServerVersions
    {
        public static class Editions
        {
            public static readonly ImmutableHashSet<SQLServerEdition> All = ImmutableHashSet.Create(
                (SQLServerEdition[])Enum.GetValues(typeof(SQLServerEdition))
            );

            public static readonly ImmutableHashSet<SQLServerEdition> AllExceptAzure = All.Remove(SQLServerEdition.Azure);
        }
        
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
            public static readonly Version SP1 = new Version(13, 0, 4001);
        }

        public static class SQL2017
        {
            public static readonly Version RTM = new Version(14, 0);
        }

        public static class SQL2019
        {
            public static readonly Version RTM = new Version(15, 0);
        }
    }
}

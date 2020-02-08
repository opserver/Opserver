using System;
using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLMemoryClerkSummaryInfo>> _memoryClerkSummary;
        public Cache<List<SQLMemoryClerkSummaryInfo>> MemoryClerkSummary => _memoryClerkSummary ??= SqlCacheList<SQLMemoryClerkSummaryInfo>(RefreshInterval);

        public class SQLMemoryClerkSummaryInfo : ISQLVersioned
        {
            Version IMinVersioned.MinVersion => SQLServerVersions.SQL2005.RTM;

            public string ClerkType { get; internal set; }
            public long UsedBytes { get; internal set; }
            public decimal UsedPercent { get; internal set; }
            public string Name =>
                ClerkType switch
                {
                    "CACHESTORE_BROKERDSH" => "Service Broker Dialog Security Header Cache",
                    "CACHESTORE_BROKERKEK" => "Service Broker Key Exchange Key Cache",
                    "CACHESTORE_BROKERREADONLY" => "Service Broker (Read-Only)",
                    "CACHESTORE_BROKERRSB" => "Service Broker Null Remote Service Binding Cache",
                    "CACHESTORE_BROKERTBLACS" => "Broker dormant rowsets",
                    "CACHESTORE_BROKERTO" => "Service Broker Transmission Object Cache",
                    "CACHESTORE_BROKERUSERCERTLOOKUP" => "Service Broker user certificates lookup result cache",
                    "CACHESTORE_CLRPROC" => "CLR Procedure Cache",
                    "CACHESTORE_CLRUDTINFO" => "CLR UDT Info",
                    "CACHESTORE_COLUMNSTOREOBJECTPOOL" => "Column Store Object Pool",
                    "CACHESTORE_CONVPRI" => "Conversation Priority Cache",
                    "CACHESTORE_EVENTS" => "Event Notification Cache",
                    "CACHESTORE_FULLTEXTSTOPLIST" => "Full Text Stoplist Cache",
                    "CACHESTORE_NOTIF" => "Notification Store",
                    "CACHESTORE_OBJCP" => "Object Plans",
                    "CACHESTORE_PHDR" => "Bound Trees",
                    "CACHESTORE_SEARCHPROPERTYLIST" => "Search Property List Cache",
                    "CACHESTORE_SEHOBTCOLUMNATTRIBUTE" => "SE Shared Column Metadata Cache",
                    "CACHESTORE_SQLCP" => "SQL Plans",
                    "CACHESTORE_STACKFRAMES" => "SOS_StackFramesStore",
                    "CACHESTORE_SYSTEMROWSET" => "System Rowset Store",
                    "CACHESTORE_TEMPTABLES" => "Temporary Tables & Table Variables",
                    "CACHESTORE_VIEWDEFINITIONS" => "View Definition Cache",
                    "CACHESTORE_XML_SELECTIVE_DG" => "XML DB Cache (Selective)",
                    "CACHESTORE_XMLDBATTRIBUTE" => "XML DB Cache (Attribute)",
                    "CACHESTORE_XMLDBELEMENT" => "XML DB Cache (Element)",
                    "CACHESTORE_XMLDBTYPE" => "XML DB Cache (Type)",
                    "CACHESTORE_XPROC" => "Extended Stored Procedures",
                    "MEMORYCLERK_BHF" => "Best Hugging Friend (According to Brent Ozar)",
                    "MEMORYCLERK_FILETABLE" => "Memory Clerk (File Table)",
                    "MEMORYCLERK_FSCHUNKER" => "Memory Clerk (FS Chunker)",
                    "MEMORYCLERK_FULLTEXT" => "Full Text",
                    "MEMORYCLERK_FULLTEXT_SHMEM" => "Full-text IG",
                    "MEMORYCLERK_HADR" => "HADR",
                    "MEMORYCLERK_HOST" => "Host",
                    "MEMORYCLERK_LANGSVC" => "Language Service",
                    "MEMORYCLERK_LWC" => "Light Weight Cache",
                    "MEMORYCLERK_QSRANGEPREFETCH" => "QS Range Prefetch",
                    "MEMORYCLERK_SERIALIZATION" => "Serialization",
                    "MEMORYCLERK_SNI" => "SNI",
                    "MEMORYCLERK_SOSMEMMANAGER" => "SOS Memory Manager",
                    "MEMORYCLERK_SOSNODE" => "SOS Node",
                    "MEMORYCLERK_SOSOS" => "SOS Memory Clerk",
                    "MEMORYCLERK_SQLBUFFERPOOL" => "Buffer Pool",
                    "MEMORYCLERK_SQLCLR" => "CLR",
                    "MEMORYCLERK_SQLCLRASSEMBLY" => "CLR Assembly",
                    "MEMORYCLERK_SQLCONNECTIONPOOL" => "Connection Pool",
                    "MEMORYCLERK_SQLGENERAL" => "General",
                    "MEMORYCLERK_SQLHTTP" => "HTTP",
                    "MEMORYCLERK_SQLLOGPOOL" => "Log Pool",
                    "MEMORYCLERK_SQLOPTIMIZER" => "SQL Optimizer",
                    "MEMORYCLERK_SQLQERESERVATIONS" => "SQL Reservations",
                    "MEMORYCLERK_SQLQUERYCOMPILE" => "SQL Query Compile",
                    "MEMORYCLERK_SQLQUERYEXEC" => "SQL Query Exec",
                    "MEMORYCLERK_SQLQUERYPLAN" => "SQL Query Plan",
                    "MEMORYCLERK_SQLSERVICEBROKER" => "SQL Service Broker",
                    "MEMORYCLERK_SQLSERVICEBROKERTRANSPORT" => "Unified Communication Stack",
                    "MEMORYCLERK_SQLSOAP" => "SQL SOAP",
                    "MEMORYCLERK_SQLSOAPSESSIONSTORE" => "SQL SOAP (Session Store)",
                    "MEMORYCLERK_SQLSTORENG" => "SQL Storage Engine",
                    "MEMORYCLERK_SQLUTILITIES" => "SQL Utilities",
                    "MEMORYCLERK_SQLXML" => "SQL XML",
                    "MEMORYCLERK_SQLXP" => "SQL XP",
                    "MEMORYCLERK_TRACE_EVTNOTIF" => "Trace Event Notification",
                    "MEMORYCLERK_XE" => "XE Engine",
                    "MEMORYCLERK_XE_BUFFER" => "XE Buffer",
                    "MEMORYCLERK_XTP" => "XTP Processor",
                    "OBJECTSTORE_LBSS" => "Lbss Cache (Object Store)",
                    "OBJECTSTORE_LOCK_MANAGER" => "Lock Manager (Object Store)",
                    "OBJECTSTORE_SECAUDIT_EVENT_BUFFER" => "Audit Event Buffer (Object Store)",
                    "OBJECTSTORE_SERVICE_BROKER" => "Service Broker (Object Store)",
                    "OBJECTSTORE_SNI_PACKET" => "SNI Packet (Object Store)",
                    "OBJECTSTORE_XACT_CACHE" => "Transactions Cache (Object Store)",
                    "USERSTORE_DBMETADATA" => "DB Metadata (User Store)",
                    "USERSTORE_OBJPERM" => "Object Permissions (User Store)",
                    "USERSTORE_SCHEMAMGR" => "Schema Manager (User Store)",
                    "USERSTORE_SXC" => "SXC (User Store)",
                    "USERSTORE_TOKENPERM" => "Token Permissions (User Store)",
                    _ => "Other (" + ClerkType + ")",
                };

            public bool IsBufferPool => ClerkType == "MEMORYCLERK_SQLBUFFERPOOL";
            public bool IsPlanCache => ClerkType == "CACHESTORE_SQLCP";

            internal const string FetchSQL = @"
   Select [type] ClerkType, 
          Sum(pages_kb * 1024) AS UsedBytes, 
          Cast(100 * Sum(pages_kb)*1.0/(Select Sum(pages_kb) From sys.dm_os_memory_clerks) as Decimal(7, 4)) UsedPercent
     From sys.dm_os_memory_clerks 
    Where pages_kb > 0
 Group By [type]
 Order By Sum(pages_kb) Desc";

            public string GetFetchSQL(Version v)
            {
                if (v < SQLServerVersions.SQL2012.RTM)
                    return FetchSQL.Replace("pages_kb", "(single_pages_kb + multi_pages_kb)");

                return FetchSQL;
            }
        }
    }
}

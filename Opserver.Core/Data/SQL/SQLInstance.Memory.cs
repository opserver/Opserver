using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLMemoryClerkSummaryInfo>> _memoryClerkSummary;
        public Cache<List<SQLMemoryClerkSummaryInfo>> MemoryClerkSummary => _memoryClerkSummary ?? (_memoryClerkSummary = SqlCacheList<SQLMemoryClerkSummaryInfo>(30));

        public class SQLMemoryClerkSummaryInfo : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public string ClerkType { get; internal set; }
            public long UsedBytes { get; internal set; }
            public decimal UsedPercent { get; internal set; }
            public string Name
            {
                get
                {
                    switch (ClerkType)
                    {
                        case "CACHESTORE_BROKERDSH": return "Service Broker Dialog Security Header Cache";
                        case "CACHESTORE_BROKERKEK": return "Service Broker Key Exchange Key Cache";
                        case "CACHESTORE_BROKERREADONLY": return "Service Broker (Read-Only)";
                        case "CACHESTORE_BROKERRSB": return "Service Broker Null Remote Service Binding Cache";
                        case "CACHESTORE_BROKERTBLACS": return "Broker dormant rowsets";
                        case "CACHESTORE_BROKERTO": return "Service Broker Transmission Object Cache";
                        case "CACHESTORE_BROKERUSERCERTLOOKUP": return "Service Broker user certificates lookup result cache";
                        case "CACHESTORE_CLRPROC": return "CLR Procedure Cache";
                        case "CACHESTORE_CLRUDTINFO": return "CLR UDT Info";
                        case "CACHESTORE_COLUMNSTOREOBJECTPOOL": return "Column Store Object Pool";
                        case "CACHESTORE_CONVPRI": return "Conversation Priority Cache";
                        case "CACHESTORE_EVENTS": return "Event Notification Cache";
                        case "CACHESTORE_FULLTEXTSTOPLIST": return "Full Text Stoplist Cache";
                        case "CACHESTORE_NOTIF": return "Notification Store";
                        case "CACHESTORE_OBJCP": return "Object Plans";
                        case "CACHESTORE_PHDR": return "Bound Trees";
                        case "CACHESTORE_SEARCHPROPERTYLIST": return "Search Property List Cache";
                        case "CACHESTORE_SEHOBTCOLUMNATTRIBUTE": return "SE Shared Column Metadata Cache";
                        case "CACHESTORE_SQLCP": return "SQL Plans";
                        case "CACHESTORE_STACKFRAMES": return "SOS_StackFramesStore";
                        case "CACHESTORE_SYSTEMROWSET": return "System Rowset Store";
                        case "CACHESTORE_TEMPTABLES": return "Temporary Tables & Table Variables";
                        case "CACHESTORE_VIEWDEFINITIONS": return "View Definition Cache";
                        case "CACHESTORE_XML_SELECTIVE_DG": return "XML DB Cache (Selective)";
                        case "CACHESTORE_XMLDBATTRIBUTE": return "XML DB Cache (Attribute)";
                        case "CACHESTORE_XMLDBELEMENT": return "XML DB Cache (Element)";
                        case "CACHESTORE_XMLDBTYPE": return "XML DB Cache (Type)";
                        case "CACHESTORE_XPROC": return "Extended Stored Procedures";
                        case "MEMORYCLERK_BHF": return "Best Hugging Friend (According to Brent Ozar)";
                        case "MEMORYCLERK_FILETABLE": return "Memory Clerk (File Table)";
                        case "MEMORYCLERK_FSCHUNKER": return "Memory Clerk (FS Chunker)";
                        case "MEMORYCLERK_FULLTEXT": return "Full Text";
                        case "MEMORYCLERK_FULLTEXT_SHMEM": return "Full-text IG";
                        case "MEMORYCLERK_HADR": return "HADR";
                        case "MEMORYCLERK_HOST": return "Host";
                        case "MEMORYCLERK_LANGSVC": return "Language Service";
                        case "MEMORYCLERK_LWC": return "Light Weight Cache";
                        case "MEMORYCLERK_QSRANGEPREFETCH": return "QS Range Prefetch";
                        case "MEMORYCLERK_SERIALIZATION": return "Serialization";
                        case "MEMORYCLERK_SNI": return "SNI";
                        case "MEMORYCLERK_SOSMEMMANAGER": return "SOS Memory Manager";
                        case "MEMORYCLERK_SOSNODE": return "SOS Node";
                        case "MEMORYCLERK_SOSOS": return "SOS Memory Clerk";
                        case "MEMORYCLERK_SQLBUFFERPOOL": return "Buffer Pool";
                        case "MEMORYCLERK_SQLCLR": return "CLR";
                        case "MEMORYCLERK_SQLCLRASSEMBLY": return "CLR Assembly";
                        case "MEMORYCLERK_SQLCONNECTIONPOOL": return "Connection Pool";
                        case "MEMORYCLERK_SQLGENERAL": return "General";
                        case "MEMORYCLERK_SQLHTTP": return "HTTP";
                        case "MEMORYCLERK_SQLLOGPOOL": return "Log Pool";
                        case "MEMORYCLERK_SQLOPTIMIZER": return "SQL Optimizer";
                        case "MEMORYCLERK_SQLQERESERVATIONS": return "SQL Reservations";
                        case "MEMORYCLERK_SQLQUERYCOMPILE": return "SQL Query Compile";
                        case "MEMORYCLERK_SQLQUERYEXEC": return "SQL Query Exec";
                        case "MEMORYCLERK_SQLQUERYPLAN": return "SQL Query Plan";
                        case "MEMORYCLERK_SQLSERVICEBROKER": return "SQL Service Broker";
                        case "MEMORYCLERK_SQLSERVICEBROKERTRANSPORT": return "Unified Communication Stack";
                        case "MEMORYCLERK_SQLSOAP": return "SQL SOAP";
                        case "MEMORYCLERK_SQLSOAPSESSIONSTORE": return "SQL SOAP (Session Store)";
                        case "MEMORYCLERK_SQLSTORENG": return "SQL Storage Engine";
                        case "MEMORYCLERK_SQLUTILITIES": return "SQL Utilities";
                        case "MEMORYCLERK_SQLXML": return "SQL XML";
                        case "MEMORYCLERK_SQLXP": return "SQL XP";
                        case "MEMORYCLERK_TRACE_EVTNOTIF": return "Trace Event Notification";
                        case "MEMORYCLERK_XE": return "XE Engine";
                        case "MEMORYCLERK_XE_BUFFER": return "XE Buffer";
                        case "MEMORYCLERK_XTP": return "XTP Processor";
                        case "OBJECTSTORE_LBSS": return "Lbss Cache (Object Store)";
                        case "OBJECTSTORE_LOCK_MANAGER": return "Lock Manager (Object Store)";
                        case "OBJECTSTORE_SECAUDIT_EVENT_BUFFER": return "Audit Event Buffer (Object Store)";
                        case "OBJECTSTORE_SERVICE_BROKER": return "Service Broker (Object Store)";
                        case "OBJECTSTORE_SNI_PACKET": return "SNI Packet (Object Store)";
                        case "OBJECTSTORE_XACT_CACHE": return "Transactions Cache (Object Store)";
                        case "USERSTORE_DBMETADATA": return "DB Metadata (User Store)";
                        case "USERSTORE_OBJPERM": return "Object Permissions (User Store)";
                        case "USERSTORE_SCHEMAMGR": return "Schema Manager (User Store)";
                        case "USERSTORE_SXC": return "SXC (User Store)";
                        case "USERSTORE_TOKENPERM": return "Token Permissions (User Store)";
                        default:
                            return "Other (" + ClerkType + ")";
                    }
                }
            }
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

            public string GetFetchSQL(Version version)
            {
                if (version < SQLServerVersions.SQL2012.RTM)
                    return FetchSQL.Replace("pages_kb", "single_pages_kb + multi_pages_kb");

                return FetchSQL;
            }
        }

    }
}

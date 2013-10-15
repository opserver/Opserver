using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StackExchange.Status.Models.SQL.QueryPlans
{
    public class ShowPlan
    {
        public static ShowPlan FromXml(string xml)
        {
            return new ShowPlan();
        }
        
        public List<BaseStatement> Statements { get; private set; }
    }

    public class SimpleStatement : BaseStatement
    {
        public QueryPlan QueryPlan;

        [XmlElementAttribute("UDF")]
        public Function[] UDF;

        public Function StoredProc;
    }

    public class QueryPlanType
    {
        public InternalInfo InternalInfo;
        public ThreadStat ThreadStat;

        [System.Xml.Serialization.XmlArrayItemAttribute("MissingIndexGroup", IsNullable = false)]
        public MissingIndexGroup[] MissingIndexes;
        public GuessedSelectivity GuessedSelectivity;
        public UnmatchedIndexesType UnmatchedIndexes;
        public Warnings Warnings;
        public MemoryGrantType MemoryGrantInfo;
        public OptimizerHardwareDependentPropertiesType OptimizerHardwareDependentProperties;
        public RelOpType RelOp;
        [System.Xml.Serialization.XmlArrayItemAttribute("ColumnReference", IsNullable = false)]
        public ColumnReferenceType[] ParameterList;

        public int DegreeOfParallelism { get; private set; }
        public bool DegreeOfParallelismSpecified { get; private set; }
        public string NonParallelPlanReason { get; private set; }
        public ulong MemoryGrant { get; private set; }
        public bool MemoryGrantSpecified { get; private set; }
        public ulong CachedPlanSize { get; private set; }
        public bool CachedPlanSizeSpecified { get; private set; }
        public ulong CompileTime { get; private set; }
        public bool CompileTimeSpecified { get; private set; }
        public ulong CompileCPU { get; private set; }
        public bool CompileCPUSpecified { get; private set; }
        public ulong CompileMemory { get; private set; }
        public bool CompileMemorySpecified { get; private set; }
        public bool UsePlan { get; private set; }
        public bool UsePlanSpecified { get; private set; }
    }

    public class InternalInfo
    {
        public System.Xml.XmlElement[] Any { get; private set; }
        public System.Xml.XmlAttribute[] AnyAttr { get; private set; }
    }

    public class ThreadStat
    {
        public ThreadReservation[] ThreadReservation { get; private set; }
        public int Branches { get; private set; }
        public int UsedThreads { get; private set; }
        public bool UsedThreadsSpecified { get; private set; }
    }

    public class ThreadReservation
    {
        public int NodeId { get; private set; }
        public int ReservedThreads { get; private set; }
    }

    public class MissingIndexGroup
    {
        public MissingIndex[] MissingIndex { get; private set; }
        public double Impact { get; private set; }
    }

    public class MissingIndex
    {
        public ColumnGroup[] ColumnGroup { get; private set; }
        public string Database { get; private set; }
        public string Schema { get; private set; }
        public string Table { get; private set; }
    }

    public class ColumnGroup
    {
        public Column[] Column { get; private set; }
        public ColumnGroupTypeUsage Usage { get; private set; }
    }

    public class Column
    {
        public string Name { get; private set; }
        public int ColumnId { get; private set; }
    }

    public enum ColumnGroupTypeUsage
    {
        EQUALITY,
        INEQUALITY,
        INCLUDE
    }

    public class GuessedSelectivity
    {
        public Object Spatial;
    }

    public class Object
    {
        public string Server;
        public string Database;
        public string Schema;
        public string Table;
        public string Index;
        public bool Filtered;
        public bool FilteredSpecified;
        public string Alias;
        public int TableReferenceId;
        public bool TableReferenceIdSpecified;
        public IndexKind IndexKind;
        public bool IndexKindSpecified;
    }

    public enum IndexKind
    {
        Heap,
        Clustered,
        FTSChangeTracking,
        FTSMapping,
        NonClustered,
        PrimaryXML,
        SecondaryXML,
        Spatial,
        ViewClustered,
        ViewNonClustered
    }

    public class UnmatchedIndexesType
    {
        [XmlArrayItemAttribute("Object", IsNullable = false)]
        public Object[] Parameterization;
    }

    public class WarningsType
    {
        [XmlElementAttribute("ColumnsWithNoStatistics", typeof(ColumnReferenceListType1))]
        [XmlElementAttribute("PlanAffectingConvert", typeof(AffectingConvertWarningType))]
        [XmlElementAttribute("SpillToTempDb", typeof(SpillToTempDbType))]
        [XmlElementAttribute("Wait", typeof(WaitWarningType))]
        public object[] Items;
        
        public bool NoJoinPredicate;
        public bool NoJoinPredicateSpecified;
        public bool SpatialGuess;
        public bool SpatialGuessSpecified;
        public bool UnmatchedIndexes;
        public bool UnmatchedIndexesSpecified;
        public bool FullUpdateForOnlineIndexBuild;
        public bool FullUpdateForOnlineIndexBuildSpecified;
    }

    public class BaseStatement
    {
        [XmlAttribute("StatementId")]
        public int? Id { get; private set; }
        [XmlAttribute("StatementCompId")]
        public int? CompId { get; private set; }

        [XmlAttribute("StatementSetOptions")]
        public StatementSetOptions SetOptions { get; private set; }
        
        [XmlAttribute("StatementText")]
        public string Text { get; private set; }
        [XmlAttribute("StatementType")]
        public string Type { get; private set; }

        [XmlAttribute("ParameterizedText")]
        public string ParameterizedText { get; private set; }
        [XmlAttribute("ParameterizedPlanHandle")]
        public string ParameterizedPlanHandle { get; private set; }
        
        [XmlAttribute("RetrievedFromCache")]
        public string RetrievedFromCache { get; private set; }
        [XmlAttribute("StatementEstRows")]
        public double? EstRows { get; private set; }
        [XmlAttribute("StatementSubTreeCost")]
        public double SubTreeCost { get; private set; }
        
        [XmlAttribute("QueryHash")]
        public string QueryHash { get; private set; }
        [XmlAttribute("QueryPlanHash")]
        public string QueryPlanHash { get; private set; }

        
        [XmlAttribute("PlanGuideDB")]
        public string PlanGuideDB { get; private set; }
        [XmlAttribute("PlanGuideName")]
        public string PlanGuideName { get; private set; }
        [XmlAttribute("TemplatePlanGuideDB")]
        public string TemplatePlanGuideDB { get; private set; }
        [XmlAttribute("TemplatePlanGuideName")]
        public string TemplatePlanGuideName { get; private set; }

        [XmlAttribute("StatementOptmLevel")]
        public string StatementOptmLevel { get; private set; }
        [XmlAttribute("StatementOptmEarlyAbortReason")]
        public StatementOptmEarlyAbortReason StatementOptmEarlyAbortReason { get; private set; }
    }

    /// <summary>
    /// The set options that affects query cost
    /// </summary>
    public class StatementSetOptions
    {
        [XmlAttribute("ANSI_NULLS")]
        public bool ANSI_NULLS { get; private set; }
        [XmlAttribute("ANSI_PADDING")]
        public bool ANSI_PADDING { get; private set; }
        [XmlAttribute("ANSI_WARNINGS")]
        public bool ANSI_WARNINGS { get; private set; }
        [XmlAttribute("ARITHABORT")]
        public bool ARITHABORT { get; private set; }
        [XmlAttribute("CONCAT_NULL_YIELDS_NULL")]
        public bool CONCAT_NULL_YIELDS_NULL { get; private set; }
        [XmlAttribute("NUMERIC_ROUNDABORT")]
        public bool NUMERIC_ROUNDABORT { get; private set; }
        [XmlAttribute("QUOTED_IDENTIFIER")]
        public bool QUOTED_IDENTIFIER { get; private set; }
    }

    public enum StatementOptmEarlyAbortReason
    {
        TimeOut,
        MemoryLimitExceeded,
        GoodEnoughPlanFound
    }

    public enum ExecutionModeType
    {
        Row,
        Batch
    }

    public enum CursorType
    {
        Dynamic,
        FastForward,
        Keyset,
        SnapShot
    }

    public class Function
    {
        [XmlElement("Statements")]
        public List<BaseStatement> Statements { get; private set; }

        [XmlAttribute("ProcName")]
        public string ProcName;
    }
}

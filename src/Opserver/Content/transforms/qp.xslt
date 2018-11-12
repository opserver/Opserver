<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:exslt="http://exslt.org/common"
  xmlns:s="http://schemas.microsoft.com/sqlserver/2004/07/showplan"
  exclude-result-prefixes="msxsl s xsl">
  <xsl:output method="html" indent="no" omit-xml-declaration="yes" />

  <!-- Disable built-in recursive processing templates -->
  <xsl:template match="*|/|text()|@*" mode="NodeLabel" />
  <xsl:template match="*|/|text()|@*" mode="NodeLabel2" />
  <xsl:template match="*|/|text()|@*" mode="ToolTipDescription" />
  <xsl:template match="*|/|text()|@*" mode="ToolTipDetails" />

  <!-- Default template -->
  <xsl:template match="/">
    <xsl:apply-templates select="s:ShowPlanXML" />
  </xsl:template>

  <!-- Outermost div that contains all statement plans. -->
  <xsl:template match="s:ShowPlanXML">
    <div class="qp-root">
      <xsl:apply-templates select="s:BatchSequence/s:Batch/s:Statements/*" mode="Statement" />
    </div>
  </xsl:template>

  <xsl:template match="s:BatchSequence/s:Batch/s:Statements/*" mode="Statement">
    <div class="qp-statement-header">
      <div class="qp-statement-header-row">
        <div>
          <xsl:value-of select="@StatementText" />
        </div>
      </div>
      <xsl:apply-templates select="s:QueryPlan/s:MissingIndexes/s:MissingIndexGroup" mode="MissingIndex" />
    </div>
    <xsl:apply-templates select="." mode="QpTr" />
  </xsl:template>

  <xsl:template match="s:MissingIndexGroup" mode="MissingIndex">
    <div class="qp-statement-header-row missing-index">
      <div>
        Missing Index (Impact <xsl:value-of select="@Impact" />): <xsl:apply-templates select="s:MissingIndex" mode="CreateIndex" />
      </div>
    </div>
  </xsl:template>

  <!-- This template produces the "CREATE INDEX ..." text -->
  <xsl:template match="s:MissingIndex" mode="CreateIndex">
    CREATE NONCLUSTERED INDEX [&lt;Name of Missing Index, sysname,>] ON <xsl:value-of select="@Schema" />.<xsl:value-of select="@Table" /> (<xsl:for-each select="s:ColumnGroup[@Usage!='INCLUDE']/s:Column">
      <xsl:value-of select="@Name" />
      <xsl:if test="position() != last()">,</xsl:if>
    </xsl:for-each>)
    <xsl:if test="s:ColumnGroup[@Usage='INCLUDE']">
      INCLUDE (<xsl:for-each select="s:ColumnGroup[@Usage='INCLUDE']/s:Column">
        <xsl:value-of select="@Name" />
        <xsl:if test="position() != last()">,</xsl:if>
      </xsl:for-each>)
    </xsl:if>
  </xsl:template>

  <!-- Each node has a parent qp-tr element which contains / positions the node and its children -->
  <xsl:template match="s:RelOp|s:StmtSimple|s:StmtUseDb|s:StmtCond|s:StmtCursor|s:Operation" mode="QpTr">
    <div class="qp-tr">
      <xsl:if test="@StatementId">
        <xsl:attribute name="data-statement-id">
          <xsl:value-of select="@StatementId" />
        </xsl:attribute>
      </xsl:if>
      <div>
        <div class="qp-node-outer">
          <div class="qp-node">
            <xsl:if test="@NodeId">
              <xsl:attribute name="data-node-id">
                <xsl:value-of select="@NodeId" />
              </xsl:attribute>
            </xsl:if>
            <xsl:call-template name="NodeIcon" />
            <div>
              <xsl:apply-templates select="." mode="NodeLabel" />
            </div>
            <xsl:apply-templates select="." mode="NodeLabel2" />
            <xsl:apply-templates select="." mode="NodeCostLabel" />
            <xsl:call-template name="ToolTip" />
          </div>
        </div>
      </div>
      <div>
        <xsl:apply-templates select="*/*" mode="QpTr" />
      </div>
    </div>
  </xsl:template>

  <!-- Writes the tool tip -->
  <xsl:template name="ToolTip">
    <div class="qp-tt">
      <div class="qp-tt-header">
        <xsl:apply-templates select="." mode="NodeLabel" />
      </div>
      <div>
        <xsl:apply-templates select="." mode="ToolTipDescription" />
      </div>
      <xsl:call-template name="ToolTipGrid" />
      <xsl:apply-templates select="* | @* | */* | */@*" mode="ToolTipDetails" />
      <xsl:choose>
        <xsl:when test="s:QueryPlan">
          <xsl:apply-templates select="s:QueryPlan" mode="QueryPlanToolTipDetails" />
        </xsl:when>
        <xsl:otherwise>
          <xsl:call-template name="ToolTipDetails" />
        </xsl:otherwise>
      </xsl:choose>
    </div>
  </xsl:template>

  <xsl:template match="s:QueryPlan" mode="QueryPlanToolTipDetails">
    <xsl:call-template name="ToolTipDetails" />
  </xsl:template>

  <!-- Writes the grid of node properties to the tool tip -->
  <xsl:template name="ToolTipGrid">
    <table>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:QueryPlan/@CachedPlanSize" />
        <xsl:with-param name="Label">Cached plan size</xsl:with-param>
        <xsl:with-param name="Value" select="concat(s:QueryPlan/@CachedPlanSize, ' KB')" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@PhysicalOp" />
        <xsl:with-param name="Label">Physical Operation</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:apply-templates select="." mode="PhysicalOperation" />
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@LogicalOp" />
        <xsl:with-param name="Label">Logical Operation</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:apply-templates select="." mode="LogicalOperation" />
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualJoinType" />
        <xsl:with-param name="Label">Actual Join Type</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:value-of select="s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualJoinType" />
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:RunTimeInformation" />
        <xsl:with-param name="Label">Actual Execution Mode</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:choose>
            <xsl:when test="s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualExecutionMode">
              <xsl:value-of select="s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualExecutionMode" />
            </xsl:when>
            <xsl:otherwise>Row</xsl:otherwise>
          </xsl:choose>
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Estimated Join Type</xsl:with-param>
        <xsl:with-param name="Value" select="@EstimatedJoinType" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@IsAdaptive" />
        <xsl:with-param name="Label">Is Adaptive</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:choose>
            <xsl:when test="@IsAdaptive = 'true'">True</xsl:when>
            <xsl:otherwise>False</xsl:otherwise>
          </xsl:choose>
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Estimated Execution Mode</xsl:with-param>
        <xsl:with-param name="Value" select="@EstimatedExecutionMode" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Storage</xsl:with-param>
        <xsl:with-param name="Value" select="s:IndexScan/@Storage|s:TableScan/@Storage" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Number of Rows Read</xsl:with-param>
        <xsl:with-param name="Value" select="sum(s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualRowsRead)" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@AdaptiveThresholdRows" />
        <xsl:with-param name="Label">Adaptive Threshold Rows</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:value-of select="@AdaptiveThresholdRows" />
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:RunTimeInformation" />
        <xsl:with-param name="Label">Actual Number of Rows</xsl:with-param>
        <xsl:with-param name="Value" select="sum(s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualRows)" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:RunTimeInformation" />
        <xsl:with-param name="Label">Actual Number of Batches</xsl:with-param>
        <xsl:with-param name="Value" select="sum(s:RunTimeInformation/s:RunTimeCountersPerThread/@Batches)" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Estimated Operator Cost</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:variable name="EstimatedOperatorCost">
            <xsl:call-template name="EstimatedOperatorCost" />
          </xsl:variable>
          <xsl:variable name="TotalCost">
            <xsl:value-of select="ancestor::s:QueryPlan/s:RelOp/@EstimatedTotalSubtreeCost" />
          </xsl:variable>
          <xsl:variable name="Percentage">
            <xsl:choose>
              <xsl:when test="$TotalCost > 0">
                <xsl:value-of select="number($EstimatedOperatorCost) div number($TotalCost)" />
              </xsl:when>
              <xsl:otherwise>0</xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:call-template name="round">
            <xsl:with-param name="value" select="$EstimatedOperatorCost" />
          </xsl:call-template> (<xsl:value-of select="format-number($Percentage, '0%')" />)
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@EstimateIO" />
        <xsl:with-param name="Label">Estimated I/O Cost</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:call-template name="round">
            <xsl:with-param name="value" select="@EstimateIO" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@EstimateCPU" />
        <xsl:with-param name="Label">Estimated CPU Cost</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:call-template name="round">
            <xsl:with-param name="value" select="@EstimateCPU" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@StatementSubTreeCost | @EstimatedTotalSubtreeCost" />
        <xsl:with-param name="Label">Estimated Subtree Cost</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:call-template name="round">
            <xsl:with-param name="value" select="@StatementSubTreeCost | @EstimatedTotalSubtreeCost" />
          </xsl:call-template>
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Estimated Number of Executions</xsl:with-param>
        <xsl:with-param name="Value" select="@EstimateRebinds + 1" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Number of Executions</xsl:with-param>
        <xsl:with-param name="Value" select="sum(s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualExecutions)" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Degree of Parallelism</xsl:with-param>
        <xsl:with-param name="Value" select="s:QueryPlan/@DegreeOfParallelism" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Memory Grant</xsl:with-param>
        <xsl:with-param name="Value" select="s:QueryPlan/@MemoryGrant" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Estimated Number of Rows to be Read</xsl:with-param>
        <xsl:with-param name="Value" select="@EstimatedRowsRead" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Estimated Number of Rows</xsl:with-param>
        <xsl:with-param name="Value" select="@StatementEstRows | @EstimateRows" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="@AvgRowSize" />
        <xsl:with-param name="Label">Estimated Row Size</xsl:with-param>
        <xsl:with-param name="Value" select="concat(@AvgRowSize, ' B')" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:RunTimeInformation" />
        <xsl:with-param name="Label">Actual Rebinds</xsl:with-param>
        <xsl:with-param name="Value" select="sum(s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualRebinds)" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:RunTimeInformation" />
        <xsl:with-param name="Label">Actual Rewinds</xsl:with-param>
        <xsl:with-param name="Value" select="sum(s:RunTimeInformation/s:RunTimeCountersPerThread/@ActualRewinds)" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Condition" select="s:IndexScan/@Ordered" />
        <xsl:with-param name="Label">Ordered</xsl:with-param>
        <xsl:with-param name="Value">
          <xsl:choose>
            <xsl:when test="s:IndexScan/@Ordered = 'true'">True</xsl:when>
            <xsl:when test="s:IndexScan/@Ordered = 1">True</xsl:when>
            <xsl:otherwise>False</xsl:otherwise>
          </xsl:choose>
        </xsl:with-param>
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Partitioning Type</xsl:with-param>
        <xsl:with-param name="Value" select="s:Parallelism/@PartitioningType" />
      </xsl:call-template>

      <xsl:call-template name="ToolTipRow">
        <xsl:with-param name="Label">Node ID</xsl:with-param>
        <xsl:with-param name="Value" select="@NodeId" />
      </xsl:call-template>

    </table>
  </xsl:template>

  <!-- Gets the Physical Operation -->
  <xsl:template match="s:RelOp" mode="PhysicalOperation">
    <xsl:value-of select="@PhysicalOp" />
  </xsl:template>
  <xsl:template match="s:RelOp[s:IndexScan/@Lookup]" mode="PhysicalOperation">Key Lookup</xsl:template>

  <!-- Gets the Logical Operation -->
  <xsl:template match="s:RelOp" mode="LogicalOperation">
    <xsl:value-of select="@LogicalOp" />
  </xsl:template>
  <xsl:template match="s:RelOp[s:IndexScan/@Lookup]" mode="LogicalOperation">Key Lookup</xsl:template>

  <!-- Calculates the estimated operator cost. -->
  <xsl:template name="EstimatedOperatorCost">
    <xsl:variable name="EstimatedTotalSubtreeCost">
      <xsl:call-template name="convertSciToNumString">
        <xsl:with-param name="inputVal">
          <xsl:choose>
            <xsl:when test="@EstimatedTotalSubtreeCost">
              <xsl:value-of select="@EstimatedTotalSubtreeCost" />
            </xsl:when>
            <xsl:otherwise>0</xsl:otherwise>
          </xsl:choose>
        </xsl:with-param>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="ChildEstimatedSubtreeCost">
      <xsl:for-each select="*/s:RelOp">
        <value>
          <xsl:call-template name="convertSciToNumString">
            <xsl:with-param name="inputVal" select="@EstimatedTotalSubtreeCost" />
          </xsl:call-template>
        </value>
      </xsl:for-each>
    </xsl:variable>
    <xsl:variable name="TotalChildEstimatedSubtreeCost">
      <xsl:choose>
        <xsl:when test="function-available('exslt:node-set')">
          <xsl:value-of select='sum(exslt:node-set($ChildEstimatedSubtreeCost)/value)' />
        </xsl:when>
        <xsl:when test="function-available('msxsl:node-set')">
          <xsl:value-of select='sum(msxsl:node-set($ChildEstimatedSubtreeCost)/value)' />
        </xsl:when>
      </xsl:choose>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="number($EstimatedTotalSubtreeCost) - number($TotalChildEstimatedSubtreeCost) &lt; 0">0</xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="number($EstimatedTotalSubtreeCost) - number($TotalChildEstimatedSubtreeCost)" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Renders a row in the tool tip details table. -->
  <xsl:template name="ToolTipRow">
    <xsl:param name="Label" />
    <xsl:param name="Value" />
    <xsl:param name="Condition" select="$Value" />
    <xsl:if test="$Condition">
      <tr>
        <th>
          <xsl:value-of select="$Label" />
        </th>
        <td>
          <xsl:value-of select="$Value" />
        </td>
      </tr>
    </xsl:if>
  </xsl:template>

  <!-- Prints the name of an object. -->
  <xsl:template match="s:Object | s:ColumnReference" mode="ObjectName">
    <xsl:param name="ExcludeDatabaseName" select="false()" />
    <xsl:choose>
      <xsl:when test="$ExcludeDatabaseName">
        <xsl:for-each select="@Table | @Index | @Column | @Alias">
          <xsl:value-of select="." />
          <xsl:if test="position() != last()">.</xsl:if>
        </xsl:for-each>
      </xsl:when>
      <xsl:otherwise>
        <xsl:for-each select="@Database | @Schema | @Table | @Index | @Column | @Alias">
          <xsl:value-of select="." />
          <xsl:if test="position() != last()">.</xsl:if>
        </xsl:for-each>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="s:Object | s:ColumnReference" mode="ObjectNameNoAlias">
    <xsl:for-each select="@Database | @Schema | @Table | @Index | @Column">
      <xsl:value-of select="." />
      <xsl:if test="position() != last()">.</xsl:if>
    </xsl:for-each>
  </xsl:template>

  <!-- Displays the node cost label. -->
  <xsl:template match="s:RelOp" mode="NodeCostLabel">
    <xsl:variable name="EstimatedOperatorCost">
      <xsl:call-template name="EstimatedOperatorCost" />
    </xsl:variable>
    <xsl:variable name="TotalCost">
      <xsl:value-of select="ancestor::s:QueryPlan/s:RelOp/@EstimatedTotalSubtreeCost" />
    </xsl:variable>
    <div>
      Cost: <xsl:value-of select="format-number(number($EstimatedOperatorCost) div number($TotalCost), '0%')" />
    </div>
  </xsl:template>

  <!-- Dont show the node cost for statements. -->
  <xsl:template match="s:StmtSimple|s:StmtUseDb" mode="NodeCostLabel" />

  <xsl:template match="s:StmtCursor|s:Operation|s:StmtCond" mode="NodeCostLabel">
    <div>Cost: 0%</div>
  </xsl:template>

  <!-- 
  ================================
  Tool tip detail sections
  ================================
  The following section contains templates used for writing the detail sections at the bottom of the tool tip,
  for example listing outputs, or information about the object to which an operator applies.
  -->

  <xsl:template match="*/s:Object" mode="ToolTipDetails">
    <!-- TODO: Make sure this works all the time -->
    <div class="qp-bold">Object</div>
    <div>
      <xsl:apply-templates select="." mode="ObjectName" />
    </div>
  </xsl:template>

  <xsl:template match="s:SetPredicate[s:ScalarOperator/@ScalarString]" mode="ToolTipDetails">
    <div class="qp-bold">Predicate</div>
    <div>
      <xsl:value-of select="s:ScalarOperator/@ScalarString" />
    </div>
  </xsl:template>

  <xsl:template match="s:Predicate[s:ScalarOperator/@ScalarString]" mode="ToolTipDetails">
    <div class="qp-bold">Predicate</div>
    <div>
      <xsl:value-of select="s:ScalarOperator/@ScalarString" />
    </div>
  </xsl:template>

  <xsl:template match="s:TopExpression[s:ScalarOperator/@ScalarString]" mode="ToolTipDetails">
    <div class="qp-bold">Top Expression</div>
    <div>
      <xsl:value-of select="s:ScalarOperator/@ScalarString" />
    </div>
  </xsl:template>

  <xsl:template match="s:OutputList[count(s:ColumnReference) > 0]" mode="ToolTipDetails">
    <div class="qp-bold">Output List</div>
    <xsl:for-each select="s:ColumnReference">
      <div>
        <xsl:apply-templates select="." mode="ObjectName" />
      </div>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="s:AdaptiveJoin/s:HashKeysProbe" mode="ToolTipDetails">
    <div class="qp-bold">Hash Keys Probe</div>
    <xsl:for-each select="s:ColumnReference">
      <div>
        <xsl:apply-templates select="." mode="ObjectNameNoAlias" />
      </div>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="s:AdaptiveJoin/s:OuterReferences" mode="ToolTipDetails">
    <div class="qp-bold">Outer References</div>
    <xsl:for-each select="s:ColumnReference">
      <div>
        <xsl:apply-templates select="." mode="ObjectNameNoAlias" />
      </div>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="s:NestedLoops/s:OuterReferences[count(s:ColumnReference) > 0]" mode="ToolTipDetails">
    <div class="qp-bold">Outer References</div>
    <xsl:for-each select="s:ColumnReference">
      <div>
        <xsl:apply-templates select="." mode="ObjectName" />
      </div>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="@StatementText" mode="ToolTipDetails">
    <div class="qp-bold">Statement</div>
    <div>
      <xsl:value-of select="." />
    </div>
  </xsl:template>

  <xsl:template match="s:StmtSimple/s:StoredProc" mode="ToolTipDetails">
    <div class="qp-bold">Procedure Name</div>
    <div>
      <xsl:value-of select="@ProcName" />
    </div>
  </xsl:template>

  <xsl:template match="s:Sort/s:OrderBy[count(s:OrderByColumn/s:ColumnReference) > 0]" mode="ToolTipDetails">
    <div class="qp-bold">Order By</div>
    <xsl:for-each select="s:OrderByColumn">
      <div>
        <xsl:apply-templates select="s:ColumnReference" mode="ObjectName" />
        <xsl:choose>
          <xsl:when test="@Ascending = 'true'"> Ascending</xsl:when>
          <xsl:when test="@Ascending = 1"> Ascending</xsl:when>
          <xsl:otherwise> Descending</xsl:otherwise>
        </xsl:choose>
      </div>
    </xsl:for-each>
  </xsl:template>

  <!-- 
  Seek Predicates Tooltip
  -->

  <xsl:template match="s:SeekPredicates" mode="ToolTipDetails">
    <div class="qp-bold">Seek Predicates</div>
    <div>
      <xsl:for-each select="s:SeekPredicateNew/s:SeekKeys">
        <xsl:call-template name="SeekKeyDetail">
          <xsl:with-param name="position" select="position()" />
        </xsl:call-template>
        <xsl:if test="position() != last()">, </xsl:if>
      </xsl:for-each>
    </div>
  </xsl:template>

  <!-- Turns out using apply-templates for this was a bad idea, but its too big a change to fix it all right now. This is probably
  the template that should contain tooltip details, I'll try to refactor more stuff into here over time, maybe. -->
  <xsl:template name="ToolTipDetails">
    <xsl:variable name="relop" select="." />
    <xsl:if test="s:Warnings">
      <div class="qp-bold">Warnings</div>
      <div>
        <xsl:if test="s:Warnings/@NoJoinPredicate=1 or s:Warnings/@NoJoinPredicate=true">
          <div>No Join Predicate</div>
        </xsl:if>
        <xsl:for-each select="s:UnmatchedIndexes/s:Parameterization/s:Object">
          <div>
            Unmatched index: <xsl:apply-templates select="." mode="ObjectNameNoAlias" />
          </div>
        </xsl:for-each>
        <xsl:for-each select="s:Warnings/s:SpillToTempDb">
          <div>
            Operator used tempdb to spill data during execution with spill level <xsl:value-of select="@SpillLevel" /> and <xsl:value-of select="@SpilledThreadCount" /> spilled thread(s)
          </div>
        </xsl:for-each>
        <xsl:for-each select="s:Warnings/s:ColumnsWithNoStatistics/s:ColumnReference">
          <div>
            Columns With No Statistics: <xsl:apply-templates select="." mode="ObjectNameNoAlias" />
          </div>
        </xsl:for-each>
        <xsl:for-each select="s:Warnings/s:Wait">
          <div>
            The query had to wait <xsl:value-of select="@WaitTime" /> seconds for <xsl:value-of select="@WaitType" /> during execution.
          </div>
        </xsl:for-each>
        <xsl:for-each select="s:Warnings/s:PlanAffectingConvert">
          <div>
            Type conversion in expression (<xsl:value-of select="@Expression" />) may affect "<xsl:value-of select="@ConvertIssue" />" in query plan choice.
          </div>
        </xsl:for-each>
        <xsl:for-each select="s:Warnings/s:SortSpillDetails">
          <div>
            <xsl:value-of select="$relop/@LogicalOp" /> wrote <xsl:value-of select="@WritesToTempDb" /> pages to and read <xsl:value-of select="@ReadsFromTempDb" /> pages from tempdb with granted memory <xsl:value-of select="@GrantedMemoryKb" />KB and used memory <xsl:value-of select="@UsedMemoryKb" />KB.
          </div>
        </xsl:for-each>
        <xsl:for-each select="s:Warnings/s:MemoryGrantWarning">
          <div>
            The query memory grant detected "<xsl:value-of select="@GrantWarningKind" />", which may impact the reliability. Grant size: Initial <xsl:value-of select="@RequestedMemory" /> KB, Final <xsl:value-of select="@GrantedMemory" /> KB, Used <xsl:value-of select="@MaxUsedMemory" /> KB.
          </div>
        </xsl:for-each>
        <xsl:for-each select="s:Warnings/s:HashSpillDetails">
          <div>
            Hash wrote <xsl:value-of select="@WritesToTempDb" /> pages to and read <xsl:value-of select="@ReadsFromTempDb" /> pages from tempdb with granted memory <xsl:value-of select="@GrantedMemoryKb" />KB and used memory <xsl:value-of select="@UsedMemoryKb" />KB.
          </div>
        </xsl:for-each>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template name="SeekKeyDetail">
    <xsl:param name="position" />Seek Keys[<xsl:value-of select="$position" />]: <xsl:for-each select="s:Prefix|s:StartRange|s:EndRange">
      <xsl:choose>
        <xsl:when test="self::s:Prefix">Prefix: </xsl:when>
        <xsl:when test="self::s:StartRange">Start: </xsl:when>
        <xsl:when test="self::s:EndRange">End: </xsl:when>
      </xsl:choose>
      <xsl:for-each select="s:RangeColumns/s:ColumnReference">
        <xsl:apply-templates select="." mode="ObjectNameNoAlias" />
        <xsl:if test="position() != last()">, </xsl:if>
      </xsl:for-each>
      <xsl:choose>
        <xsl:when test="@ScanType = 'EQ'"> = </xsl:when>
        <xsl:when test="@ScanType = 'LT'"> &lt; </xsl:when>
        <xsl:when test="@ScanType = 'GT'"> > </xsl:when>
        <xsl:when test="@ScanType = 'LE'"> &lt;= </xsl:when>
        <xsl:when test="@ScanType = 'GE'"> >= </xsl:when>
      </xsl:choose>
      <xsl:for-each select="s:RangeExpressions/s:ScalarOperator">
        Scalar Operator(<xsl:value-of select="@ScalarString" />)<xsl:if test="position() != last()">, </xsl:if>
      </xsl:for-each>
      <xsl:if test="position() != last()">, </xsl:if>
    </xsl:for-each>
  </xsl:template>

  <xsl:template name="NodeIcon">
    <xsl:variable name="iconName">
      <xsl:choose>
        <!-- Use the logical operation to determine the icon for the "Parallelism" operators. -->
        <xsl:when test="@PhysicalOp = 'Parallelism'">
          <xsl:value-of select="translate(@LogicalOp, ' ', '')" />
        </xsl:when>
        <xsl:when test="s:CursorPlan/@CursorActualType">
          <xsl:value-of select="s:CursorPlan/@CursorActualType" />
        </xsl:when>
        <xsl:when test="@OperationType">
          <xsl:value-of select="@OperationType" />
        </xsl:when>
        <xsl:when test="s:IndexScan/@Lookup">KeyLookup</xsl:when>
        <xsl:when test="s:TableValuedFunction">TableValuedFunction</xsl:when>
        <!-- Use the physical operation to determine icon if it is present. -->
        <xsl:when test="@PhysicalOp">
          <xsl:value-of select="translate(@PhysicalOp, ' ', '')" />
        </xsl:when>
        <!-- Matches all statements. -->
        <xsl:when test="local-name() = 'StmtSimple'">Statement</xsl:when>
        <xsl:when test="local-name() = 'StmtCursor'">StmtCursor</xsl:when>
        <!-- Fallback - show the Bitmap icon. -->
        <xsl:otherwise>Catchall</xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:element name="div">
      <xsl:attribute name="class">
        qp-icon-<xsl:value-of select="$iconName" />
      </xsl:attribute>
      <xsl:if test="s:Warnings or s:QueryPlan/s:Warnings">
        <div class="qp-iconwarn" />
      </xsl:if>
      <xsl:if test="@Parallel='1' or @Parallel='true'">
        <div class="qp-iconpar" />
      </xsl:if>
    </xsl:element>
  </xsl:template>

  <!-- 
  ================================
  Node labels
  ================================
  The following section contains templates used to determine the first (main) label for a node.
  -->

  <xsl:template match="s:RelOp[s:IndexScan]" mode="NodeLabel">
    <xsl:choose>
      <xsl:when test="s:IndexScan/@Storage='ColumnStore'">Columnstore Index Scan</xsl:when>
      <xsl:when test="s:IndexScan/@Lookup and s:IndexScan/s:Object/@IndexKind='Clustered'">Key Lookup</xsl:when>
      <xsl:when test="s:IndexScan/@Lookup">RID Lookup</xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@PhysicalOp" />
      </xsl:otherwise>
    </xsl:choose>
    <xsl:if test="s:IndexScan/s:Object/@IndexKind">
      (<xsl:value-of select="s:IndexScan/s:Object/@IndexKind" />)
    </xsl:if>
  </xsl:template>

  <xsl:template match="s:RelOp" mode="NodeLabel">
    <xsl:value-of select="@PhysicalOp" />
  </xsl:template>

  <xsl:template match="*[@StatementType]" mode="NodeLabel">
    <xsl:value-of select="@StatementType" />
  </xsl:template>

  <xsl:template match="*[s:StoredProc]" mode="NodeLabel">Stored Procedure</xsl:template>

  <xsl:template match="*[s:CursorPlan/@CursorActualType = 'Dynamic']" mode="NodeLabel">Dynamic</xsl:template>
  <xsl:template match="*[s:CursorPlan/@CursorActualType = 'FastForward']" mode="NodeLabel">Fast Forward</xsl:template>
  <xsl:template match="*[s:CursorPlan/@CursorActualType = 'Keyset']" mode="NodeLabel">Keyset</xsl:template>
  <xsl:template match="*[s:CursorPlan/@CursorActualType = 'SnapShot']" mode="NodeLabel">Snapshot</xsl:template>

  <xsl:template match="*[@OperationType = 'FetchQuery']" mode="NodeLabel">Fetch Query</xsl:template>
  <xsl:template match="*[@OperationType = 'PopulateQuery']" mode="NodeLabel">Population Query</xsl:template>
  <xsl:template match="*[@OperationType = 'RefreshQuery']" mode="NodeLabel">Refresh Query</xsl:template>

  <!--
  ================================
  Node alternate labels
  ================================
  The following section contains templates used to determine the second label to be displayed for a node.
  -->

  <!-- Display the object for any node that has one -->
  <xsl:template match="*[*/s:Object]" mode="NodeLabel2">
    <xsl:variable name="ObjectName">
      <xsl:apply-templates select="*/s:Object" mode="ObjectName">
        <xsl:with-param name="ExcludeDatabaseName" select="true()" />
      </xsl:apply-templates>
    </xsl:variable>
    <div>
      <xsl:value-of select="substring($ObjectName, 0, 36)" />
      <xsl:if test="string-length($ObjectName) >= 36">…</xsl:if>
    </div>
  </xsl:template>

  <!-- Display the logical operation for any node where it is not the same as the physical operation. -->
  <xsl:template match="s:RelOp[@LogicalOp != @PhysicalOp]" mode="NodeLabel2">
    <div>
      (<xsl:value-of select="@LogicalOp" />)
    </div>
  </xsl:template>

  <!-- Disable the default template -->
  <xsl:template match="*" mode="NodeLabel2" />

  <!-- 
  ================================
  Tool tip descriptions
  ================================
  The following section contains templates used for writing the description shown in the tool tip.
  -->

  <xsl:template match="*[@PhysicalOp = 'Table Insert']" mode="ToolTipDescription">Insert input rows into the table specified in Argument field.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Compute Scalar']" mode="ToolTipDescription">Compute new values from existing values in a row.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Sort']" mode="ToolTipDescription">Sort the input.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Clustered Index Scan']" mode="ToolTipDescription">Scanning a clustered index, entirely or only a range.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Stream Aggregate']" mode="ToolTipDescription">Compute summary values for groups of rows in a suitably sorted stream.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Hash Match']" mode="ToolTipDescription">Use each row from the top input to build a hash table, and each row from the bottom input to probe into the hash table, outputting all matching rows.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Bitmap']" mode="ToolTipDescription">Bitmap.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Clustered Index Seek']" mode="ToolTipDescription">Scanning a particular range of rows from a clustered index.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Index Seek']" mode="ToolTipDescription">Scan a particular range of rows from a nonclustered index.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Adaptive Join']" mode="ToolTipDescription">Chooses dynamically between hash join and nested loops.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Index Spool']" mode="ToolTipDescription">Reformats the data from the input into a temporary index, which is then used for seeking with the supplied seek predicate.</xsl:template>
  <xsl:template match="*[s:IndexScan/@Lookup]" mode="ToolTipDescription">Uses a supplied clustering key to lookup on a table that has a clustered index.</xsl:template>

  <xsl:template match="*[@PhysicalOp = 'Parallelism' and @LogicalOp='Repartition Streams']" mode="ToolTipDescription">Repartition Streams.</xsl:template>
  <xsl:template match="*[@PhysicalOp = 'Parallelism']" mode="ToolTipDescription">An operation involving parallelism.</xsl:template>

  <xsl:template match="*[s:TableScan]" mode="ToolTipDescription">Scan rows from a table.</xsl:template>
  <xsl:template match="*[s:NestedLoops]" mode="ToolTipDescription">For each row in the top (outer) input, scan the bottom (inner) input, and output matching rows.</xsl:template>
  <xsl:template match="*[s:Top]" mode="ToolTipDescription">Select the first few rows based on a sort order.</xsl:template>

  <xsl:template match="*[@OperationType='FetchQuery']" mode="ToolTipDescription">The query used to retrieve rows when a fetch is issued against a cursor.</xsl:template>
  <xsl:template match="*[@OperationType='PopulateQuery']" mode="ToolTipDescription">The query used to populate a cursor's work table when the cursor is opened.</xsl:template>
  <xsl:template match="*[s:CursorPlan/@CursorActualType='FastForward']" mode="ToolTipDescription">Fast Forward.</xsl:template>
  <xsl:template match="*[s:CursorPlan/@CursorActualType='Dynamic']" mode="ToolTipDescription">Cursor that can see all changes made by others.</xsl:template>
  <xsl:template match="*[s:CursorPlan/@CursorActualType='Keyset']" mode="ToolTipDescription">Cursor that can see updates made by others, but not inserts.</xsl:template>
  <xsl:template match="*[s:CursorPlan/@CursorActualType='SnapShot']" mode="ToolTipDescription">A cursor that does not see changes made by others.</xsl:template>

  <!-- 
  ================================
  Number handling
  ================================
  The following section contains templates used for handling numbers (scientific notation, rounding etc...)
  -->

  <!-- Outputs a number rounded to 7 decimal places - to be used for displaying all numbers.
  This template accepts numbers in scientific notation. -->
  <xsl:template name="round">
    <xsl:param name="value" select="0" />
    <xsl:variable name="number">
      <xsl:call-template name="convertSciToNumString">
        <xsl:with-param name="inputVal" select="$value" />
      </xsl:call-template>
    </xsl:variable>
    <xsl:value-of select="format-number(round(number($number) * 10000000) div 10000000, '0.#######')" />
  </xsl:template>

  <!-- Template for handling of scientific numbers
  See: http://www.orm-designer.com/article/xslt-convert-scientific-notation-to-decimal-number -->
  <xsl:variable name="max-exp">
    <xsl:value-of select="'0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000'" />
  </xsl:variable>

  <xsl:template name="convertSciToNumString">
    <xsl:param name="inputVal" select="0" />

    <xsl:variable name="numInput">
      <xsl:value-of select="translate(string($inputVal),'e','E')" />
    </xsl:variable>

    <xsl:choose>
      <xsl:when test="number($numInput) = $numInput">
        <xsl:value-of select="$numInput" />
      </xsl:when>
      <xsl:otherwise>
        <!-- ==== Mantisa ==== -->
        <xsl:variable name="numMantisa">
          <xsl:value-of select="number(substring-before($numInput,'E'))" />
        </xsl:variable>

        <!-- ==== Exponent ==== -->
        <xsl:variable name="numExponent">
          <xsl:choose>
            <xsl:when test="contains($numInput,'E+')">
              <xsl:value-of select="substring-after($numInput,'E+')" />
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="substring-after($numInput,'E')" />
            </xsl:otherwise>
          </xsl:choose>
        </xsl:variable>

        <!-- ==== Coefficient ==== -->
        <xsl:variable name="numCoefficient">
          <xsl:choose>
            <xsl:when test="$numExponent > 0">
              <xsl:text>1</xsl:text>
              <xsl:value-of select="substring($max-exp, 1, number($numExponent))" />
            </xsl:when>
            <xsl:when test="$numExponent &lt; 0">
              <xsl:text>0.</xsl:text>
              <xsl:value-of select="substring($max-exp, 1, -number($numExponent)-1)" />
              <xsl:text>1</xsl:text>
            </xsl:when>
            <xsl:otherwise>1</xsl:otherwise>
          </xsl:choose>
        </xsl:variable>
        <xsl:value-of select="number($numCoefficient) * number($numMantisa)" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
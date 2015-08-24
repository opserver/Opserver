using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<PerfCounterRecord>> _perfCounters;
        public Cache<List<PerfCounterRecord>> PerfCounters
        {
            get
            {
                return _perfCounters ?? (_perfCounters = new Cache<List<PerfCounterRecord>>
                    {
                        CacheForSeconds = 20,
                        UpdateCache = UpdateFromSql("PerfCounters", conn =>
                            {
                                var sql = GetFetchSQL<PerfCounterRecord>();
                                return conn.QueryAsync<PerfCounterRecord>(sql, new {maxEvents = 60});
                            })
                    });
            }
        }

        public PerfCounterRecord GetPerfCounter(string category, string name, string instance)
        {
            var counters = PerfCounters.SafeData();
            var objectName = ObjectName + ":" + category;
            return counters?.FirstOrDefault(c => c.ObjectName == objectName && c.CounterName == name && c.InstanceName == instance);
        }

        public class PerfCounterRecord : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2000.RTM;

            public string ObjectName { get; internal set; }
            public string CounterName { get; internal set; }
            public string InstanceName { get; internal set; }
            public long CurrentValue { get; internal set; }
            public decimal CalculatedValue { get; internal set; }
            public int Type { get; internal set; }

            internal string FetchSQL = @"
Declare @PCounters Table (object_name nvarchar(128),
                          counter_name nvarchar(128),
                          instance_name nvarchar(128),
                          cntr_value bigint,
                          cntr_type int,
                          Primary Key(object_name, counter_name, instance_name));

Insert Into @PCounters
Select RTrim(spi.object_name) object_name, RTrim(spi.counter_name) counter_name, RTrim(spi.instance_name) instance_name, spi.cntr_value, spi.cntr_type
  From sys.dm_os_performance_counters spi
 Where spi.instance_name Not In (Select name From sys.databases)
   And spi.object_name Not Like 'SQLServer:Backup Device%'

WAITFOR DELAY '00:00:01'

Declare @CCounters Table (object_name nvarchar(128),
                          counter_name nvarchar(128),
                          instance_name nvarchar(128),
                          cntr_value bigint,
                          cntr_type INT,
                          Primary Key(object_name, counter_name, instance_name));

Insert Into @CCounters
Select RTrim(spi.object_name) object_name, RTrim(spi.counter_name) counter_name, RTrim(spi.instance_name) instance_name, spi.cntr_value, spi.cntr_type
  From sys.dm_os_performance_counters spi
 Where spi.instance_name Not In (Select name From sys.databases)
   And spi.object_name Not Like 'SQLServer:Backup Device%'

Select cc.object_name ObjectName,
       cc.counter_name CounterName,
       cc.instance_name InstanceName,
       cc.cntr_value CurrentValue,
       (Case cc.cntr_type 
        When 65792 Then cc.cntr_value -- Count
        When 537003264 Then IsNull(Cast(cc.cntr_value as Money) / NullIf(cbc.cntr_value, 0), 0) -- Ratio
        When 272696576 Then cc.cntr_value - pc.cntr_value -- Per Second
        When 1073874176 Then IsNull(Cast(cc.cntr_value - pc.cntr_value as Money) / NullIf(cbc.cntr_value - pbc.cntr_value, 0), 0) -- Avg
        When 1073939712 Then cc.cntr_value - pc.cntr_value -- Base
        Else cc.cntr_value
        End) CalculatedValue,
       cc.cntr_type Type
  From @CCounters cc
       Left Join @CCounters cbc
         On cc.object_name = cbc.object_name
        And (Case When cc.counter_name Like '%(ms)' Then Replace(cc.counter_name, ' (ms)',' Base')
                  When cc.object_name = 'SQLServer:FileTable' Then Replace(cc.counter_name, 'Avg ','') + ' base'
                  When cc.counter_name = 'Worktables From Cache Ratio' Then 'Worktables From Cache Base'
                  When cc.counter_name = 'Avg. Length of Batched Writes' Then 'Avg. Length of Batched Writes BS'
                  Else cc.counter_name + ' base' 
             End) = cbc.counter_name
        And cc.instance_name = cbc.instance_name
        And cc.cntr_type In (537003264, 1073874176)
        And cbc.cntr_type = 1073939712
       Join @PCounters pc 
         On cc.object_name = pc.object_name
        And cc.counter_name = pc.counter_name
        And cc.instance_name = pc.instance_name
        And cc.cntr_type = pc.cntr_type
       Left Join @PCounters pbc
         On pc.object_name = pbc.object_name
        And pc.instance_name = pbc.instance_name
        And (Case When pc.counter_name Like '%(ms)' Then Replace(pc.counter_name, ' (ms)',' Base')
                  When pc.object_name = 'SQLServer:FileTable' Then Replace(pc.counter_name, 'Avg ','') + ' base'
                  When pc.counter_name = 'Worktables From Cache Ratio' Then 'Worktables From Cache Base'
                  When pc.counter_name = 'Avg. Length of Batched Writes' Then 'Avg. Length of Batched Writes BS'
                  Else pc.counter_name + ' base' 
             End) = pbc.counter_name
        And pc.cntr_type In (537003264, 1073874176)
        And pbc.cntr_type = 1073939712";

            public string GetFetchSQL(Version v)
            {
                if (v < SQLServerVersions.SQL2005.RTM)
                {
                    return FetchSQL.Replace("dm_os_performance_counters", "sysperfinfo");
                }
                return FetchSQL;
            }
        }
    }
}

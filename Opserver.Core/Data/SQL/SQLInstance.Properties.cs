using System;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<SQLServerProperties> _serverProperties;
        public Cache<SQLServerProperties> ServerProperties
        {
            get
            {
                return _serverProperties ?? (_serverProperties = new Cache<SQLServerProperties>
                    {
                        CacheForSeconds = 60,
                        UpdateCache = UpdateFromSql("Properties", async conn =>
                            {
                                var result = (await conn.QueryAsync<SQLServerProperties>(SQLServerProperties.FetchSQL)).FirstOrDefault();
                                if (result != null)
                                {
                                    Version = result.ParsedVersion;
                                    if (result.PhysicalMemoryBytes > 0)
                                    {
                                        CurrentMemoryPercent = result.CommittedBytes/(decimal) result.PhysicalMemoryBytes*100;
                                    }
                                }
                                return result;
                            })
                    });
            }
        }

        public decimal? CurrentMemoryPercent { get; set; }

        public class SQLServerProperties
        {
            public string Version { get; internal set; }
            public string FullVersion { get; internal set; }
            public string Level { get; internal set; }
            public string Edition { get; internal set; }
            public string Collation { get; internal set; }
            public string BuildClrVersion { get; internal set; }
            public string InstanceName { get; internal set; }
            public FullTextInstallStatus? IsFullTextInstalled { get; internal set; }
            public HADREnabledStatus? IsHadrEnabled { get; internal set; }
            public HADRManagerStatus? HadrManagerStatus { get; internal set; }
            public string MachineName { get; internal set; }
            public string ServerName { get; internal set; }
            public int ProcessID { get; internal set; }
            public int SessionCount { get; internal set; }
            public int ConnectionCount { get; internal set; }
            public int JobCount { get; internal set; }
            public int CPUCount { get; internal set; }
            public int HyperthreadRatio { get; internal set; }
            public long PhysicalMemoryBytes { get; internal set; }
            public long VirtualMemoryBytes { get; internal set; }
            public long CommittedBytes { get; internal set; }
            public long CommittedTargetBytes { get; internal set; }
            public long StackSizeBytes { get; internal set; }
            public int CurrentWorkerCount { get; internal set; }
            public int MaxWorkersCount { get; internal set; }
            public int SchedulerCount { get; internal set; }
            public int SchedulerTotalCount { get; internal set; }
            public DateTime SQLServerStartTime { get; internal set; }
            public VirtualMachineTypes VirtualMachineType { get; internal set; }

            public int CPUSocketCount => CPUCount/HyperthreadRatio;

            private Version _version;
            public Version ParsedVersion => _version ?? (_version = Version != null ? System.Version.Parse(Version) : new Version(0, 0));

            public string MajorVersion
            {
                get
                {
                    if (Version.HasValue())
                    {
                        if (Version.StartsWith("12.")) return "SQL 2014";
                        if (Version.StartsWith("11.")) return "SQL 2012";
                        if (Version.StartsWith("10.5")) return "SQL 2008 R2";
                        if (Version.StartsWith("10.25")) return "SQL Azure";
                        if (Version.StartsWith("10.")) return "SQL 2008";
                        if (Version.StartsWith("9.")) return "SQL 2005";
                        if (Version.StartsWith("8.")) return "SQL 2000";
                    }
                    return Version.HasValue() ? "Unknown: " + Version : "Unknown";
                }
            }

            internal const string FetchSQL = @"Declare @sql nvarchar(4000);
Set @sql = '
Select Cast(SERVERPROPERTY(''ProductVersion'') as nvarchar(128)) Version,
       @@VERSION FullVersion,
       Cast(SERVERPROPERTY(''ProductLevel'') as nvarchar(128)) Level,
       Cast(SERVERPROPERTY(''Edition'') as nvarchar(128)) Edition,
       Cast(SERVERPROPERTY(''Collation'') as nvarchar(128)) Collation,
       Cast(SERVERPROPERTY(''BuildClrVersion'') as nvarchar(128)) BuildClrVersion,
       Cast(SERVERPROPERTY(''InstanceName'') as nvarchar(128)) InstanceName,
       Cast(SERVERPROPERTY(''IsFullTextInstalled'') as int) IsFullTextInstalled,
       Cast(SERVERPROPERTY(''IsHadrEnabled'') as int) IsHadrEnabled,
       Cast(SERVERPROPERTY(''HadrManagerStatus'') as int) HadrManagerStatus,
       Cast(SERVERPROPERTY(''MachineName'') as nvarchar(128)) MachineName,
       Cast(SERVERPROPERTY(''ServerName'') as nvarchar(128)) ServerName,
       Cast(SERVERPROPERTY(''ProcessID'') as int) ProcessID,
       (Select Count(*) From sys.dm_exec_sessions) SessionCount,
       (Select Count(*) From sys.dm_exec_connections) ConnectionCount,
       (Select Sum(current_workers_count) From sys.dm_os_schedulers) CurrentWorkerCount,
       (Select Count(*) From msdb.dbo.sysjobs) JobCount,
       cpu_count CPUCount,
       hyperthread_ratio HyperthreadRatio,
       stack_size_in_bytes StackSizeBytes,
       max_workers_count MaxWorkersCount,
       scheduler_count SchedulerCount,
       scheduler_total_count SchedulerTotalCount,'
       
If (SELECT @@MICROSOFTVERSION / 0x01000000) >= 10
	Set @sql = @sql + '
       sqlserver_start_time SQLServerStartTime,';

If (SELECT @@MICROSOFTVERSION / 0x01000000) >= 11
    Set @sql = @sql + '
	   virtual_machine_type VirtualMachineType,	  
       Cast(physical_memory_kb as bigint) * 1024 PhysicalMemoryBytes,
       Cast(virtual_memory_kb as bigint) * 1024 VirtualMemoryBytes,
       Cast(committed_kb as bigint) * 1024 CommittedBytes,
       Cast(committed_target_kb as bigint) * 1024 CommittedTargetBytes';
Else 
    Set @sql = @sql + '
       null VirtualMachineType,
       physical_memory_in_bytes PhysicalMemoryBytes,
       virtual_memory_in_bytes VirtualMemoryBytes,
       Cast(bpool_committed as bigint) * 8 * 1024 CommittedBytes,
       Cast(bpool_commit_target as bigint) * 8 * 1024 CommittedTargetBytes';
Exec (@sql + ' 
  From sys.dm_os_sys_info');";
        }
    }
}

using System.ComponentModel;

namespace Opserver.Data.SQL
{
    public enum AutomatedBackupPreferences : byte
    {
        [Description("Primary")] Primary = 0,
        [Description("Secondary")] Secondary = 1,
        [Description("Prefer Secondary")] PreferSecondary = 2,
        [Description("Any Replica")] AnyReplica = 3
    }

    public enum AvailabilityModes : byte
    {
        [Description("Asynchronous Commit")] AsynchronousCommit = 0,
        [Description("Syncrhonous Commit")] SynchronousCommit = 1
    }

    public enum QuorumTypes : byte
    {
        [Description("Node Majority")] NodeMajority = 0,
        [Description("Node and Disk Majority")] NodeAndDiskMajority = 1,
        [Description("Node and File Share Majority")] NodeAndFileShareMajority = 2,
        [Description("No Majority")] NoMajority = 3,
        [Description("Unknown")] Unknown = 4
    }

    public enum QuorumStates : byte
    {
        [Description("Unknown Quorum State")] Unknown = 0,
        [Description("Normal")] Normal = 1,
        [Description("Forced")] Forced = 2
    }

    public enum ClusterMemberStates : byte
    {
        [Description("Offline")] Offline = 0,
        [Description("Online")] Online = 1
    }

    public enum ClusterMemberTypes : byte
    {
        [Description("WSFC Node")] WSFCNode = 0,
        [Description("Disk Witness")] DiskWitness = 1,
        [Description("File Share Witness")] FileShareWitness = 2
    }

    public enum ConnectedStates : byte
    {
        [Description("Disconnected")] Disconnected = 0,
        [Description("Connected")] Connected = 1
    }

    public enum DatabaseStates : byte
    {
        [Description("Online")] Online = 0,
        [Description("Restoring")] Restoring = 1,
        [Description("Recovering")] Recovering = 2,
        [Description("Recovery Pending")] RecoveryPending = 3,
        [Description("Suspect")] Suspect = 4,
        [Description("Emergency")] Emergency = 5,
        [Description("Offline")] Offline = 6,
        [Description("Copying")] Copying = 7
    }

    public enum FailoverModes : byte
    {
        [Description("Manual")] Manual = 1,
        [Description("Automatic")] Automatic = 2
    }

    public enum JoinStates : byte
    {
        [Description("Not Joined")] NotJoined = 0,
        [Description("Joined (Standalone)")] JoinedStandalone = 1,
        [Description("Joined (Failover)")] JoinedFailover = 2
    }

    public enum ListenerStates : byte
    {
        [Description("Offline")] Offline = 0,
        [Description("Online")] Online = 1,
        [Description("Online Pending")] OnlinePending = 2,
        [Description("Failed")] Failed = 3
    }

    public enum OperationStates : byte
    {
        [Description("Pending Failover")] PendingFailover = 0,
        [Description("Pending")] Pending = 1,
        [Description("Online")] Online = 2,
        [Description("Offline")] Offline = 3,
        [Description("Failed")] Failed = 4,
        [Description("Failed (No Quorum)")] FailedNoQuorun = 5
        // null = not local
    }

    public enum PriRoleAllowConnections : byte
    {
        [Description("All")] All = 2,
        [Description("Read/Write")] ReadWrite = 3
    }

    public enum RecoveryHealths : byte
    {
        [Description("In Progress")] InProgress = 0,
        [Description("Online")] Online = 1
    }

    public enum ReplicaRoles : byte
    {
        [Description("Resolving")] Resolving = 0,
        [Description("Primary")] Primary = 1,
        [Description("Secondary")] Secondary = 2
    }

    public enum SecRoleAllowConnections : byte
    {
        [Description("No")] No = 0,
        [Description("Read Only")] ReadOnly = 1,
        [Description("All")] All = 2
    }

    public enum SuspendReasons : byte
    {
        [Description("User Action")] UserAction = 0,
        [Description("Suspend From Partner")] SuspendFromPartner = 1,
        [Description("Redo")] Redo = 2,
        [Description("Apply")] Apply = 3,
        [Description("Capture")] Capture = 4,
        [Description("Restart")] Restart = 5,
        [Description("Undo")] Undo = 6,
        [Description("Revalidation")] Revalidation = 7,
        [Description("Error in the calculation of the secondary replica synchronization point")] ErrorInSecondaryReplicaSyncPoint = 8
    }

    public enum SynchronizationHealths : byte
    {
        [Description("Not Healthy (None)")] NotHealthy = 0,
        [Description("Partially Healthy (Some)")] PartiallyHealthy = 1,
        [Description("Healthy")] Healthy = 2
    }

    public enum SynchronizationStates : byte
    {
        [Description("Not Synchronizing")] NotSynchronizing = 0,
        [Description("Synchronizing")] Synchronizing = 1,
        [Description("Synchronized")] Synchronized = 2,
        [Description("Reverting")] Reverting = 3,
        [Description("Initializing")] Initializing = 4
    }

    public enum TCPListenerStates : short
    {
        [Description("Online")] Online = 0,
        [Description("Pending Restart")] PendingRestart = 1
    }

    public enum TCPListenerTypes : short
    {
        [Description("Transact-SQL")] TransactSQL = 0,
        [Description("Service Broker")] ServiceBroker = 1,
        [Description("Database Mirroring")] DatabaseMirroring = 2
    }

    public enum CompatabilityLevels : byte
    {
        [Description("2008 & 2008 R2 (70)")] Level70 = 70,
        [Description("2008 & 2008 R2 (80)")] Level80 = 80,
        [Description("2008 - 2012 (90)")] Level90 = 90,
        [Description("2008 - 2012 (100)")] Level100 = 100,
        [Description("2012 Only (110)")] Level110 = 110
    }

    public enum RecoveryModels : byte
    {
        [Description("Full")] Full = 1,
        [Description("Bulk Logged")] BulkLogged = 2,
        [Description("Simple")] Simple = 3
    }

    public enum PageVerifyOptions : byte
    {
        [Description("None")] None = 0,
        [Description("Torn Page Detection")] TornPageDetection = 1,
        [Description("Checksum")] Checksum = 2
    }

    public enum LogReuseWaits : byte
    {
        [Description("None")] Nothing = 0,
        [Description("Checkpoint")] Checkpoint = 1,
        [Description("Log Backup")] LogBackup = 2,
        [Description("Active Backup or Restore")] ActiveBackupOrRestore = 3,
        [Description("Active Transaction")] ActiveTransaction = 4,
        [Description("Database Mirroring")] DatabaseMirroring = 5,
        [Description("replication")] Replication = 6,
        [Description("Database Snapshot Creation")] DatabaseSnapshotCreation = 7,
        [Description("Log Scan")] LogScan = 8,
        [Description("Always On Replication")] AlwaysOnReplicaTransactionLogs = 9,
        [Description("Internal Use")] Internal10 = 10,
        [Description("Internal Use")] Internal11 = 11,
        [Description("Internal Use")] Internal12 = 12,
        [Description("Internal Use")] OldestPage = 13,
        [Description("Other (transient)")] Other = 14
    }

    public enum UserAccesses : byte
    {
        [Description("Multi-User")] MultiUser = 0,
        [Description("Single User")] SingleUser = 1,
        [Description("Restricted User")] RestrictedUser = 2
    }

    public enum SnapshotIsolationStates : byte
    {
        [Description("Off")] Off = 0,
        [Description("On")] On = 1,
        [Description("On (Transitioning to Off)")] TransitioningToOff = 2,
        [Description("Off (Transitioning to On)")] TransitioningToOn = 3
    }

    public enum Containments : byte
    {
        [Description("Off")] Off = 0,
        [Description("Partial")] Partial = 1
    }

    public enum FullTextInstallStatus
    {
        [Description("Not Installed")] NotInstalled = 0,
        [Description("Installed")] Installed = 1
    }

    public enum HADREnabledStatus
    {
        [Description("Enabled")] Enabled = 0,
        [Description("Disabled")] Disabled = 1
    }

    public enum HADRManagerStatus
    {
        [Description("Not started, pending communication")] NotStarted = 0,
        [Description("Started and running")] Running = 1,
        [Description("Not started and failed")] Failed = 2
    }

    public enum MediaDeviceTypes : byte
    {
        [Description("Disk")] Disk = 2,
        [Description("Tape")] Tape = 4,
        [Description("Virtual Device")] VirtualDevice = 7,
        [Description("Permenant Device")] Permenant = 105
    }

    public enum VirtualMachineTypes
    {
        [Description("None")] None = 0,
        [Description("Hypervisor")] Hypervisor = 1,
        [Description("Other")] Other = 2
    }

    public enum ServiceStartupTypes
    {
        [Description("Other (0)")] Other0 = 0,
        [Description("Other (1)")] Other1 = 1,
        [Description("Automatic")] Automatic = 2,
        [Description("Manual")] Manual = 3,
        [Description("Disabled")] Disabled = 4
    }

    public enum ServiceStatuses
    {
        [Description("Stopped")] Stopped = 1,
        [Description("Start Pending")] StartPending = 2,
        [Description("Stop Pending")] StopPending = 3,
        [Description("Running")] Running = 4,
        [Description("Continue Pending")] ContinuePending = 5,
        [Description("Pause Pending")] PausePending = 6,
        [Description("Paused")] Paused = 7
    }

    public enum JobStatuses
    {
        [Description("Failed")] Failed = 0,
        [Description("Succeeded")] Succeeded = 1,
        [Description("Retry")] Retry = 2,
        [Description("Canceled")] Canceled = 3
    }

    public enum JobRunSources
    {
        [Description("Scheduler")] Scheduler = 1,
        [Description("Alerter")] Alerter = 2,
        [Description("Boot")] Boot = 3,
        [Description("User")] User = 4,
        [Description("On Idle Schedule")] OnIdleSchedule = 6
    }

    public enum TableTypes
    {
        [Description("Heap")] Heap = 0,
        [Description("Clustered")] Clustered = 1
    }

    public enum TransactionIsolationLevel : short
    {
        [Description("Unspecified")] Unspecified = 0,
        [Description("Uncommited")] ReadUncomitted = 1,
        [Description("Commited")] ReadCommitted = 2,
        [Description("Repeatable")] Repeatable = 3,
        [Description("Serializable")] Serializable = 4,
        [Description("Snapshot")] Snapshot = 5
    }

    public enum DatabaseFileTypes : byte
    {
        [Description("Rows")] Rows = 0,
        [Description("Log")] Log = 1,
        [Description("Reserved (2)")] Reserved2 = 2,
        [Description("Reserved (3)")] Reserved3 = 3,
        [Description("Full-text")] FullText = 4
    }

    public enum DatabaseFileStates : byte
    {
        [Description("Online")] Online = 0,
        [Description("Restoring")] Restoring = 1,
        [Description("Recovering")] Recovering = 2,
        [Description("Recovery Pending")] RecoveryPending = 3,
        [Description("Suspect")] Suspect = 4,
        [Description("Reserved (5)")] Reserved5 = 5,
        [Description("Offline")] Offline = 6,
        [Description("Defunct")] Defunct = 7
    }

    public enum IndexType : byte
    {
        [Description("Heap")] Heap = 0,
        [Description("Clustered")] Clustered = 1,
        [Description("Nonclustered")] Nonclustered = 2,
        [Description("XML")] XML = 3,
        [Description("Spatial")] Spatial = 4,
        [Description("Clustered Columnstore")] ClusteredColumnstore = 5,
        [Description("Nonclustered Columnstore")] NonclusteredColumnstore = 6,
        [Description("Nonclustered Hash")] NonclusteredHash = 7
    }

    public enum PartitionDataCompression : byte
    {
        [Description("None")] None = 0,
        [Description("Row")] Row = 1,
        [Description("Page")] Page = 2,
        [Description("Columnstore")] Columnstore = 3,
        [Description("Columnstore (Archive)")] ColumnstoreArchive = 4
    }
}

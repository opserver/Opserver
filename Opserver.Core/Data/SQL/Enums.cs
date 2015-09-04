using System.ComponentModel;

namespace StackExchange.Opserver.Data.SQL
{
    public enum AutomatedBackupPreferences : byte
    {
        Primary = 0,
        Secondary = 1,
        [Description("Prefer Secondary")] PreferSecondary = 2,
        [Description("Any Replica")] AnyReplica = 3
    }

    public enum AvailabilityModes : byte
    {
        [Description("Asyncrhonous Commit")] AsyncrhonousCommit = 0,
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
        Offline = 0,
        Online = 1
    }

    public enum ClusterMemberTypes : byte
    {
        [Description("WSFC Node")] WSFCNode = 0,
        [Description("Disk Witness")] DiskWitness = 1,
        [Description("File Share Witness")] FileShareWitness = 2
    }

    public enum ConnectedStates : byte
    {
        Disconnected = 0,
        Connected = 1
    }

    public enum DatabaseStates : byte
    {
        Online = 0,
        Restoring = 1,
        Recovering = 2,
        [Description("Recovery Pending")] RecoveryPending = 3,
        Suspect = 4,
        Emergency = 5,
        Offline = 6,
        Copying = 7
    }

    public enum FailoverModes : byte
    {
        Manual = 1,
        Automatic = 2
    }

    public enum JoinStates : byte
    {
        [Description("Not Joined")] NotJoined = 0,
        [Description("Joined (Standalone)")] JoinedStandalone = 1,
        [Description("Joined (Failover)")] JoinedFailover = 2
    }

    public enum ListenerStates : byte
    {
        Offline = 0,
        Online = 1,
        [Description("Online Pending")] OnlinePending = 2,
        Failed = 3
    }

    public enum OperationStates : byte
    {
        [Description("Pending Failover")] PendingFailover = 0,
        Pending = 1,
        Online = 2,
        Offline = 3,
        Failed = 4,
        [Description("Failed (No Quorum)")] FailedNoQuorun = 5
        // null = not local
    }

    public enum PriRoleAllowConnections : byte
    {
        All = 2,
        [Description("Read/Write")] ReadWrite = 3
    }

    public enum RecoveryHealths : byte
    {
        [Description("In Progress")] InProgress = 0,
        [Description("Online")] Online = 1
    }

    public enum ReplicaRoles : byte
    {
        Resolving = 0,
        Primary = 1,
        Secondary = 2
    }

    public enum SecRoleAllowConnections : byte
    {
        No = 0,
        [Description("Read Only")] ReadOnly = 1,
        All = 2
    }

    public enum SuspendReasons : byte
    {
        [Description("User Action")] UserAction = 0,
        [Description("Suspend From Partner")] SuspendFromPartner = 1,
        Redo = 2,
        Apply = 3,
        Capture = 4,
        Restart = 5,
        Undo = 6,
        Revaldiation = 7,
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
        Synchronizing = 1,
        Synchronized = 2,
        Reverting = 3,
        Initializing = 4
    }

    public enum TCPListenerStates : short
    {
        Online = 0,
        [Description("Pending Restart")]
        PendingRestart = 1
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
        Full = 1,
        [Description("Bulk Logged")] BulkLogged = 2,
        Simple = 3
    }

    public enum PageVerifyOptions : byte
    {
        None = 0,
        [Description("Torn Page Detection")] TornPageDetection = 1,
        Checksum = 2
    }

    public enum LogReuseWaits : byte
    {
        Nothing = 0,
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
        Off = 0,
        On = 1,
        [Description("On (Transitioning to Off)")] TransitioningToOff = 2,
        [Description("Off (Transitioning to On)")] TransitioningToOn = 3
    }

    public enum Containments : byte
    {
        Off = 0,
        Partial = 1
    }

    public enum FullTextInstallStatus
    {
        [Description("Not Installed")] NotInstalled = 0,
        Installed = 1
    }

    public enum HADREnabledStatus
    {
        Enabled = 0,
        Disabled = 1
    }

    public enum HADRManagerStatus
    {
        [Description("Not started, pending communication")] NotStarted = 0,
        [Description("Started and running")] Running = 1,
        [Description("Not started and failed")] Failed = 2
    }

    public enum MediaDeviceTypes : byte
    {
        Disk = 2,
        Tape = 4,
        [Description("Virtual Device")] VirtualDevice = 7,
        [Description("Permenant Device")] Permenant = 105
    }

    public enum VirtualMachineTypes
    {
        None = 0,
        Hypervisor = 1,
        Other = 2
    }

    public enum ServiceStartupTypes
    {
        [Description("Other")] Other0 = 0,
        [Description("Other")] Other1 = 1,
        Automatic = 2,
        Manual = 3,
        Disabled = 4
    }

    public enum ServiceStatuses
    {
        Stopped = 1,
        [Description("Start Pending")] StartPending = 2,
        [Description("Stop Pending")] StopPending = 3,
        Running = 4,
        [Description("Continue Pending")] ContinuePending = 5,
        [Description("Pause Pending")] PausePending = 6,
        Paused = 7
    }

    public enum JobStatuses
    {
        Failed = 0,
        Succeeded = 1,
        Retry = 2,
        Canceled = 3
    }

    public enum JobRunSources
    {
        Scheduler = 1,
        Alerter = 2,
        Boot = 3,
        User = 4,
        [Description("On Idle Schedule")] OnIdleSchedule = 6
    }

    public enum TableTypes
    {
        Heap = 0,
        Clustered = 1
    }

    public enum TransactionIsolationLevel : short
    {
        Unspecified = 0,
        [Description("Uncommited")] ReadUncomitted = 1,
        [Description("Commited")] ReadCommitted = 2,
        Repeatable = 3,
        Serializable = 4,
        Snapshot = 5
    }

    public enum DatabaseFileTypes : byte
    {
        Rows = 0,
        Log = 1,
        Reserved2 = 2,
        Reserved3 = 3,
        [Description("Full-text")] FullText = 4
    }

    public enum DatabaseFileStates : byte
    {
        Online = 0,
        Restoring = 1,
        Recovering = 2,
        [Description("Recovery Pending")] RecoveryPending = 3,
        Suspect = 4,
        Reserved5 = 5,
        Offline = 6,
        Defunct = 7
    }
}
namespace StackExchange.Opserver.Data.Elastic
{
    public static class ShardStates
    {
        public const string Unassigned = "UNASSIGNED";
        public const string Initializing = "INITIALIZING";
        public const string Started = "STARTED";
        public const string Relocating = "RELOCATING";
    }
}

using System;
using System.Threading.Tasks;

namespace Opserver.Data.Redis
{
    public class RedisInstanceOperation
    {
        public RedisInstance Instance { get; set; }
        public InstanceCommandType Command { get; set; }
        public RedisInstance NewMaster { get; set; }

        public Task PerformAsync() => Command switch
        {
            InstanceCommandType.MakeMaster => Instance.PromoteToPrimaryAsync(),
            InstanceCommandType.ReplicateFrom => Instance.ReplicateFromAsync(NewMaster.HostAndPort),
            _ => throw new ArgumentOutOfRangeException(nameof(InstanceCommandType)),
        };

        public static RedisInstanceOperation FromString(RedisModule module, string s)
        {
            var parts = s.Split(StringSplits.VerticalBar);
            if (parts.Length > 1 && Enum.TryParse<InstanceCommandType>(parts[0], out var opType))
            {
                var opee = module.Instances.Find(i => i.UniqueKey == parts[1]);
                switch (opType)
                {
                    case InstanceCommandType.MakeMaster:
                        return MakeMaster(opee);
                    case InstanceCommandType.ReplicateFrom:
                        var newMaster = module.Instances.Find(i => i.UniqueKey == parts[2]);
                        return ReplicateFrom(opee, newMaster);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(InstanceCommandType));
                }
            }
            throw new ArgumentOutOfRangeException(nameof(s), $"Invalid op string provided: '{s}'");
        }

        public override string ToString() =>
            Command switch
            {
                InstanceCommandType.MakeMaster => $"{InstanceCommandType.MakeMaster}|{Instance.UniqueKey}",
                InstanceCommandType.ReplicateFrom => $"{InstanceCommandType.ReplicateFrom}|{Instance.UniqueKey}|{NewMaster.UniqueKey}",
                _ => throw new ArgumentOutOfRangeException(nameof(InstanceCommandType)),
            };

        public static RedisInstanceOperation MakeMaster(RedisInstance instance) =>
            new RedisInstanceOperation
            {
                Command = InstanceCommandType.MakeMaster,
                Instance = instance
            };

        public static RedisInstanceOperation ReplicateFrom(RedisInstance instance, RedisInstance newMaster) =>
            new RedisInstanceOperation
            {
                Command = InstanceCommandType.ReplicateFrom,
                Instance = instance,
                NewMaster = newMaster
            };
    }

    public enum InstanceCommandType
    {
        MakeMaster,
        ReplicateFrom,
    }
}

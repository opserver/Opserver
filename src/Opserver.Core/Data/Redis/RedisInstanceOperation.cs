using System;
using System.Threading.Tasks;

namespace Opserver.Data.Redis
{
    public class RedisInstanceOperation
    {
        public RedisInstance Instance { get; set; }
        public InstanceCommandType Command { get; set; }
        public RedisInstance NewMaster { get; set; }

        public Task PerformAsync()
        {
            switch (Command)
            {
                case InstanceCommandType.MakeMaster:
                    var result = Instance.PromoteToMaster();
                    return Task.FromResult(result);
                case InstanceCommandType.SlaveTo:
                    return Instance.SlaveToAsync(NewMaster.HostAndPort);
                default:
                    throw new ArgumentOutOfRangeException(nameof(InstanceCommandType));
            }
        }

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
                    case InstanceCommandType.SlaveTo:
                        var newMaster = module.Instances.Find(i => i.UniqueKey == parts[2]);
                        return SlaveTo(opee, newMaster);
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
                InstanceCommandType.SlaveTo => $"{InstanceCommandType.SlaveTo}|{Instance.UniqueKey}|{NewMaster.UniqueKey}",
                _ => throw new ArgumentOutOfRangeException(nameof(InstanceCommandType)),
            };

        public static RedisInstanceOperation MakeMaster(RedisInstance instance) =>
            new RedisInstanceOperation
            {
                Command = InstanceCommandType.MakeMaster,
                Instance = instance
            };

        public static RedisInstanceOperation SlaveTo(RedisInstance instance, RedisInstance newMaster) =>
            new RedisInstanceOperation
            {
                Command = InstanceCommandType.SlaveTo,
                Instance = instance,
                NewMaster = newMaster
            };
    }

    public enum InstanceCommandType
    {
        MakeMaster,
        SlaveTo,
    }
}

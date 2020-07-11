namespace Opserver.Data.Redis
{
    public static class RedisRoles
    {
        public const string Admin = nameof(RedisModule) + ":" + nameof(Admin);
        public const string Viewer = nameof(RedisModule) + ":" + nameof(Viewer);
    }
}

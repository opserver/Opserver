namespace Opserver.Data.HAProxy
{
    public static class HAProxyRoles
    {
        public const string Admin = nameof(HAProxyModule) + ":" + nameof(Admin);
        public const string Viewer = nameof(HAProxyModule) + ":" + nameof(Viewer);
    }
}

namespace Opserver.Data.Cloudflare
{
    public static class CloudflareRoles
    {
        public const string Admin = nameof(CloudflareModule) + ":" + nameof(Admin);
        public const string Viewer = nameof(CloudflareModule) + ":" + nameof(Viewer);
    }
}

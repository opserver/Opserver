namespace Opserver.Data.Dashboard
{
    public static class DashboardRoles
    {
        public const string Admin = nameof(DashboardModule) + ":" + nameof(Admin);
        public const string Viewer = nameof(DashboardModule) + ":" + nameof(Viewer);
    }
}

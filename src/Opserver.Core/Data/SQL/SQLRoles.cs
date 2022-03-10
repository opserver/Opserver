namespace Opserver.Data.SQL
{
    public static class SQLRoles
    {
        public const string Admin = nameof(SQLModule) + ":" + nameof(Admin);
        public const string Viewer = nameof(SQLModule) + ":" + nameof(Viewer);
    }
}

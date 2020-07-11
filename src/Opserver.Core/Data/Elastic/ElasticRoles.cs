namespace Opserver.Data.Elastic
{
    public static class ElasticRoles
    {
        public const string Admin = nameof(ElasticModule) + ":" + nameof(Admin);
        public const string Viewer = nameof(ElasticModule) + ":" + nameof(Viewer);
    }
}

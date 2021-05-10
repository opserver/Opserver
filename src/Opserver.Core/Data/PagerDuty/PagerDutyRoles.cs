namespace Opserver.Data.PagerDuty
{
    public static class PagerDutyRoles
    {
        public const string Admin = nameof(PagerDutyModule) + ":" + nameof(Admin);
        public const string Viewer = nameof(PagerDutyModule) + ":" + nameof(Viewer);
    }
}

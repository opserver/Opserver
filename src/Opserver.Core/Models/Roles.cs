namespace Opserver.Models
{
    public static class Roles
    {
        public const string Anonymous = nameof(Anonymous);
        public const string Authenticated = nameof(Authenticated);

        public const string LocalRequest = nameof(LocalRequest);
        public const string InternalRequest = nameof(InternalRequest);
        public const string ApiRequest = nameof(ApiRequest);

        public const string GlobalAdmin = nameof(GlobalAdmin);
        public const string GlobalViewer = nameof(GlobalViewer);
    }
}

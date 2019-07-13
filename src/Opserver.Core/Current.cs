namespace Opserver
{
    internal static class Current
    {
        public static readonly Helpers.LocalCache LocalCache = new Helpers.LocalCache();
    }

    public static class CoreCurrent
    {
        public static Helpers.LocalCache LocalCache => Current.LocalCache;
    }
}

namespace StackExchange.Opserver
{
    internal partial class Current
    {
        public static Helpers.LocalCache LocalCache = new Helpers.LocalCache();
    }

    public static class CoreCurrent
    {
        public static Helpers.LocalCache LocalCache => Current.LocalCache;
    }
}
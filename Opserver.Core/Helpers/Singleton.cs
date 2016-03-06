namespace StackExchange.Opserver.Helpers
{
    public static class Singleton<T> where T : new()
    {
        public static readonly T Instance = new T();
    }
}

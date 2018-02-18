using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StackExchange.Opserver.SettingsProviders
{
    public class JSONFileSettingsProvider : SettingsProvider
    {
        public override string ProviderType => "JSON File";

        public JSONFileSettingsProvider(SettingsSection settings) : base(settings)
        {
            if (Path.StartsWith("~\\"))
                Path = Path.Replace("~\\", AppDomain.CurrentDomain.BaseDirectory);
            AddDirectoryWatcher();
        }

        protected void AddDirectoryWatcher()
        {
            var watcher = new FileSystemWatcher(Path);
            watcher.Changed += (s, args) => SettingsChanged();
            watcher.EnableRaisingEvents = true;
        }

        private readonly object _loadLock = new object();
        private readonly ConcurrentDictionary<Type, object> _settingsCache = new ConcurrentDictionary<Type, object>();

        public override T GetSettings<T>()
        {
            lock (_loadLock)
            {
                if (_settingsCache.TryGetValue(typeof(T), out object cached))
                    return (T)cached;

                var settings = GetFromFile<T>();
                if (settings == null)
                    return null;
                _settingsCache.TryAdd(typeof (T), settings);
                return settings;
            }
        }

        public override T SaveSettings<T>(T settings)
        {
            return settings;
        }

        private string GetFullFileName<T>()
        {
            return System.IO.Path.Combine(Path, typeof (T).Name + ".json");
        }

        private T GetFromFile<T>() where T : new()
        {
            var path = GetFullFileName<T>();
            try
            {
                if (!File.Exists(path))
                {
                    return new T();
                }

                using (var sr = File.OpenText(path))
                {
                    var serializer = new JsonSerializer
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        };
                    return (T) serializer.Deserialize(sr, typeof (T));
                }
            }
            catch (Exception e)
            {
                // A race on reloads can happen - ignore as this is during shutdown
                if (!e.Message.Contains("The process cannot access the file"))
                    Opserver.Current.LogException("Error loading settings from " + path, e);
                return new T();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Opserver.Helpers
{
    public class PrefixedJsonConfigurationSource : JsonConfigurationSource
    {
        public string Prefix { get; set; }
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new PrefixedJsonConfigurationProvider(this);
        }
    }

    public class PrefixedJsonConfigurationProvider : JsonConfigurationProvider
    {
        private PrefixedJsonConfigurationSource ConfigSource { get; }
        public PrefixedJsonConfigurationProvider(PrefixedJsonConfigurationSource source) : base(source) => ConfigSource = source;

        public override void Load()
        {
            base.Load();
            // Do this once, rather than allocate a string per key fetch later
            var newData = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in Data.Keys)
            {
                newData[ConfigSource.Prefix + ':' + key] = Data[key];
            }
            Data = newData;
        }
    }

    public static class PrefixedJsonConfigurationExtensions
    {
        /// <summary>
        /// Adds a compatbility JSON file, from another path and with a specified prefix.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="prefix">The config prefix to use for lookups.</param>
        /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddPrefixedJsonFile(this IConfigurationBuilder builder, string prefix, string path, bool optional = true, bool reloadOnChange = true)
        {
            return builder.Add<PrefixedJsonConfigurationSource>(s =>
            {
                s.Prefix = prefix;
                s.Path = path;
                s.Optional = optional;
                s.ReloadOnChange = reloadOnChange;
                s.ResolveFileProvider();
            });
        }
    }
}

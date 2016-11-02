using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Exceptions
{
    public class ExceptionsModule : StatusModule
    {
        public static bool Enabled => Stores.Count > 0;

        public static List<ExceptionStore> Stores { get; }

        static ExceptionsModule()
        {
            Stores = Current.Settings.Exceptions.Stores
                .Select(s => new ExceptionStore(s))
                .Where(s => s.TryAddToGlobalPollers())
                .ToList();
        }

        public override bool IsMember(string node) => false;
    }
}

using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class ProvidersSettings
    {
        public BosunSettings Bosun { get; set; }
        public OrionSettings Orion { get; set; }
        public WMISettings WMI { get; set; }

        public bool Any() => All.Any(p => p != null);

        public IEnumerable<IProviderSettings> All
        {
            get
            {
                yield return Bosun;
                yield return Orion;
                yield return WMI;
            }
        }
    }
    
    public interface IProviderSettings
    {
        bool Enabled { get; }
        string Name { get; }

        void Normalize();
    }
}

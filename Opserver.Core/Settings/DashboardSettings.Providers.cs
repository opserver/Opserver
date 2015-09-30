namespace StackExchange.Opserver
{
    public class ProvidersSettings
    {
        public BosunSettings Bosun { get; set; }
        public OrionSettings Orion { get; set; }
        public WMISettings WMI { get; set; }
        
        public bool Any() => Bosun != null || Orion != null || WMI != null;
    }
    
    public interface IProviderSettings
    {
        bool Enabled { get; }
        string Name { get; }
    }
}

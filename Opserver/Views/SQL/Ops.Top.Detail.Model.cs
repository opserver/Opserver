using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class OpsTopDetailModel
    {
        public SQLInstance Instance { get; set; }
        public SQLInstance.TopOperation Op { get; set; }
    }
}
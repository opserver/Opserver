using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class OperationsActiveModel : DashboardModel
    {
        public SQLInstance.ActiveSearchOptions ActiveSearchOptions { get; set; }
    }
}
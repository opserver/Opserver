using Opserver.Data.SQL;

namespace Opserver.Views.SQL
{
    public class OperationsActiveModel : DashboardModel
    {
        public SQLInstance.ActiveSearchOptions ActiveSearchOptions { get; set; }
    }
}
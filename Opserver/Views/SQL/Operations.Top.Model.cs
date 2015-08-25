using System.Collections.Generic;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class OperationsTopModel : DashboardModel
    {
        public SQLInstance.TopSearchOptions TopSearchOptions { get; set; }
        public List<SQLInstance.TopOperation> TopOperations { get; set; }
        public bool Detailed { get; set; }
    }
}
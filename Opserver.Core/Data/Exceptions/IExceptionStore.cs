using StackExchange.Exceptional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Exceptions
{
    public interface IExceptionStore
    {      

        
         string Name { get;  }
         string Description { get;  }
        
        string NodeType { get; }
         Cache LastFetch { get;  }
        Cache<List<Application>> Applications { get; }
        Cache<List<Error>> ErrorSummary { get; }
        Task<List<Error>> GetSimilarErrorsInTimeAsync(Error error, int max);
        Task<List<Error>> GetSimilarErrorsAsync(Error error, int max);
        Task<List<Error>> FindErrorsAsync(string searchText, string appName, int max, bool includeDeleted);
        Task<Error> GetErrorAsync(Guid guid);
        Task<int> DeleteAllErrorsAsync(string appName);
        Task<bool> DeleteErrorAsync(Guid guid);
        Task<int> DeleteErrorsAsync(List<Guid> ids);
        Task<int> DeleteSimilarErrorsAsync(Error error);

        bool TryAddToGlobalPollers();
        Task<bool> ProtectErrorAsync(Guid guid);
         MonitorStatus MonitorStatus { get; }
        List<Error> GetErrorSummary(int maxPerApp, string appName = null);
    }
}

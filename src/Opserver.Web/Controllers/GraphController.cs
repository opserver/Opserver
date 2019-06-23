using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController : StatusController
    {
        public GraphController(IOptions<OpserverSettings> _settings) : base(_settings) { }
    }
}

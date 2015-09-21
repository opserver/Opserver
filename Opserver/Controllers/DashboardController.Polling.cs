using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Controllers
{
    public partial class DashboardController
    {
        [OutputCache(Duration = 1, VaryByParam = "node", VaryByContentEncoding = "gzip;deflate")]
        [Route("dashboard/node/poll/cpu")]
        public async Task<JsonResult> PollCPU(string id = null, string node = null, string range = null)
        {
            var n = id.HasValue() ? DashboardData.GetNodeById(id) : DashboardData.GetNodeByName(node);
            if (n == null)
                return JsonNotFound();

            var data = await n.GetCPUUtilization();
            if (data?.Data == null)
                return JsonNotFound();

            var total = data.Data.FirstOrDefault(c => c.Name == "Total");

            return Json(new
                {
                    duration = data.Duration.TotalMilliseconds,
                    cores = data.Data.Where(c => c != total).Select(c => new
                        {
                            name = c.Name,
                            utilization = c.Utilization
                        }),
                    total = total?.Utilization ?? 0
                });
        }
    }
}
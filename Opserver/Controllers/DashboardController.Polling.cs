using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Controllers
{
    public partial class DashboardController
    {
        [OutputCache(Duration = 1, VaryByParam = "node", VaryByContentEncoding = "gzip;deflate")]
        [Route("dashboard/node/poll/cpu")]
        public ActionResult PollCPU(int? id = null, string node = null, string range = null)
        {
            var n = id.HasValue ? DashboardData.GetNodeById(id.Value) : DashboardData.GetNodeByName(node);
            if (n == null)
                return JsonNotFound();

            var data = n.GetCPUUtilization();
            if (data == null || data.Data == null)
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
                    total = total != null ? total.Utilization : 0
                });
        }
    }
}
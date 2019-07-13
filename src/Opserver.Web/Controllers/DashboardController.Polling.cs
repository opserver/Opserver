using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Opserver.Controllers
{
    public partial class DashboardController
    {
        [ResponseCache(Duration = 1, VaryByQueryKeys = new string[] { "id", "node" })]
        [Route("dashboard/node/poll/cpu")]
        public async Task<ActionResult> PollCPU(string id = null, string node = null)
        {
            var n = id.HasValue() ? Module.GetNodeById(id) : Module.GetNodeByName(node);
            if (n == null)
                return JsonNotFound();

            var data = await n.GetCPUUtilization();
            if (data?.Data == null)
                return JsonNotFound();

            var total = data.Data.Find(c => c.Name == "Total");

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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController : StatusController
    {   
        private static Chart GetChart(int? height = null, int? width = null)
        {
            return new Chart
                       {
                           BackColor = System.Drawing.Color.Transparent,
                           Width = Unit.Pixel(width ?? Current.ViewSettings.SummaryChartWidth),
                           Height = Unit.Pixel(height ?? Current.ViewSettings.SummaryChartHeight),
                           AntiAliasing = AntiAliasingStyles.All,
                           TextAntiAliasingQuality = TextAntiAliasingQuality.High,
                           Palette = ChartColorPalette.None
                       };
        }
    }

    public static class ChartExtentions
    {
        public static ActionResult ToResult(this Chart chart)
        {
            var width = (int)chart.Width.Value;
            var height = (int)chart.Height.Value;

            using (var bmp = new Bitmap(width, height))
            {
                bmp.SetResolution(326, 326); // retina max
                using (var g = Graphics.FromImage(bmp))
                {
                    chart.Paint(g, new Rectangle(new Point(0, 0), new Size(width, height)));
                }
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    return new FileContentResult(ms.ToArray(), "image/png");
                }
            }
        }
    }
}

using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web.Mvc;
using Svg;
using Svg.Transforms;

namespace StackExchange.Opserver.Controllers
{
    public class ExportController : Controller
    {
        [Route("export"), ValidateInput(false)]
        public ActionResult ChartExport(string fileName, string type, int width, string svg)
        {
            type = type.ToLowerInvariant(); //normalize bitches!
            var name = $"{(fileName.HasValue() ? fileName : "Chart")}.{GetExtension(type)}";

            byte[] result;
            using (var ms = new MemoryStream())
            {
                switch (type)
                {
                    case "image/jpeg":
                        CreateSvgDocument(svg, width).Draw().Save(ms, ImageFormat.Jpeg);
                        break;
                    case "image/svg+xml":
                        using (var sw = new StreamWriter(ms))
                            sw.Write(svg);
                        break;
                    //case "image/png":
                    default:
                        CreateSvgDocument(svg, width).Draw().Save(ms, ImageFormat.Png);
                        break;
                }
                result = ms.ToArray();
            }

            return File(result, type, name);
        }

        /// <summary>
        /// Creates an SvgDocument from the SVG text string.
        /// </summary>
        /// <remarks>Adapted from Tek4: https://github.com/imclem/Highcharts-export-module-asp.net/blob/master/Tek4.Highcharts.Exporting/Exporter.cs </remarks>
        private static SvgDocument CreateSvgDocument(string source, int width)
        {
            var truncatedSource = source.HasValue() ? source.Substring(0, source.IndexOf("</svg>") + 6) : "";

            SvgDocument svgDoc;
            using (var streamSvg = new MemoryStream(Encoding.UTF8.GetBytes(truncatedSource)))
            {
                svgDoc = SvgDocument.Open(streamSvg);
            }

            // Scale SVG document to requested width.
            svgDoc.Transforms = new SvgTransformCollection();
            float scalar = (float)width / (float)svgDoc.Width;
            svgDoc.Transforms.Add(new SvgScale(scalar, scalar));
            svgDoc.Width = new SvgUnit(svgDoc.Width.Type, svgDoc.Width * scalar);
            svgDoc.Height = new SvgUnit(svgDoc.Height.Type, svgDoc.Height * scalar);

            return svgDoc;
        }

        private string GetExtension(string type)
        {
            // Validate requested MIME type.
            switch (type)
            {
                case "image/jpeg":
                    return "jpg";
                case "image/svg+xml":
                    return "svg";
                //case "image/png":
                default:
                    return "png";
            }
        }
    }
}
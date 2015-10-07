using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Xsl;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Models
{
    public static class ExtensionMethods
    {
        public static IHtmlString ToSpeed(this float bps, string unit = "b")
        {
            if (bps < 1) return @"<span class=""speed pow0"">0 b/s</span>".AsHtml();
            var pow = Math.Floor(Math.Log10(bps) / 3);
            var byteScale = bps.ToSize(unit);
            return string.Format(@"<span class=""speed pow{1}"">{0}/s</span>", byteScale, pow).AsHtml();
        }

        public static IHtmlString ToQueryString(this SQLInstance.TopSearchOptions options, bool leadingAmp = false)
        {
            var sb = new StringBuilder();
            
            if (options.MinExecs.HasValue) sb.Append("MinExecs=").Append(options.MinExecs).Append("&");
            if (options.MinExecsPerMin.HasValue) sb.Append("MinExecsPerMin=").Append(options.MinExecsPerMin).Append("&");
            if (options.Search.HasValue()) sb.Append("Search=").Append(HttpUtility.UrlEncode(options.Search)).Append("&");
            if (options.Database.HasValue) sb.Append("Database=").Append(options.Database.Value).Append("&");
            if (options.LastRunSeconds.HasValue) sb.Append("LastRunSeconds=").Append(options.LastRunSeconds.Value).Append("&");

            if (sb.Length > 0)
            {
                if (leadingAmp) sb.Insert(0, "&");
                return sb.ToString(0, sb.Length - 1).AsHtml();
            }
            return "".AsHtml();
        }
    }

    public static class VolumeExtensionMethods
    {
        public static IHtmlString FreeSpaceSpan(this Volume vol)
        {
            return string.Format("<span class=\"free-space {1}\">{0:n0}% Free</span>", 100 - vol.PercentUsed, vol.SpaceStatusClass).AsHtml();
        }
    }

    public static class InterfaceExtensionMethods
    {
        private static readonly IHtmlString _unknownSpan = @"<span class=""unknown""></span>".AsHtml();

        public static IHtmlString PrettyPercentUtilization(this Interface i)
        {
            if (!i.InPercentUtil.HasValue || !i.OutPercentUtil.HasValue) return "n/a".AsHtml();
            return $@"{i.InPercentUtil}% <span class=""note"">In - </span>{i.OutPercentUtil}% <span class=""note"">Out</span>".AsHtml();
        }

        public static IHtmlString PrettyIn(this Interface i)
        {
            return i.InBps.HasValue ? i.InBps.Value.ToSpeed() : _unknownSpan;
        }

        public static IHtmlString PrettyOut(this Interface i)
        {
            return i.OutBps.HasValue ? i.OutBps.Value.ToSpeed() : _unknownSpan;
        }
    }

    public static class ServerInfoExtensionMethods
    {
        private static readonly IHtmlString _unknownSpan = @"<span class=""unknown""></span>".AsHtml();
        
        public static IHtmlString LastUpdatedSpan(this Node info)
        {
            var addClass = "";
            if (info.LastSync < DateTime.UtcNow.AddMinutes(-30))
            {
                addClass = MonitorStatus.Critical.GetDescription();
            }
            else if (info.LastSync < DateTime.UtcNow.AddMinutes(-15))
            {
                addClass = MonitorStatus.Warning.GetDescription();
            }
            return info.LastSync.ToRelativeTimeSpan(addClass);
        }

        public static string PrettyTotalMemory(this Node info)
        {
            return info.TotalMemory.HasValue ? (info.TotalMemory.Value + 16 * 1024 * 1024).ToSize() : "";
        }

        public static string PrettyMemoryUsed(this Node info)
        {
            return info.MemoryUsed.HasValue ? info.MemoryUsed.Value.ToSize() : "";
        }

        public static MonitorStatus MemoryMonitorStatus(this Node info)
        {
            if (!info.PercentMemoryUsed.HasValue) return MonitorStatus.Unknown;
            if (info.Category.MemoryCriticalPercent > 0 && info.PercentMemoryUsed > (float)info.Category.MemoryCriticalPercent) return MonitorStatus.Critical;
            if (info.Category.MemoryWarningPercent > 0 && info.PercentMemoryUsed > (float)info.Category.MemoryWarningPercent) return MonitorStatus.Warning;
            return MonitorStatus.Good;
        }

        public static IHtmlString MemoryStatusSpan(this Node info)
        {
                if (info.MemoryUsed == -2) return _unknownSpan;
                return string.Format(@"<span class=""{3}"">{0} / {1} ({2:n2}%)</span>",
                                  info.PrettyMemoryUsed(),
                                  info.PrettyTotalMemory(),
                                  info.PercentMemoryUsed,
                                  info.MemoryMonitorStatus().GetDescription()).AsHtml();
        }

        public static IHtmlString MemoryPercentStatusSpan(this Node info)
        {
            if (info.MemoryUsed == -2) return _unknownSpan;
            return string.Format(@"<span title=""{0} / {1}"" class=""{3}"">{2:n0}%</span>",
                                info.PrettyMemoryUsed(),
                                info.PrettyTotalMemory(),
                                info.PercentMemoryUsed,
                                info.MemoryMonitorStatus().GetDescription()).AsHtml();
        }

        public static MonitorStatus CPUMonitorStatus(this Node info)
        {
            if (!info.CPULoad.HasValue) return MonitorStatus.Unknown;
            if (info.Category.CPUCriticalPercent > 0 && info.CPULoad > info.Category.CPUCriticalPercent) return MonitorStatus.Critical;
            if (info.Category.CPUWarningPercent > 0 && info.CPULoad > info.Category.CPUWarningPercent) return MonitorStatus.Warning;
            return MonitorStatus.Good;
        }

        public static IHtmlString CPUStatusSpan(this Node info)
        {
            if (info == null || info.CPULoad == -2) return _unknownSpan;
            return string.Format(@"<span class=""{1}"">{0:n0} %</span>", info.CPULoad, info.CPUMonitorStatus().GetDescription()).AsHtml();
        }

        public static IHtmlString PrettyTotalNetwork(this Node info)
        {
            if (info.Interfaces.All(i => i.InBps < 0)) return _unknownSpan;
            var bps = info.TotalPrimaryNetworkbps;
            return bps.ToSpeed();
        }

        public static IHtmlString NetworkTextSummary(this Node info)
        {
                var sb = new StringBuilder();
                sb.Append("Total Traffic: ").Append(info.TotalPrimaryNetworkbps.ToSize("b")).AppendLine("/s");
                sb.AppendFormat("Interfaces ({0} total):", info.Interfaces.Count()).AppendLine();
                info.PrimaryInterfaces.Take(5).OrderByDescending(i => i.InBps + i.OutBps)
                    .ForEach(
                        i => sb.AppendFormat("{0}: {1}/s\n(In: {2}/s, Out: {3}/s)\n",
                                             i.PrettyName,
                                             (i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)).ToSize("b"),
                                             i.InBps.GetValueOrDefault(0).ToSize("b"),
                                             i.OutBps.GetValueOrDefault(0).ToSize("b")));
                return sb.ToString().AsHtml();
        }

        public static IHtmlString ApplicationCPUTextSummary(this Node info)
        {
            if (info.Apps?.Any() != true) return MvcHtmlString.Empty;

            var sb = new StringBuilder();
            sb.AppendFormat("Total App Pool CPU: {0} %\n", info.Apps.Sum(a => a.PercentCPU.GetValueOrDefault(0)));
            sb.AppendLine("App Pools:");
            info.Apps.OrderBy(a => a.NiceName)
                .ForEach(a => sb.AppendFormat("  {0}: {1} %\n", a.NiceName, a.PercentCPU));
            return sb.ToString().AsHtml();
        }

        public static IHtmlString ApplicationMemoryTextSummary(this Node info)
        {
            if (info.Apps?.Any() != true) return MvcHtmlString.Empty;

            var sb = new StringBuilder();
            sb.AppendFormat("Total App Pool Memory: {0}\n", info.Apps.Sum(a => a.MemoryUsed.GetValueOrDefault(0)).ToSize());
            sb.AppendLine("App Pools:");
            info.Apps.OrderBy(a => a.NiceName)
                .ForEach(a => sb.AppendFormat("  {0}: {1}\n", a.NiceName, a.MemoryUsed.GetValueOrDefault(0).ToSize()));
            return sb.ToString().AsHtml();
        }
    }

    public static class SQLExtenstions
    {
        private static readonly XslCompiledTransform _queryPlanTransform;

        static SQLExtenstions()
        {
            _queryPlanTransform = new XslCompiledTransform();
            _queryPlanTransform.Load(Current.Context.Server.MapPath("~/Content/transforms/qp.xslt"));
        }

        //XNamespace ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";
        public static IHtmlString QueryPlanHtml(this SQLInstance.TopOperation op)
        {
            if (op.QueryPlan == null) return null;

            using(var sr = new StringReader(op.QueryPlan))
            using (var xmlr = XmlReader.Create(sr))
            {
                using(var sw = new StringWriter())
                using (var xmlw = XmlWriter.Create(sw, _queryPlanTransform.OutputSettings))
                {
                    _queryPlanTransform.Transform(xmlr, xmlw);
                    return sw.ToString().AsHtml();
                }
            }
        }
    }
}
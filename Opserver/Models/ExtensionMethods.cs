using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
            return string.Format(@"<span class=""speed pow{1}"">{0}/s</span>", byteScale, pow.ToString(CultureInfo.InvariantCulture)).AsHtml();
        }

        public static IHtmlString ToQueryString(this SQLInstance.TopSearchOptions options, bool leadingAmp = false)
        {
            var sb = StringBuilderCache.Get();

            // TODO: Refactor all of this to proper classes
            // For example, this is called in a loop and running the same code n times for no reason
            
            if (options.MinExecs.HasValue) sb.Append("MinExecs=").Append(options.MinExecs.ToString()).Append("&");
            if (options.MinExecsPerMin.HasValue) sb.Append("MinExecsPerMin=").Append(options.MinExecsPerMin.ToString()).Append("&");
            if (options.Search.HasValue()) sb.Append("Search=").Append(options.Search.UrlEncode()).Append("&");
            if (options.Database.HasValue) sb.Append("Database=").Append(options.Database.Value).Append("&");
            if (options.LastRunSeconds.HasValue) sb.Append("LastRunSeconds=").Append(options.LastRunSeconds.Value).Append("&");

            if (sb.Length <= 0) return MvcHtmlString.Empty;

            if (leadingAmp) sb.Insert(0, "&");
            return sb.ToStringRecycle(0, sb.Length - 1).AsHtml();
        }
    }

    public static class VolumeExtensionMethods
    {
        public static string PercentFreeSpace(this Volume vol) => (100 - vol.PercentUsed)?.ToString("n0") + "% Free";
    }

    public static class InterfaceExtensionMethods
    {
        public static IHtmlString PrettyIn(this Interface i) => i.InBps?.ToSpeed() ?? MvcHtmlString.Empty;

        public static IHtmlString PrettyOut(this Interface i) => i.OutBps?.ToSpeed() ?? MvcHtmlString.Empty;
    }

    public static class NodeExtensionMethods
    {
        public static IHtmlString LastUpdatedSpan(this Node info)
        {
            var addClass = "";
            if (info.LastSync < DateTime.UtcNow.AddMinutes(-30))
            {
                addClass = MonitorStatus.Critical.TextClass();
            }
            else if (info.LastSync < DateTime.UtcNow.AddMinutes(-15))
            {
                addClass = MonitorStatus.Warning.TextClass();
            }
            return info.LastSync?.ToRelativeTimeSpan(addClass) ?? "Unknown".AsHtml();
        }

        public static string PrettyTotalMemory(this Node info) => info.TotalMemory?.ToSize() ?? "";

        public static string PrettyMemoryUsed(this Node info) => info.MemoryUsed?.ToSize() ?? "";

        public static MonitorStatus MemoryMonitorStatus(this Node info)
        {
            if (!info.PercentMemoryUsed.HasValue) return MonitorStatus.Unknown;
            if (info.Category.MemoryCriticalPercent > 0 && info.PercentMemoryUsed > (float) info.Category.MemoryCriticalPercent) return MonitorStatus.Critical;
            if (info.Category.MemoryWarningPercent > 0 && info.PercentMemoryUsed > (float) info.Category.MemoryWarningPercent) return MonitorStatus.Warning;
            return MonitorStatus.Good;
        }

        public static IHtmlString MemoryStatusSpan(this Node info, bool includePercent = true)
        {
            return info.MemoryUsed < 0 
                ? MvcHtmlString.Empty
                : includePercent 
                    ? $@"<span class=""{info.MemoryMonitorStatus().TextClass()}"">{info.PrettyMemoryUsed()} / {info.PrettyTotalMemory()} ({info.PercentMemoryUsed?.ToString("n2")}%)</span>".AsHtml()
                    : $@"<span class=""{info.MemoryMonitorStatus().TextClass()}"">{info.PrettyMemoryUsed()} / {info.PrettyTotalMemory()}</span>".AsHtml();
        }

        public static IHtmlString MemoryPercentStatusSpan(this Node info)
        {
            return info.MemoryUsed < 0 
                ? MvcHtmlString.Empty
                : $@"<span title=""{info.PrettyMemoryUsed()} / {info.PrettyTotalMemory()}"" class=""{info.MemoryMonitorStatus().TextClass()}"">{info.PercentMemoryUsed?.ToString("n0")}%</span>".AsHtml();
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
            if (info == null || info.CPULoad < 0) return MvcHtmlString.Empty;
            return $@"<span class=""{info.CPUMonitorStatus().GetDescription()}"">{info.CPULoad?.ToString("n0")} %</span>".AsHtml();
        }

        public static IHtmlString PrettyTotalNetwork(this Node info) =>
            info.TotalPrimaryNetworkbps < 0
                ? MvcHtmlString.Empty
                : info.TotalPrimaryNetworkbps.ToSpeed();

        public static IHtmlString NetworkTextSummary(this Node info)
        {
            var sb = StringBuilderCache.Get();
            sb.Append("Total Traffic: ").Append(info.TotalPrimaryNetworkbps.ToSize("b")).AppendLine("/s");
            sb.AppendFormat("Interfaces ({0} total):", info.Interfaces.Count.ToString()).AppendLine();
            foreach (var i in info.PrimaryInterfaces.Take(5).OrderByDescending(i => i.InBps + i.OutBps))
            {
                sb.AppendFormat("{0}: {1}/s\n(In: {2}/s, Out: {3}/s)\n", i.PrettyName,
                    (i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)).ToSize("b"),
                    i.InBps.GetValueOrDefault(0).ToSize("b"), i.OutBps.GetValueOrDefault(0).ToSize("b"));
            }
            return sb.ToStringRecycle().AsHtml();
        }

        public static IHtmlString ApplicationCPUTextSummary(this Node info)
        {
            if (info.Apps?.Any() != true) return MvcHtmlString.Empty;

            var sb = StringBuilderCache.Get();
            sb.AppendFormat("Total App Pool CPU: {0} %\n", info.Apps.Sum(a => a.PercentCPU.GetValueOrDefault(0)).ToString(CultureInfo.CurrentCulture));
            sb.AppendLine("App Pools:");
            foreach (var a in info.Apps.OrderBy(a => a.NiceName))
            {
                sb.AppendFormat("  {0}: {1} %\n", a.NiceName, a.PercentCPU?.ToString(CultureInfo.CurrentCulture));
            }   
            return sb.ToStringRecycle().AsHtml();
        }

        public static IHtmlString ApplicationMemoryTextSummary(this Node info)
        {
            if (info.Apps?.Any() != true) return MvcHtmlString.Empty;

            var sb = StringBuilderCache.Get();
            sb.AppendFormat("Total App Pool Memory: {0}\n", info.Apps.Sum(a => a.MemoryUsed.GetValueOrDefault(0)).ToSize());
            sb.AppendLine("App Pools:");
            foreach (var a in info.Apps.OrderBy(a => a.NiceName))
            {
                sb.AppendFormat("  {0}: {1}\n", a.NiceName, a.MemoryUsed.GetValueOrDefault(0).ToSize());
            }
            return sb.ToStringRecycle().AsHtml();
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

            using (var sr = new StringReader(op.QueryPlan))
            using (var xmlr = XmlReader.Create(sr))
            {
                using (var sw = new StringWriter())
                using (var xmlw = XmlWriter.Create(sw, _queryPlanTransform.OutputSettings))
                {
                    _queryPlanTransform.Transform(xmlr, xmlw);
                    return sw.ToString().AsHtml();
                }
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Opserver.Data;

namespace Opserver.Helpers
{
    public static class Health
    {
        public static IHtmlContent Description(IEnumerable<IMonitorStatus> ims, bool unknownIsHealthy = false)
        {
            if (ims == null)
            {
                return HtmlString.Empty;
            }

            var sb = StringBuilderCache.Get();
            var warning = ims.Where(ag => ag.MonitorStatus == MonitorStatus.Warning).ToList();
            var good = ims.Where(ag => ag.MonitorStatus == MonitorStatus.Good || (unknownIsHealthy && ag.MonitorStatus == MonitorStatus.Unknown)).ToList();
            var bad = ims.Except(warning).Except(good).ToList();
            bool addSpace = false;

            void Append(MonitorStatus status, int count)
            {
                if (count > 0)
                {
                    if (addSpace)
                    {
                        sb.Append(" ");
                        addSpace = false;
                    }
                    sb.Append(status.IconSpan()).Append(" ").Append(count.ToComma());
                    addSpace = true;
                }
            }

            Append(MonitorStatus.Critical, bad.Count);
            Append(MonitorStatus.Warning, warning.Count);
            Append(MonitorStatus.Good, good.Count);
            return sb.ToStringRecycle().AsHtml();
        }

        public static IHtmlContent OfAGs(IEnumerable<IMonitorStatus> ims, bool minimal = false)
        {
            if (ims == null)
            {
                return HtmlString.Empty;
            }

            var sb = StringBuilderCache.Get();
            var bad = ims.Where(ag => ag.MonitorStatus != MonitorStatus.Good).ToList();
            var good = ims.Where(ag => ag.MonitorStatus == MonitorStatus.Good).ToList();
            if (minimal)
            {
                if (good.Count > 0)
                {
                    sb.Append(MonitorStatus.Good.Span(good.Count.ToComma(), good.Count.Pluralize("Healthy Database")));
                }
                sb.Append(" ");
                if (bad.Count > 0)
                {
                    if (good.Count > 0)
                    {
                        sb.Append(@"<span class=""text-muted"">/</span> ");
                    }
                    sb.Append(MonitorStatus.Critical.Span(bad.Count.ToComma(), bad.Count.Pluralize("Unhealthy Database")));
                }
            }
            else
            {
                if (bad.Count > 0)
                {
                    sb.Append(MonitorStatus.Critical.IconSpan()).Append(" ").Append(bad.Count.ToComma()).Append(" Unhealthy");
                }
                sb.Append(" ");
                if (good.Count > 0)
                {
                    sb.Append(MonitorStatus.Good.IconSpan()).Append(" ").Append(good.Count.ToComma()).Append(" Healthy");
                }
            }
            return sb.ToStringRecycle().AsHtml();
        }
    }
}

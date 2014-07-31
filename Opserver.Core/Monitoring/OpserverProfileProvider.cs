using System;
using System.Runtime.Remoting.Messaging;
using System.Web;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Monitoring
{
    public class OpserverProfileProvider : BaseProfilerProvider
    {
        public WebRequestProfilerProvider WebProfilerProvider = new WebRequestProfilerProvider();

        public const string LocalContextKey = "ContextProfiler";
        public static bool EnablePollerProfiling { get; set; }

        [Obsolete("Please use the Start(string sessionName) overload instead of this one. ProfileLevel is going away.")]
        public override MiniProfiler Start(ProfileLevel level, string sessionName = null)
        {
            if (HttpContext.Current != null)
            {
                return WebProfilerProvider.Start(level, sessionName);
            }

            // Anything not a web request goes HEREREEEEEEEERERERE!
            var contextProfiler = CreateContextProfiler(sessionName);
            SetProfilerActive(contextProfiler);
            return contextProfiler;
        }

        public override MiniProfiler Start(string sessionName = null)
        {
#pragma warning disable 618
            return Start(ProfileLevel.Info, sessionName);
#pragma warning restore 618
        }

        public override void Stop(bool discardResults)
        {
            var profiler = GetContextProfiler();
            if (profiler != null)
            {
                if (!StopProfiler(profiler)) return;
                SaveProfiler(profiler);
            }
            else
            {
                WebProfilerProvider.Stop(discardResults);
            }
        }

        public override MiniProfiler GetCurrentProfiler()
        {
            return GetContextProfiler() ?? WebProfilerProvider.GetCurrentProfiler();
        }

        /// <summary>
        /// Gets the profiler from the current context - this could be for a task/poll or for an entire web request
        /// </summary>
        public static MiniProfiler GetContextProfiler()
        {
            return CallContext.LogicalGetData(LocalContextKey) as MiniProfiler;
        }

        /// <summary>
        /// Creates a new profiler for the current context, used for background tasks
        /// </summary>
        /// <param name="name">The name of the profiler to create</param>
        /// <param name="id">The Id of the profiler</param>
        public static MiniProfiler CreateContextProfiler(string name, Guid? id = null)
        {
            var profiler = new MiniProfiler(name);
            SetProfilerActive(profiler);
            if (id.HasValue) profiler.Id = id.Value;
            CallContext.LogicalSetData(LocalContextKey, profiler);
            return profiler;
        }

        /// <summary>
        /// Stops the context provider only
        /// </summary>
        public static void StopContextProfiler()
        {
            var profiler = GetContextProfiler();
            if (profiler == null) return;
            
            StopProfiler(profiler);
            SaveProfiler(profiler);
            CallContext.LogicalSetData(LocalContextKey, null);
        }
    }
}

using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opserver.Helpers;

namespace Opserver
{
    public static class Program
    {
        public static readonly DateTime StartDate = DateTime.UtcNow;

        private static void Main(string[] args)
        {
            var runAsService = false;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--service") >= 0)
            {
                runAsService = true;
            }

            var hostBuilder = WebHost.CreateDefaultBuilder(args);
            if (runAsService)
            {
                // windows services are started with dotnet.exe
                // so their initial directory needs to be explicitly set
                var pathToContentRoot = Path.GetDirectoryName(
                    new Uri(typeof(Program).Assembly.CodeBase).LocalPath
                );

                hostBuilder = hostBuilder.UseContentRoot(pathToContentRoot);
            }

            var host = hostBuilder
                .ConfigureAppConfiguration(
                    (_, config) =>
                    {
                        config
                            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                            // v1.0 compat, for easier migrations
                            .AddPrefixedJsonFile("Security", "Config/SecuritySettings.json") // Note: manual migration from XML
                            .AddPrefixedJsonFile("Modules:Dashboard", "Config/DashboardSettings.json")
                            .AddPrefixedJsonFile("Modules:Cloudflare", "Config/CloudFlareSettings.json")
                            .AddPrefixedJsonFile("Modules:Elastic", "Config/ElasticSettings.json")
                            .AddPrefixedJsonFile("Modules:Exceptions", "Config/ExceptionSettings.json")
                            .AddPrefixedJsonFile("Modules:HAProxy", "Config/HAProxySettings.json")
                            .AddPrefixedJsonFile("Modules:PagerDuty", "Config/PagerDutySettings.json")
                            .AddPrefixedJsonFile("Modules:Redis", "Config/RedisSettings.json")
                            .AddPrefixedJsonFile("Modules:SQL", "Config/SQLSettings.json")
                            // End compat
                            .AddJsonFile("opserverSettings.json", optional: true, reloadOnChange: true)
                            .AddJsonFile("localSettings.json", optional: true, reloadOnChange: true);
                    }
                )
                .ConfigureLogging(
                    (hostingContext, config) =>
                    {
                        var loggingConfig = hostingContext.Configuration.GetSection("Logging");
                        config.AddConfiguration(loggingConfig)
                              .AddConsole();
                    }
                )
                .UseStartup<Startup>()
                .Build();

            if (!runAsService)
            {
                host.Run();
            }
            else
            {
                host.RunAsService();
            }
        }
    }
}

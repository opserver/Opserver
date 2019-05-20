using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StackExchange.Opserver
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
                .UseKestrel()
                .ConfigureAppConfiguration(
                    (hostingContext, config) =>
                    {
                        config
                            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                            .AddJsonFile("opserverSettings.json", optional: false, reloadOnChange: true);
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

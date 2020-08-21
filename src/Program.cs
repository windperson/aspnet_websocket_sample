using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using System;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace EchoApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                LoggerConfiguration logConfig = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.Trace();

                var aiKey = GetAzureApplicationInsightKey();
                if (!string.IsNullOrEmpty(aiKey))
                {
                    logConfig.WriteTo.ApplicationInsights(aiKey, TelemetryConverter.Traces);
                }

                Log.Logger = logConfig.Enrich.FromLogContext().CreateLogger();

                var host = CreateWebHostBuilder(args).Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "start failed");
            }

        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseSerilog();
                });

        private static string GetAzureApplicationInsightKey() =>
         Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

    }
}

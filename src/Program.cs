using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using System;
using System.IO;

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
                    logConfig.WriteTo.ApplicationInsightsTraces(aiKey);
                }

                Log.Logger = logConfig
                        .Enrich.FromLogContext().CreateLogger();

                IWebHost host = CreateWebHostBuilder(args).Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "start failed");
            }

        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args);

            var aiKey = GetAzureApplicationInsightKey();
            if (!string.IsNullOrEmpty(aiKey))
            {
                builder = builder.UseApplicationInsights();
            }

            return builder.UseKestrel()
                          .UseContentRoot(Directory.GetCurrentDirectory())
                          .UseIISIntegration()
                          .UseSerilog()
                          .UseStartup<Startup>();
        }


        private static string GetAzureApplicationInsightKey() =>
         Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

    }
}

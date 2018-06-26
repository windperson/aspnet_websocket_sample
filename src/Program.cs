using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;

namespace EchoApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.Trace()
                    .Enrich.FromLogContext()
                    .CreateLogger();

                var host = CreateWebHostBuilder(args)
                    .Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "start failed");
            }
            
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseSerilog()
            .UseStartup<Startup>();
    }
}

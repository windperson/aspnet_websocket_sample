using System.Text;
using EchoApp.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EchoApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //For BroadcastController keep sent message history
            services.AddMemoryCache();

            services.AddControllersWithViews();

            var signalRServerBuilder = services.AddSignalR().AddHubOptions<EchoHub>(options =>
            {
                options.EnableDetailedErrors = true;
                //options.SupportedProtocols = new List<string>{"json"};
            });

            if (IsSetUseAzureSignalr())
            {
                signalRServerBuilder.AddAzureSignalR();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //for System.Encoding.GetEncoding() to work.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "broadcast", pattern: "{controller}/{action=Index}");
                endpoints.MapHub<EchoHub>("/ws");
            });
        }

        private bool IsSetUseAzureSignalr()
        {
            return !string.IsNullOrEmpty(Configuration["UseAzureSignalR"]) && bool.Parse(Configuration["UseAzureSignalR"].Trim());
        }
    }
}

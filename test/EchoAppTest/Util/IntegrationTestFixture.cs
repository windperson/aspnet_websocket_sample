using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EchoAppTest.Util
{
    public class IntegrationTestFixture<TStartup> : IDisposable where TStartup : class
    {
        private readonly TestServer _server;


        public IntegrationTestFixture() : this(Path.Combine("src"))
        {
        }

        protected IntegrationTestFixture(string underTestProjectParaentDir)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Trace()
                .Enrich.FromLogContext()
                .CreateLogger();

            var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;
            var appRoot = GetProjectPath(underTestProjectParaentDir, startupAssembly);

            var builder = new WebHostBuilder()
                .UseContentRoot(appRoot)
                .ConfigureServices(InitializeServices)
                .UseEnvironment("Development")
                .UseSerilog()
                .UseStartup(typeof(TStartup));

            _server = new TestServer(builder);
        }

        public HttpClient CreateHttpClient(Uri baseAddress, params DelegatingHandler[] handlers)
        {
            HttpClient client;

            if (handlers == null || handlers.Length == 0)
            {
                client = _server.CreateClient();
                client.BaseAddress = baseAddress;

                return client;
            }

            for (var i = handlers.Length - 1; i > 1; i--)
            {
                handlers[i - 1].InnerHandler = handlers[i];
            }

            var serverHandler = _server.CreateHandler();
            handlers[handlers.Length - 1].InnerHandler = serverHandler;
            client = new HttpClient(handlers[0]) { BaseAddress = baseAddress };

            return client;
        }

        public WebSocketClient CreateWebSocketClient()
        {
            return _server.CreateWebSocketClient();
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        protected virtual void InitializeServices(IServiceCollection services)
        {
            var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;

            // Inject a custom application part manager. 
            // Overrides AddMvcCore() because it uses TryAdd().
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new AssemblyPart(startupAssembly));
            manager.FeatureProviders.Add(new ControllerFeatureProvider());
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

            services.AddSingleton(manager);
        }

        /// <summary>
        /// Gets the full path to the target project that we wish to test
        /// </summary>
        /// <param name="projectRelativePath">
        /// The parent directory of the target project.
        /// e.g. src, samples, test, or test/Websites
        /// </param>
        /// <param name="startupAssembly">The target project's assembly.</param>
        /// <returns>The full path to the target project.</returns>
        private static string GetProjectPath(string projectRelativePath, Assembly startupAssembly)
        {
            // Get name of the target project which we want to test
            var projectName = startupAssembly.GetName().Name;

            // Get currently executing test project path
            var applicationBasePath = System.AppContext.BaseDirectory;

            // Find the path to the target project
            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                directoryInfo = directoryInfo.Parent;

                var projectDirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, projectRelativePath));
                if (projectDirectoryInfo.Exists)
                {
                    var projectThatSameNameFolderFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, projectName, $"{projectName}.csproj"));
                    if (projectThatSameNameFolderFileInfo.Exists)
                    {
                        return Path.Combine(projectDirectoryInfo.FullName, projectName);
                    }

                    var projectFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, $"{projectName}.csproj"));
                    if (projectFileInfo.Exists)
                    {
                        return projectDirectoryInfo.FullName;
                    }
                }
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Project root could not be located using the application root {applicationBasePath}.");
        }
    }
}

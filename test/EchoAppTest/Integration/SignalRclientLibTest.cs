using EchoApp;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using EchoAppTest.Utils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace EchoAppTest.Integration
{
    public class SignalRclientLibTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly ILogger _output;

        private const string _url = "http://localhost/ws";


        public SignalRclientLibTest(ITestOutputHelper testOutputHelper)
        {
            _output = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(testOutputHelper)
                .CreateLogger();
        }

        // NOTE: When testing signalR, we cannot run test on websocket client of TestHost due to this:
        // https://github.com/aspnet/SignalR/issues/1595
        // Use another way to build hub connection:
        // https://lurumad.github.io/integration-tests-in-aspnet-core-signalr
        [Fact]
        public async Task TestServerHubInvocation()
        {
            //Arrange
            const string helloStr = "Hello SignalR";
            var webHostBuilder = WebHost.CreateDefaultBuilder().UseStartup<Startup>();
            var testServer = new TestServer(webHostBuilder);
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(_url, o => 
                    o.HttpMessageHandlerFactory = _ => testServer.CreateHandler())
                .ConfigureLogging(
                    builder =>
                    {
                        builder.AddSerilog(_output);
                    }).Build();

            //Act
            await hubConnection.StartAsync().OrTimeout();
            var recvStr = await hubConnection.InvokeCoreAsync<string>("EchoWithJsonFormat", new object[] { helloStr }).OrTimeout();

            //Assert
            Assert.Equal($"{{\"recv\": \"{helloStr}\"}}", recvStr);
        }

        [Fact]
        public async Task UsingLongPollingToTestEcho()
        {
            var webHostBuilder = WebHost.CreateDefaultBuilder().UseStartup<Startup>();

            const string helloStr = "Hello SignalR";

            string recvStr = string.Empty;

            using (var testServer = new TestServer(webHostBuilder))
            {
                var hubConnection = await StartLongPollingSignalRConnectionAsync(_url, testServer.CreateHandler());

                recvStr = await hubConnection.InvokeCoreAsync<string>("EchoWithJsonFormat", new object[] { helloStr }).OrTimeout();
            }

            Assert.Equal($"{{\"recv\": \"{helloStr}\"}}", recvStr);
        }

        private async Task<HubConnection> StartLongPollingSignalRConnectionAsync(string url, HttpMessageHandler handler)
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(url, options =>
               {
                   options.Transports = HttpTransportType.LongPolling;
                   options.HttpMessageHandlerFactory = h => handler;
               })
                .ConfigureLogging(
                    builder =>
                    {
                        builder.AddSerilog(_output);
                    })
                .Build();

            await hubConnection.StartAsync().OrTimeout();
            return hubConnection;
        }
    }
}
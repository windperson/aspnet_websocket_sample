using EchoApp;
using EchoAppTest.Util;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EchoAppTest.Integration
{
    public class SignalRclientLibTest : IClassFixture<IntegrationTestFixture<Startup>>
    {
        private readonly ILogger _output;

        private const string _url = "http://localhost/ws";

        private readonly WebSocketClient _webSocketClient;
        private readonly HttpClient _httpClient;

        public SignalRclientLibTest(IntegrationTestFixture<Startup> fixture, ITestOutputHelper testOutputHelper)
        {
            _output = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(testOutputHelper)
                .CreateLogger();

            _webSocketClient = fixture.CreateWebSocketClient();
            _httpClient = fixture.CreateHttpClient(new Uri(_url));
        }

        // NOTE: Cannot do test due to  https://github.com/aspnet/SignalR/issues/1595
        [Fact(Skip = "SignalR C# Client not support TestHost yet.")]
        public async Task TestServerHubInvocation()
        {

            //Arrange
            HubConnection connection = new HubConnectionBuilder()
                .WithUrl("http://localhost/ws", HttpTransportType.WebSockets, options =>
                      {
                          options.WebSocketConfiguration = socketOptions =>
                              {
                                  socketOptions.UseDefaultCredentials = true;
                              };

                      })
                .ConfigureLogging(
                    builder =>
                    {
                        builder.AddSerilog(_output);
                    }).Build();

            await connection.StartAsync();


        }


    }
}
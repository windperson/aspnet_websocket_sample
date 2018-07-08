using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoApp;
using Microsoft.AspNetCore.Mvc.Testing;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace EchoAppTest.Integration
{
    public class WebSocketEchoTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly ILogger _output;
        private readonly WebApplicationFactory<Startup> _factory;

        public WebSocketEchoTests(WebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
        {
            _output = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(testOutputHelper)
                .CreateLogger();
            _factory = factory;
        }

        [Fact]
        public async Task SendToCorrectServerHostingUrlAndGetCorrectReceiveMessage()
        {
            //Arrange
            //Note: We have to call CreateClient() first or the Server property in WebApplicationFactory would be null.
            _factory.CreateClient();
            var socket = await _factory.Server.CreateWebSocketClient().ConnectAsync(new Uri("https://localhost/ws"), CancellationToken.None);
            var helloStr = "hello";
            var hello = Encoding.UTF8.GetBytes(helloStr);
            var recvBuffer = new byte[1024 * 4];

            //Act
            await socket.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, CancellationToken.None);
            var result = await socket.ReceiveAsync(recvBuffer, CancellationToken.None);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "end test", CancellationToken.None);

            //Assert
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            Assert.True(result.Count > 0);
            var recvStr = GetReadableString(recvBuffer);
            _output.Information("receive={0}", recvStr);
            Assert.Equal($"{{\"recv\": \"{helloStr}\"}}", recvStr);
        }

        private string GetReadableString(byte[] buffer)
        {
            var nullStart = Array.IndexOf(buffer, (byte)0);
            nullStart = (nullStart == -1) ? buffer.Length : nullStart;
            var ret = Encoding.Default.GetString(buffer, 0, nullStart);
            return ret;
        }
    }
}

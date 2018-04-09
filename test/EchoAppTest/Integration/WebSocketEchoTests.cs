using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoApp;
using EchoAppTest.Util;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace EchoAppTest.Integration
{
    public class WebSocketEchoTests : IClassFixture<IntegrationTestFixture<Startup>>
    {
        private readonly WebSocketClient _webSocketClient;

        public WebSocketEchoTests(IntegrationTestFixture<Startup> fixture)
        {
            _webSocketClient = fixture.CreateWebSocketClient();
        }

        [Fact]
        public async Task SendToServerGetCorrectReceiveMessage()
        {
            //Arrange
            var socket = await _webSocketClient.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
            var hello = Encoding.UTF8.GetBytes("hello");
            var recvBuffer = new byte[1024 * 4];

            //Act
            await socket.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, CancellationToken.None);
            var result = await socket.ReceiveAsync(recvBuffer, CancellationToken.None);

            //Assert
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            Assert.True(result.Count > 0);

        }
    }
}

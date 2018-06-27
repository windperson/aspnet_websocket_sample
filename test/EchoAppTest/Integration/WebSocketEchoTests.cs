using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoApp;
using EchoAppTest.Util;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace EchoAppTest.Integration
{
    public class WebSocketEchoTests : IClassFixture<IntegrationTestFixture<Startup>>
    {
        private readonly ILogger _output;

        private readonly WebSocketClient _webSocketClient;

        public WebSocketEchoTests(IntegrationTestFixture<Startup> fixture, ITestOutputHelper testOutputHelper)
        {
            _output = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(testOutputHelper)
                .CreateLogger();
            _webSocketClient = fixture.CreateWebSocketClient();
        }

        [Fact]
        public async Task SendToCorrectServerHostingUrlAndGetCorrectReceiveMessage()
        {
            //Arrange
            var socket = await _webSocketClient.ConnectAsync(new Uri("https://localhost/ws"), CancellationToken.None);

            const string helloStr = "hello";
            var invocationId = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ff");
            var hello = CreateServerEchoInvocation(invocationId, helloStr);
            var handshakeBuffer = new byte[1024 * 2];
            var recvBuffer = new byte[1024 * 2];

            //Act
            await socket.SendAsync(new ArraySegment<byte>(GenerateHandShakMsg()), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
            await socket.ReceiveAsync(new ArraySegment<byte>(handshakeBuffer), CancellationToken.None);
            var handShakeResult = GetReadableStringFromSignalrJsonPayload(handshakeBuffer);
            Assert.Equal("{}", handShakeResult);

            await socket.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, CancellationToken.None);
            var result = await socket.ReceiveAsync(recvBuffer, CancellationToken.None);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "end test", CancellationToken.None);

            //Assert
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            Assert.True(result.Count > 0);
            var recvStr = GetReadableStringFromSignalrJsonPayload(recvBuffer);
            _output.Information("receive={0}", recvStr);

            var rpcResult = JsonConvert.DeserializeObject<SignalRpcResult>(recvStr);

            Assert.Equal(3, rpcResult.Type);
            Assert.Equal(invocationId, rpcResult.InvocationId);
            Assert.Equal($"\"{{\\\"recv\\\": \\\"{helloStr}\\\"}}\"", JsonConvert.SerializeObject(rpcResult.Result));
        }

        [Fact]
        public async Task CanReceivingServerSideStreaming()
        {
            //Arrange
            var socket = await _webSocketClient.ConnectAsync(new Uri("https://localhost/ws"), CancellationToken.None);

            const string helloStr = "hello";
            var invocationId = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ff");
            var hello = CreateServerReverseStreamInvocation(invocationId, helloStr);
            var handshakeBuffer = new byte[1024 * 2];
            var recvResults = new List<SignalRpcStreamResult>(helloStr.Length + 1);
            

            //Act & Assert
            await socket.SendAsync(new ArraySegment<byte>(GenerateHandShakMsg()), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
            await socket.ReceiveAsync(new ArraySegment<byte>(handshakeBuffer), CancellationToken.None);
            var handShakeResult = GetReadableStringFromSignalrJsonPayload(handshakeBuffer);
            Assert.Equal("{}", handShakeResult);

            await socket.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, CancellationToken.None);

            do
            {
                var tempBuffer = new byte[1024 * 2];
                var result = await socket.ReceiveAsync(tempBuffer, CancellationToken.None);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                Assert.True(result.Count > 0);

                var rpcResult =
                    JsonConvert.DeserializeObject<SignalRpcStreamResult>(
                        GetReadableStringFromSignalrJsonPayload(tempBuffer));
                if(rpcResult.Type == 6) { continue;}
                
                recvResults.Add(rpcResult);

                if (rpcResult.Type == 3 && rpcResult.InvocationId == invocationId)
                {
                    break;
                }
            } while (true);
            
            var index = helloStr.Length - 1;
            foreach (var theChar in helloStr)
            {
                var rpcResult = recvResults[index];

                Assert.Equal(2, rpcResult.Type);
                Assert.Equal(invocationId, rpcResult.InvocationId);
                var retValue = rpcResult.Item.ToString();
                Assert.Equal(1, retValue.Length);
                Assert.Equal(theChar, retValue[0]);
                index--;
            }

            var theLastRpcResult = recvResults.Last();
            Assert.Equal(3, theLastRpcResult.Type);
            Assert.Equal(invocationId, theLastRpcResult.InvocationId);
            Assert.Null(theLastRpcResult.Item);
            Assert.True(string.IsNullOrEmpty(theLastRpcResult.Error));
        }

        private byte[] CreateServerReverseStreamInvocation(string invocationId, string input)
        {
            var invocationJsonStr = $"{{\"type\":4,\"invocationId\":\"{invocationId}\",\"target\":\"Reverse\",\"arguments\":[\"{input}\"] }}";

            return PaddingJsonRecordSeparator(invocationJsonStr);

        }

        public class SignalRpcResult
        {
            public int Type { get; set; }
            public string InvocationId { get; set; }
            public Object Result { get; set; }
        }

        public class SignalRpcStreamResult
        {
            public int Type { get; set; }
            public string InvocationId { get; set; }
            public Object Item { get; set; }

            public string Error { get; set; }
        }

        private string GetReadableStringFromSignalrJsonPayload(byte[] buffer)
        {
            var nullStart = Array.IndexOf(buffer, (byte)0x1E);
            nullStart = (nullStart == -1) ? buffer.Length : nullStart;
            var ret = Encoding.Default.GetString(buffer, 0, nullStart);
            return ret;
        }

        private static byte[] GenerateHandShakMsg()
        {
            var handshakeRequestStr = @"{""protocol"": ""json"", ""version"" : 1}";

            return PaddingJsonRecordSeparator(handshakeRequestStr);
        }

        private static byte[] CreateServerEchoInvocation(string invocationId, string line)
        {
            var invocationJsonStr = $"{{\"type\":1,\"invocationId\":\"{invocationId}\",\"target\":\"EchoWithJsonFormat\",\"arguments\":[\"{line}\"] }}";

            return PaddingJsonRecordSeparator(invocationJsonStr);
        }

        private static byte[] PaddingJsonRecordSeparator(string jsonStr)
        {
            var rawBytes = Encoding.UTF8.GetBytes(jsonStr);
            var paddingBytes = new byte[rawBytes.Length + 1];
            Buffer.BlockCopy(rawBytes, 0, paddingBytes, 0, rawBytes.Length);
            paddingBytes[rawBytes.Length] = 0x1E;
            return paddingBytes;
        }
    }
}


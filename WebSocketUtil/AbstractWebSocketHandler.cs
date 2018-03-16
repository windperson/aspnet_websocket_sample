using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace aspnet_websocket_sample.WebSocketUtil
{
    public abstract class AbstractWebSocketHandler
    {
        protected readonly ILogger<AbstractWebSocketHandler> Logger;

        protected WebSocketManager WebSocketManager { get; set; }

        protected AbstractWebSocketHandler(WebSocketManager socketManager, ILogger<AbstractWebSocketHandler> logger)
        {
            WebSocketManager = socketManager;
            Logger = logger;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public virtual async Task<bool> OnConnect(WebSocket webSocket, string id = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
            }
            return WebSocketManager.AddWebSocket(id, webSocket);
        }

        public virtual async Task<bool> OnDisconnect(WebSocket webSocket)
        {
            var id = WebSocketManager.GetWebSocketId(webSocket);
            return await WebSocketManager.CloseWebSocketAsync(id);
        }

        public async Task<bool> SendAsync(string id, string message)
        {
            var webSocket = WebSocketManager.GetWebSocket(id);
            return await SendAsync(webSocket, message);
        }

        public async Task<bool> SendAsync(WebSocket webSocket, string message)
        {
            if (webSocket.State != WebSocketState.Open)
            {
                return false;
            }

            var dataBytes = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(
                buffer: new ArraySegment<byte>(dataBytes, 0, dataBytes.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
            return true;
        }

        public async Task<bool> BroadcastTo(string[] ids, string message)
        {
            var sendAllSuccess = false;
            foreach (var id in ids)
            {
                sendAllSuccess = await SendAsync(id, message);
            }
            return sendAllSuccess;
        }

        public async Task BroadcastAll(string message)
        {
            foreach (var kv in WebSocketManager.GetAll())
            {
                if (kv.Value.State == WebSocketState.Open)
                {
                    await SendAsync(kv.Value, message);
                }
            }
        }

        public abstract Task ReceiveAsync(WebSocket webSocket, WebSocketReceiveResult receiveResult, byte[] buffer);
    }
}
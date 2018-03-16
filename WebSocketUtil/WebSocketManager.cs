using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace aspnet_websocket_sample.WebSocketUtil
{
    public class WebSocketManager
    {
        private readonly ILogger<WebSocketManager> _logger;
        private ConcurrentDictionary<string, WebSocket> _socketPool;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _logger = logger;
            _socketPool = new ConcurrentDictionary<string, WebSocket>();
        }

        public bool AddWebSocket(string id, WebSocket webSocket)
        {
            _logger.LogTrace("Add WebSocket {0}", id);
            return _socketPool.TryAdd(id, webSocket);
        }

        public WebSocket GetWebSocket(string id)
        {
            return _socketPool[id];
        }

        public string GetWebSocketId(WebSocket webSocket)
        {
            return _socketPool.FirstOrDefault(p => p.Value == webSocket).Key;
        }

        public async Task<bool> CloseWebSocketAsync(string id, string description = null)
        {
            _logger.LogTrace("Close WebSocket {0} due to {1}", id, description);

            if (_socketPool.TryRemove(id, out var webSocket))
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, description, CancellationToken.None);
                return true;
            }
            return false;
        }

        public int ConnectionCount()
        {
            return _socketPool.Keys.Count;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _socketPool;
        }
    }
}
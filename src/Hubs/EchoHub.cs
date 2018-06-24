using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EchoApp.Hubs
{
    public class EchoHub : Hub
    {
        private ILogger _logger;

        public EchoHub(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EchoHub>();
        }

        public async Task EchoMessage(string message)
        {
            var structedBuffer = new MyStructedLog() { Buffer = message };
            _logger.LogInformation("buffer= {@1}", structedBuffer);
            var sendStr = $"{{\"recv\": \"{message}\"}}";
            await Clients.Caller.SendAsync("client_echo",sendStr);
        }
    }
}

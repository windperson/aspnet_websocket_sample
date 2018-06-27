﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EchoApp.Hubs
{
    public class EchoHub : Hub
    {
        private readonly ILogger _logger;

        public EchoHub(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EchoHub>();
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("SignalR client {@1} connected", Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SignalR Users");
            await base.OnDisconnectedAsync(exception);
            _logger.LogInformation("SignalR client {@1} disconnected", Context.ConnectionId);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        // ReSharper disable UnusedMember.Global
        public async Task<string> EchoWithJsonFormat(string message)
        // ReSharper restore UnusedMember.Global
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var structedBuffer = new MyStructedLog() { Buffer = message };
            _logger.LogInformation("buffer= {@1}", structedBuffer);
            var sendStr = $"{{\"recv\": \"{message}\"}}";

            return sendStr;
        }
    }
}

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using aspnet_websocket_sample.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoApp
{
    public class Startup
    {
        private ILogger _logger;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //for System.Encoding.GetEncoding() to work.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _logger = loggerFactory.CreateLogger<Startup>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseLogRequest();
            app.UseLogResponse();

            #region UseWebSocketsOptions
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 1024 * 4
            };
            app.UseWebSockets(webSocketOptions);
            #endregion

            #region AcceptWebSocket
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Echo(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });
            #endregion

            app.UseFileServer();

        }
        #region Echo
        private async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var receiveStr = GetReadableString(buffer);

                var structedBuffer = new MyStructedLog() { Buffer = receiveStr };
                _logger.LogInformation("buffer= {@1}", structedBuffer);

                var sendStr = $"{{\"recv\": \"{receiveStr}\"}}";
                var sendBuffer = StringToByteArray(sendStr);

                await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Count()), result.MessageType, true, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        #endregion

        private string GetReadableString(byte[] buffer)
        {
            var nullStart = Array.IndexOf(buffer, (byte)0);
            nullStart = (nullStart == -1) ? buffer.Length : nullStart;
            var ret = Encoding.Default.GetString(buffer, 0, nullStart);
            return ret;
        }

        private byte[] StringToByteArray(string source)
        {
            return Encoding.Default.GetBytes(source);
        }
    }
}

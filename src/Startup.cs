using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using aspnet_websocket_sample.Middlewares;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EchoApp
{
    public class Startup
    {
        private ILogger _logger;

        private const string QueueName = "test_queue";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            #region Service Bus

            services.AddMassTransit();

            var bus = Bus.Factory.CreateUsingInMemory(ConfigInMemoryBus);

            services.AddSingleton<ISendEndpointProvider>(bus);
            services.AddSingleton<IPublishEndpoint>(bus);
            services.AddSingleton<IBus>(bus);
            services.AddSingleton<IBusControl>(bus);

            #endregion

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
                              IBusControl bus, IApplicationLifetime applicationLifetime)
        {
            //for System.Encoding.GetEncoding() to work.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            #region Service Bus

            applicationLifetime.ApplicationStarted.Register(bus.Start);
            applicationLifetime.ApplicationStarted.Register(bus.Stop);

            #endregion


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

                        var sendPoint = await bus.GetSendEndpoint(new Uri($"loopback://localhost/{QueueName}"));

                        await Echo(context, webSocket, sendPoint);
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

        private void ConfigInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(QueueName,
                endpointConfigurator =>
                {
                    endpointConfigurator.Handler<YourMessage>(MsgHandler);
                });
        }

#pragma warning disable 1998
        private async Task MsgHandler(ConsumeContext<YourMessage> context)
#pragma warning restore 1998
        {
            _logger.LogInformation("YourMessage: {@1}", context.Message);
        }

        #region Echo
        private async Task Echo(HttpContext context, WebSocket webSocket, ISendEndpoint sendEndpoint)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var receiveStr = GetReadableString(buffer);

                var structedBuffer = new MyStructedLog() { Buffer = receiveStr };
                _logger.LogInformation("buffer= {@1}", structedBuffer);

                var sendStr = $"{{\"recv\": \"{receiveStr}\"}}";

                await sendEndpoint.Send<YourMessage>(new YourMessage {Text = sendStr});

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

    public class YourMessage { public string Text { get; set; } }

}

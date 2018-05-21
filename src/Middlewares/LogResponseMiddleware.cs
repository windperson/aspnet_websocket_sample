using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace aspnet_websocket_sample.Middlewares
{
    //example inspired from: http://www.sulhome.com/blog/10/log-asp-net-core-request-and-response-using-middleware
    public class LogResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogResponseMiddleware> _logger;

        public LogResponseMiddleware(RequestDelegate next, ILogger<LogResponseMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                _logger.LogInformation("don't log websocket connction response");
                await _next.Invoke(context);
                return;
            }

            using (var memoryStream = new MemoryStream())
            {
                var originSteam = context.Response.Body;
                context.Response.Body = memoryStream;

                try
                {
                    await _next.Invoke(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "log response middleware call chained middleware(s) error");
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBodyStr = await (new StreamReader(memoryStream)).ReadToEndAsync();
                var headers = context.Response.Headers.ToDictionary(k => k.Key, v => v.Value.ToString());
                var logResponse = new
                {
                    URL = context.Request.GetDisplayUrl(),
                    STATUS = context.Response.StatusCode,
                    Header = headers,
                    Body = responseBodyStr
                };

                _logger.LogInformation("{@LogResponse}", logResponse);

                if (StatusCanHaveBody(context.Response.StatusCode))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(originSteam);
                }
            }
        }

        private bool StatusCanHaveBody(int statusCode)
        {
            // List of status codes taken from Microsoft.Net.Http.Server.Response
            return statusCode != 204 &&
                   statusCode != 205 &&
                   statusCode != 304;
        }
    }

    public static class LogResponseMiddlewareExtension
    {
        public static IApplicationBuilder UseLogResponse(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogResponseMiddleware>();
        }
    }
}
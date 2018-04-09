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
    public class LogRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogRequestMiddleware> _logger;

        public LogRequestMiddleware(RequestDelegate next, ILogger<LogRequestMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var originBodyStream = context.Request.Body;
            var requestBodyStream = await Copy(context.Request.Body);
            var requestBody = await (new StreamReader(requestBodyStream)).ReadToEndAsync();
            var headers = context.Request.Headers.ToDictionary(k => k.Key, v => v.Value.ToString());

            var logRequest = new
            {
                METHOD = context.Request.Method,
                URL = context.Request.GetDisplayUrl(),
                Header = headers,
                Body = requestBody
            };

            _logger.LogInformation("{@LogRequest}", logRequest);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;
            await _next(context);
            if (context.WebSockets.IsWebSocketRequest)
            {
                _logger.LogInformation("Incoming connection is a websocket connction request");
                return;
            }
            context.Request.Body = originBodyStream;
        }

        private async Task<MemoryStream> Copy(Stream input)
        {
            var ret = new MemoryStream();
            await input.CopyToAsync(ret);
            ret.Seek(0, SeekOrigin.Begin);
            return ret;
        }
    }

    public static class LogRequestMiddlewareExtension
    {
        public static IApplicationBuilder UseLogRequest(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogRequestMiddleware>();
        }
    }
}
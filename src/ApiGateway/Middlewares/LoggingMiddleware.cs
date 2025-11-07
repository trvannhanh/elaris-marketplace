using System.Diagnostics;
using System.Text;

namespace ApiGateway.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            if (context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            var sw = Stopwatch.StartNew();

            // Đọc request body (nếu là JSON)
            string requestBody = "";
            if (context.Request.ContentType?.Contains("application/json") == true)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // Sao chép response stream
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Đọc response body
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            sw.Stop();

            _logger.LogInformation(@"
                ==== HTTP {Method} {Path} ====
                Status: {StatusCode}
                Duration: {Elapsed} ms
                Request Body: {RequestBody}
                Response Body: {ResponseBody}
                ==============================",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                string.IsNullOrWhiteSpace(requestBody) ? "(empty)" : requestBody,
                string.IsNullOrWhiteSpace(responseBodyText) ? "(empty)" : responseBodyText
            );

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}


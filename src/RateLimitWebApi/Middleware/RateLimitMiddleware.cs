using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimitWebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RateLimitWebApi.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiRequestThrottleOptions _settings;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, IOptions<ApiRequestThrottleOptions> settings, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_settings.EnableThrottle)
            {
                await _next.Invoke(context);
                return;
            }

            // Check if the number of API requests in the configured time interval has exceeded the configured rate limit
            if (!ApiRequestContainer.Instance.HasExceededRateLimit(_settings.Limit, _settings.GetTimeSpan()))
            {
                ApiRequestContainer.Instance.EnqueueRequestPool();
            }
            else
            {
                // Return 429 Too Many Requests and Retry-After header
                var retryAfterDateTime = ApiRequestContainer.Instance.RetryAfter(_settings.GetTimeSpan());
                var retryAfterSeconds = retryAfterDateTime.Subtract(DateTime.UtcNow).Seconds + 1;
                var message = string.Format(
                $"Rate limit exceeded. Try again in #{retryAfterSeconds} seconds." +
                $" Allowed request rate is {_settings.Limit} per {_settings.Interval}");

                context.Response.Headers["Retry-After"] = retryAfterDateTime.ToString("r");
                context.Response.Headers["Retry-After-Seconds"] = retryAfterSeconds.ToString();
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "text/plain";

                await context.Response.WriteAsync(message).ConfigureAwait(false);
                return;
            }
            await _next.Invoke(context);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitMiddleware>();
        }
    }
}

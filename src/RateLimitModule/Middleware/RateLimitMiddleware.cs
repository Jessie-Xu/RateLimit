using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimitModule.Models;
using System;
using System.Threading.Tasks;

namespace RateLimitModule.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestThrottleOptions _settings;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, IOptions<RequestThrottleOptions> settings, ILogger<RateLimitMiddleware> logger)
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

            // TODO: get from the header
            var clientId = "*";
            // Check if the number of API requests in the configured time interval has exceeded the configured rate limit
            if (!RequestContainer.Instance.HasExceededRateLimit(_settings.Limit, _settings.GetTimeSpan()))
            {
                RequestContainer.Instance.EnqueueRequestPool();
            }
            else
            {
                await ReturnTooManyRequestsErrorResponse(context, clientId);
                return;
            }
            await _next.Invoke(context);
        }

        // Return HttpResponse Status 429 Too Many Requests and Retry-After headers
        private async Task ReturnTooManyRequestsErrorResponse(HttpContext context, string clientId)
        {
            var retryAfterDateTime = RequestContainer.Instance.RetryAfter(_settings.GetTimeSpan());
            var retryAfterSeconds = Math.Round(retryAfterDateTime.Subtract(DateTime.UtcNow).TotalSeconds) + 1;
            var message = string.Format(
            $"Rate limit exceeded. Try again in #{retryAfterSeconds} seconds." +
            $" Allowed request rate is {_settings.Limit} per {_settings.Interval}");

            _logger.LogInformation($"ClientId: {clientId}. {message}");

            context.Response.Headers["Retry-After"] = retryAfterDateTime.ToString("r");
            context.Response.Headers["Retry-After-Seconds"] = retryAfterSeconds.ToString();
            context.Response.StatusCode = 429;
            context.Response.ContentType = "text/plain";

            await context.Response.WriteAsync(message).ConfigureAwait(false);
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

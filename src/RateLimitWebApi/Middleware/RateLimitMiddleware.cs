using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimitWebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            // Check if the number of API requests in the configured time interval has exceeded the configured rate limit
            if (!ApiRequestContainer.Instance.HasExceededRateLimit(_settings.Limit, _settings.GetTimeSpan()))
            {
                ApiRequestContainer.Instance.EnqueueRequestPool();
            }
            else
            {
                // Return 429 Too many requests and customised header

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

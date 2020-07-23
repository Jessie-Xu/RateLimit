using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimitModule.Models;
using RateLimitModule.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimitModule.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ClientService _clientService;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, IOptions<RequestThrottleOptions> settings, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _clientService = new ClientService(settings);
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_clientService.RateLimitApplied())
            {
                await _next.Invoke(context);
                return;
            }

            // Check if the number of API requests in the configured time interval has exceeded the configured rate limit
            var client = GetClient(context);

            if (!RequestContainer.Instance.HasExceededRateLimit(client))
            {
                RequestContainer.Instance.EnqueueRequestPool(client);
            }
            else
            {
                await ReturnTooManyRequestsErrorResponse(context, client);
                return;
            }

            await _next.Invoke(context);
        }

        private ClientSetting GetClient(HttpContext context)
        {
            var clientId = "*";

            if (context.Request.Headers.TryGetValue("Client-Id", out var id))
            {
                clientId = id.First();
            }

            return _clientService.GetClient(clientId);
        }

        // Return HttpResponse Status 429 Too Many Requests and Retry-After headers
        private async Task ReturnTooManyRequestsErrorResponse(HttpContext context, ClientSetting client)
        {
            var retryAfterDateTime = RequestContainer.Instance.RetryAfter(client);
            var retryAfterSeconds = Math.Round(retryAfterDateTime.Subtract(DateTime.UtcNow).TotalSeconds) + 1;
            var message = string.Format(
            $"Rate limit exceeded. Try again in #{retryAfterSeconds} seconds." +
            $" Allowed request rate is {client.Limit} per {client.Interval}");

            _logger.LogInformation($"ClientId: {client.ClientId}. {message}");

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

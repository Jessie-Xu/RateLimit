using Microsoft.Extensions.Options;
using RateLimitWebApi.Models;
using System;
using System.Collections.Generic;

namespace RateLimitWebApi.Models
{
    public class ApiRequestContainer
    {
        private static readonly Lazy<ApiRequestContainer> _instance 
            = new Lazy<ApiRequestContainer>(() => new ApiRequestContainer());

        public static ApiRequestContainer Instance => _instance.Value;

        private readonly Queue<DateTime> _requestPool;
        

        private ApiRequestContainer()
        {
            _requestPool = new Queue<DateTime>();
        }

        public bool HasExceededRateLimit(int limit, TimeSpan timeSpan)
        {
            while ((_requestPool.Count > 0) 
                && (_requestPool.Peek() < DateTime.UtcNow.Subtract(timeSpan)))
                _requestPool.Dequeue();

            return _requestPool.Count >= limit;
        }

        public void EnqueueRequestPool()
        {
            _requestPool.Enqueue(DateTime.UtcNow);
        }

        public DateTime RetryAfter(TimeSpan timeSpan)
        {
            return _requestPool.Peek().Add(timeSpan);
        }
    }
}

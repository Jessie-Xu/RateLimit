using RateLimitWebApi.Models;
using System;
using System.Collections.Generic;

namespace RateLimitWebApi.Middleware
{
    public class ApiRequestContainer
    {
        private static readonly Lazy<ApiRequestContainer> _instance 
            = new Lazy<ApiRequestContainer>(() => new ApiRequestContainer());

        public static ApiRequestContainer Instance => _instance.Value;

        private readonly Queue<DateTime> _requestPool;
        private readonly ApiRequestThrottleOptions _options;

        private ApiRequestContainer()
        {
            _requestPool = new Queue<DateTime>();
            _options = new ApiRequestThrottleOptions();
        }
 
        public bool IsExceedRateLimit()
        {
            while ((_requestPool.Count > 0) 
                && (_requestPool.Peek() < DateTime.UtcNow.Subtract(_options.GetTimeSpan())))
                _requestPool.Dequeue();

            return _requestPool.Count >= _options.Limit;
        }

        public void EnqueueRequestPool()
        {
            _requestPool.Enqueue(DateTime.UtcNow);
        }
    }
}

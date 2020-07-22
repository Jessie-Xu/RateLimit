using System;
using System.Collections.Generic;

namespace RateLimitModule.Models
{
    public class RequestContainer
    {
        private static readonly Lazy<RequestContainer> _instance 
            = new Lazy<RequestContainer>(() => new RequestContainer());

        public static RequestContainer Instance => _instance.Value;
        private readonly object _enqueueLock; 
        private readonly object _dequeueLock; 
        public readonly Queue<DateTime> _requestPool;
        

        private RequestContainer()
        {
            _enqueueLock = new object();
            _dequeueLock = new object();
            _requestPool = new Queue<DateTime>();
        }

        public bool HasExceededRateLimit(int limit, TimeSpan timeSpan)
        {
            while (_requestPool.Count > 0
            && _requestPool.Peek() < DateTime.UtcNow.Subtract(timeSpan))
            {
                lock (_dequeueLock)
                {
                    _requestPool.Dequeue();
                }
            }
            return _requestPool.Count >= limit;
        }

        public void EnqueueRequestPool()
        {
            lock (_dequeueLock)
            {
                _requestPool.Enqueue(DateTime.UtcNow);
            } 
        }

        public DateTime RetryAfter(TimeSpan timeSpan)
        {
            return _requestPool.Peek().Add(timeSpan);
        }
    }
}

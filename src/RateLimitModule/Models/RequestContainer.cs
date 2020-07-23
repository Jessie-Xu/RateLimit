using System;
using System.Collections.Generic;

namespace RateLimitModule.Models
{
    public class RequestContainer
    {
        private readonly object _lock;
        private readonly IDictionary<string, Queue<DateTime>> _requestPool;
        private readonly IDictionary<string, object> _locks;

        private static readonly Lazy<RequestContainer> _instance
            = new Lazy<RequestContainer>(() => new RequestContainer());

        public static RequestContainer Instance => _instance.Value;

        private RequestContainer()
        {
            _lock = new object();
            _requestPool = new Dictionary<string, Queue<DateTime>>();
            _locks = new Dictionary<string, object>();
        }

        public bool HasExceededRateLimit(ClientSetting client)
        {
            lock (_lock)
            {
                var requestQueue = GetRequestQueue(client);

                while (requestQueue.Count > 0
                    && requestQueue.Peek() < DateTime.UtcNow.Subtract(client.GetTimeSpan()))
                {
                    requestQueue.Dequeue();
                }
                return requestQueue.Count >= client.Limit;
            }
        }

        public void EnqueueRequestPool(ClientSetting client)
        {
            lock (_lock)
            {
                GetRequestQueue(client).Enqueue(DateTime.UtcNow);
            } 
        }

        public DateTime RetryAfter(ClientSetting client)
        {
            lock (_lock)
            {
                return GetRequestQueue(client).Peek().Add(client.GetTimeSpan());
            }
        }

        private Queue<DateTime> GetRequestQueue(ClientSetting client)
        {
            if (!_requestPool.TryGetValue(client.ClientId, out Queue<DateTime> requestQueue))
            {
                requestQueue = new Queue<DateTime>(client.Limit);
                _requestPool.Add(client.ClientId, requestQueue);
            }
            return requestQueue;
        }
    }
}

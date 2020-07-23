using System;
using System.Collections.Generic;

namespace RateLimitModule.Models
{
    /// <summary>
    /// A singleton class provides a single enter to the request pool
    /// </summary>
    public class RequestContainer
    {
        private readonly object _mainlock;
        private readonly IDictionary<string, Queue<DateTime>> _requestPool;
        private readonly IDictionary<string, object> _enqueuelocks;
        private readonly IDictionary<string, object> _dequeuelocks;

        private static readonly Lazy<RequestContainer> _instance
          = new Lazy<RequestContainer>(() => new RequestContainer());

        public static RequestContainer Instance => _instance.Value;

        private RequestContainer()
        {
            _mainlock = new object();
            _requestPool = new Dictionary<string, Queue<DateTime>>();
            _enqueuelocks = new Dictionary<string, object>();
            _dequeuelocks = new Dictionary<string, object>();
        }

        /// <summary>
        /// Check if the rate limit has exceeded for a particular client or an anonymous client
        /// </summary>
        public bool HasExceededRateLimit(ClientSetting client)
        {
            lock (GetRequestDequeueLock(client))
            {
                var requestQueue = GetRequestQueue(client);

                // Synchronise the request queue. Dequeue all the requests that are added before the current period.
                while (requestQueue.Count > 0
                  && requestQueue.Peek() < DateTime.UtcNow.Subtract(client.GetTimeSpan()))
                {
                    requestQueue.Dequeue();
                }

                return requestQueue.Count >= client.Limit;
            }
        }

        /// <summary>
        /// Push the current UTC time into the request queue for a particular client or an anonymous client
        /// </summary>
        public void EnqueueRequestPool(ClientSetting client)
        {
            lock (GetRequestEnqueueLock(client))
            {
                GetRequestQueue(client).Enqueue(DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Return the DateTime when the next follow-up request can be make for a particular client or an anonymous client 
        /// </summary>
        public DateTime RetryAfter(ClientSetting client)
        {
            lock (GetRequestDequeueLock(client))
            {
                return GetRequestQueue(client).Peek().Add(client.GetTimeSpan());
            }
        }

        /// <summary>
        /// Get the request queue for a particular client or an anonymous client
        /// </summary>
        private Queue<DateTime> GetRequestQueue(ClientSetting client)
        {
            if (!_requestPool.TryGetValue(client.ClientId, out Queue<DateTime> requestQueue))
            {
                requestQueue = new Queue<DateTime>(client.Limit);
                _requestPool.Add(client.ClientId, requestQueue);
            }
            return requestQueue;
        }

        /// <summary>
        /// Get the lock applied when enqueue a request for a particular client or an anonymous client
        /// </summary>
        private object GetRequestEnqueueLock(ClientSetting client)
        {
            lock (_mainlock)
            {
                if (!_enqueuelocks.TryGetValue(client.ClientId, out object queueLock))
                {
                    queueLock = new object();
                    _enqueuelocks.Add(client.ClientId, queueLock);
                }
                return queueLock;
            }
        }

        /// <summary>
        /// Get the lock applied when dequeue a request for a particular client or an anonymous client
        /// </summary>
        private object GetRequestDequeueLock(ClientSetting client)
        {
            lock (_mainlock)
            {
                if (!_dequeuelocks.TryGetValue(client.ClientId, out object queueLock))
                {
                    queueLock = new object();
                    _dequeuelocks.Add(client.ClientId, queueLock);
                }
                return queueLock;
            }
        }
    }
}
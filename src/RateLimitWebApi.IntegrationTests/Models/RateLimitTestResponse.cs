using System.Collections.Generic;

namespace RateLimitWebApi.IntegrationTests.Models
{
    internal class RateLimitTestResponse
    {
        public int StatusCode { get; set; }
        public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; set; }
    }
}
using System.Collections.Generic;

namespace RateLimitModule.Models
{
    public class RequestThrottleOptions
    {
        public bool EnableThrottle{ get; set; }
        public List<string> Clientlist { get; set; }
        public List<ClientSetting> ClientSettings { get; set; }
    }
}

using System;

namespace RateLimitModule.Models
{
    public class RequestThrottleOptions
    {
        public bool EnableThrottle{ get; set; }
        public int Limit { get; set; }
        public string Interval { get; set; }

        public TimeSpan GetTimeSpan()
        {
            var time = Interval.Substring(0, Interval.Length - 1);
            var type = Interval.Substring(Interval.Length - 1, 1);

            switch (type)
            {
                case "s": return TimeSpan.FromSeconds(double.Parse(time));
                case "m": return TimeSpan.FromMinutes(double.Parse(time));
                case "h": return TimeSpan.FromHours(double.Parse(time));
                case "d": return TimeSpan.FromDays(double.Parse(time));
                default: throw new FormatException($"Time type {type} is not supported.");
            }
        }
    }
}

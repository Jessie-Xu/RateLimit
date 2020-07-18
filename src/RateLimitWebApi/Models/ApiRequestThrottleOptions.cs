using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimitWebApi.Models
{
    public class ApiRequestThrottleOptions
    {
        public bool EnableThrottle{ get; set; }
        public int Limit { get; set; }
        public string Interval { get; set; }

        public TimeSpan GetTimeSpan()
        {
            var time = Interval[0..^1];
            var type = Interval.Substring(Interval.Length - 1, 1);

            return type switch
            {
                "s" => TimeSpan.FromSeconds(double.Parse(time)),
                "m" => TimeSpan.FromMinutes(double.Parse(time)),
                "h" => TimeSpan.FromHours(double.Parse(time)),
                "d" => TimeSpan.FromDays(double.Parse(time)),
                _ => throw new FormatException($"Time type {type} is not supported."),
            };
        }
    }
}

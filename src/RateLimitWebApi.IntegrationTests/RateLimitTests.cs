using Microsoft.AspNetCore.Mvc.Testing;
using RateLimitWebApi.IntegrationTests.Models;
using RateLimitModule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RateLimitWebApi.IntegrationTests
{
    public class RateLimitTests : IClassFixture<RateLimitWebApiFactory>
    {
        private const string TestApiBaseUri = "https://localhost:50004";
        private const string TestApi = "/api/demo";
        private readonly HttpClient _client;
        private readonly RequestThrottleOptions _settings;

        public RateLimitTests(RateLimitWebApiFactory factory)
        {
            // Arrange
            _client = factory.CreateClient(options: new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(TestApiBaseUri)
            });

            _settings = factory.Settings;
        }

        [Fact]
        public async Task ApiRequestsWithinRateLimitTest()
        {
            var allTasks = new List<Task>();

            for (var i = 1; i <= _settings.Limit; i++)
            {
                allTasks.Add(Task.Run(async () =>
                {
                    // Act
                    var response = await _client.GetAsync(TestApi);

                    // Assert
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Assert.Equal(200, (int)response.StatusCode);
                    Assert.Equal("Hello AirTasker!", responseContent);
                }));
            }

            await Task.WhenAll(allTasks);
        }

        [Fact]
        public async Task ApiRequestsExceedRateLimitTest()
        {
            if (!_settings.EnableThrottle)
            {
                Assert.True(false);
                return;
            }

            // Act
            var allTasks = new List<Task<RateLimitTestResponse>>();

            for (var i = 1; i <= _settings.Limit + 1; i++)
            {
                allTasks.Add(Task.Run(async () =>
                {
                    var response = await _client.GetAsync(TestApi);
                    return new RateLimitTestResponse
                    {
                        StatusCode = (int)response.StatusCode,
                        Headers = response.Headers.ToList()
                    };
                }));
            }
        
            var responses = await Task.WhenAll(allTasks);

            // Assert
            // Check if the failed response status is 429 and check if there are Retry-After and Retry-After-Seconds headers
            var failedResponse = responses.SingleOrDefault(r => r.StatusCode == 429);
            Assert.NotNull(failedResponse);

            var retryAfterHeader = failedResponse.Headers.Where(h => h.Key.Equals("Retry-After")).ToList();
            var retryAfterSecondsHeader = failedResponse.Headers.Where(h => h.Key.Equals("Retry-After-Seconds")).ToList();
            Assert.Single(retryAfterHeader);
            Assert.Single(retryAfterSecondsHeader);

            var retryAfter = retryAfterHeader[0].Value.FirstOrDefault();
            var retryAfterSeconds = retryAfterSecondsHeader[0].Value.FirstOrDefault();
            Assert.NotNull(retryAfter);
            Assert.True(int.TryParse(retryAfterSeconds, out int seconds));

            // Retry API request after retryAfterSeconds, check if the value of Retry-After-Seconds is correct
            Thread.Sleep(seconds * 1000);

            var retryResponse = await _client.GetAsync(TestApi);
            Assert.Equal(200, (int)retryResponse.StatusCode);
        }
    }
}
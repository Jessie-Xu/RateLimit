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
using RateLimitModule.Services;
using Microsoft.Extensions.Options;

namespace RateLimitWebApi.IntegrationTests
{
    public class RateLimitTests : IClassFixture<RateLimitWebApiFactory>
    {
        private const string TestApiBaseUri = "https://localhost:50004";
        private const string TestApi = "/api/demo";
        private readonly HttpClient _client;
        private readonly IOptions<RequestThrottleOptions> _settings;
        private readonly ClientService _clientService;

        public RateLimitTests(RateLimitWebApiFactory factory)
        {
            // Arrange
            _client = factory.CreateClient(options: new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(TestApiBaseUri)
            });

            _settings = factory.Settings;
            _clientService = new ClientService(_settings);
        }

        [Theory]
        [InlineData("annoymous")]
        [InlineData("35932b11-4951-451a-917c-d136ecf2ec83")]
        [InlineData("8251bcad-1fc7-40c6-834e-57c144787a16")]
        public async Task ApiRequestsWithinRateLimitTest(string clientId)
        {
            var client = _clientService.GetClient(clientId);
            var allTasks = new List<Task>();

            for (var i = 1; i <= client.Limit; i++)
            {
                allTasks.Add(Task.Run(async () =>
                {
                    // Act
                    var response = await SendAsync(clientId);

                    // Assert
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Assert.Equal(200, (int)response.StatusCode);
                    Assert.Equal("Hello AirTasker!", responseContent);
                }));
            }

            await Task.WhenAll(allTasks);
        }

        [Theory]
        [InlineData("annoymous")]
        [InlineData("35932b11-4951-451a-917c-d136ecf2ec83")]
        [InlineData("8251bcad-1fc7-40c6-834e-57c144787a16")]
        public async Task ApiRequestsExceedRateLimitTest(string clientId)
        {
            if (!_settings.Value.EnableThrottle)
            {
                Assert.True(false);
                return;
            }

            // Act
            var client = _clientService.GetClient(clientId);
            var allTasks = new List<Task<RateLimitTestResponse>>();

            for (var i = 1; i <= client.Limit + 1; i++)
            {
                allTasks.Add(Task.Run(async () =>
                {
                    var response = await SendAsync(clientId);
                    return new RateLimitTestResponse
                    {
                        StatusCode = (int)response.StatusCode,
                        Headers = response.Headers.ToList()
                    };
                }));
            }
        
            var responses = await Task.WhenAll(allTasks);

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
            var retryResponse = await SendAsync(clientId);
            Assert.Equal(200, (int)retryResponse.StatusCode);
        }

        private async Task<HttpResponseMessage> SendAsync(string clientId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TestApi);
            request.Headers.Add("Client-Id", clientId);
            return await _client.SendAsync(request);
        }
    }
}
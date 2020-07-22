using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RateLimitModule.Models;

namespace RateLimitWebApi.IntegrationTests
{
    public class RateLimitWebApiFactory : WebApplicationFactory<Startup>
    {
        public RequestThrottleOptions Settings { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseStartup<Startup>()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .UseEnvironment("Development")               
                .ConfigureServices(services =>
                {
                    // Build the service provider.
                    var sp = services.BuildServiceProvider();

                    // Create a scope to obtain a reference to ApiRequestThrottleOptions
                    using (var scope = sp.CreateScope())
                    {
                        Settings = scope.ServiceProvider.GetRequiredService<IOptions<RequestThrottleOptions>>().Value;
                    }
                });
        }
    }
}

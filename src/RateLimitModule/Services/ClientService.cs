using Microsoft.Extensions.Options;
using RateLimitModule.Models;
using System.Linq;

namespace RateLimitModule.Services
{
    public class ClientService
    {
        private readonly RequestThrottleOptions _settings;

        public ClientService (IOptions<RequestThrottleOptions> settings)
        {
            _settings = settings.Value;
        }

        public ClientSetting GetClient(string clientId)
        {
            // This is the default settings for all clients including the clients in the list and any anonymous clients
            var defaultClientSetting = _settings.ClientSettings.First(c => c.ClientId == "*");
            var clientSetting = _settings.ClientSettings.FirstOrDefault(c => c.ClientId == clientId);
            if (_settings.Clientlist.Contains(clientId))
            {
                if (clientSetting != null)
                {
                    return clientSetting;
                }
                else
                {
                    defaultClientSetting.ClientId = clientId;
                    return defaultClientSetting;
                }
            }

            return defaultClientSetting;
        }
    }
}

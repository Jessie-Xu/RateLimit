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

        /// <summary>
        /// Get the client/requestor by the client Id.
        /// 
        /// The default client settings in the appsettings.json is the settings of clientId *
        /// 
        /// The default client settings will be applied to any clients listed in the ClientList but don't have their own configurations in ClientSettings
        /// 
        /// The default client settings will also be applied to any anonymous clients.
        /// All anonymous clients will be treated as one requestor, 
        /// meaning the default client settings will be applied in the application level.
        /// </summary>
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

        public bool RateLimitApplied()
        {
            return _settings.EnableThrottle &&
                _settings.ClientSettings != null &&
                _settings.ClientSettings.Any(c => c.ClientId == "*");
        }
    }
}

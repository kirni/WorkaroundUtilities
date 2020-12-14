using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// used as reference for other services and to display misc information at starting
/// </summary>
namespace WorkaroundUtilities
{
    public class GreetingService : IGreetingService
    {
        private readonly ILogger<GreetingService> _log;
        private readonly IConfiguration _config;

        public GreetingService(ILogger<GreetingService> log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }
        public void Run()
        {
            _log.LogInformation("Application starting");

            if (_config.GetSection("workarounds").Exists())
            {
                _log.LogInformation("appsettings loaded");
            }
            else
            {
                _log.LogError("no valid appsettings for Workaround Utilities found");
            }
        }
    }
}

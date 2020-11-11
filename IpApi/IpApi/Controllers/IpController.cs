
using IpApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IpApi.Controllers
{
    [ApiController]
    [Route("api/ip")]
    [Produces("application/json")]
    public class IpController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IpController> _logger;
        private readonly ServiceManager _serviceManager;

        public IpController(IConfiguration configuration, ILogger<IpController> logger, ServiceManager serviceManager)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceManager = serviceManager;
        }

        [HttpPost]
        public IpResult Post([FromBody] IpData data)
        {
            // Prep
            data.Services = data.Services?.Select(x => x.ToLower()).ToList();
            var services = _configuration
                .GetSection("Services")
                .Get<Dictionary<string, bool>>()
                .ToDictionary(x => x.Key.ToLower(), x => x.Value);
            var result = new IpResult();

            // Validation
            if (!(data.Services?.Any() ?? false))
            {
                data.Services = services
                    .Where(x => x.Value)
                    .Select(x => x.Key)
                    .ToList();
            }
            else
            {
                var invalidNames = data.Services
                    .Where(x => !services.Keys.Contains(x))
                    .ToList();
                foreach (var name in invalidNames)
                {
                    result.ServiceResults[name] = new
                    {
                        Error = ServiceErrorType.InvalidServiceName.ToString()
                    };
                    data.Services.Remove(name);
                }
            }

            // Execution
            var serviceResults = _serviceManager.SendIp(data.Services, data.Ip);

            foreach (var kvp in serviceResults)
            {
                result.ServiceResults.Add(kvp.Key, kvp.Value);
            }

            _logger.LogInformation($"Processed {data.Ip} with services [{string.Join(",", data.Services)}]");
            return result;
        }
    }
}

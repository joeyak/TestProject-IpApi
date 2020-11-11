
using IpApi.Models;
using IpCommon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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
        private const string WEATHER_NAME = "weather";
        private const string GEOIP_NAME = "geoip";
        private const string IPAPI_NAME = "ipapi";

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
            var hasWeather = data.Services.Contains(WEATHER_NAME);

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
                // Check for services that do not exist
                var invalidNames = data.Services
                    .Where(x => !services.Keys.Contains(x))
                    .ToList();
                foreach (var name in invalidNames)
                {
                    result.DetailedResults[name] = new ServiceError
                    {
                        Error = ServiceErrorType.InvalidServiceName,
                        Reason = "Could not find a match for the service specified.",
                    };
                    data.Services.Remove(name);
                }

                // Check that the weather service requirement is met
                if (hasWeather && !(data.Services.Contains(GEOIP_NAME) || data.Services.Contains(IPAPI_NAME)))
                {
                    result.DetailedResults[WEATHER_NAME] = new ServiceError
                    {
                        Error = ServiceErrorType.InvalidWorkflow,
                        Reason = "The weather service must have the geoip or ipapi service specified so the geo coordinates can be retrieved.",
                    };
                    data.Services.Remove(WEATHER_NAME);
                }
            }

            // Execution
            // the weather flow has a special one that is run after getting geo data
            if (hasWeather)
            {
                data.Services.Remove(WEATHER_NAME);
            }

            var sessionId = Guid.NewGuid().ToString();

            var serviceResults = _serviceManager.SendIp(data.Services, data.Ip, sessionId);

            if (hasWeather && !result.DetailedResults.ContainsKey(WEATHER_NAME))
            {
                string coords = null;
                if (serviceResults.ContainsKey(GEOIP_NAME))
                {
                    object obj = serviceResults[GEOIP_NAME];
                    if (!(obj is ServiceError))
                    {
                        dynamic dObj = obj;
                        coords = $"{dObj.latitude},{dObj.longitude}";
                    }
                }

                if (coords is null && serviceResults.ContainsKey(IPAPI_NAME))
                {
                    object obj = serviceResults[IPAPI_NAME];
                    if (!(obj is ServiceError))
                    {
                        dynamic dObj = obj;
                        coords = $"{dObj.lat},{dObj.lon}";
                    }
                }

                object weatherResult = null;
                if (coords is null)
                {
                    weatherResult = new ServiceError
                    {
                        Error = ServiceErrorType.InvalidWorkflow,
                        Reason = "A lat and lon was not retrieved to pass to the weather service."
                    };
                }
                else
                {
                    weatherResult = _serviceManager.SendMessage(WEATHER_NAME, coords, sessionId);
                }
                serviceResults[WEATHER_NAME] = weatherResult;
            }

            foreach (var kvp in serviceResults)
            {
                result.DetailedResults.Add(kvp.Key, kvp.Value);
            }

            result.Summary = MapSummary(result.DetailedResults);

            _logger.LogInformation($"Processed {data.Ip} with services [{string.Join(",", result.Summary.ProcessedServices)}]");
            return result;
        }

        private IpSummary MapSummary(Dictionary<string, object> data)
        {
            var summary = new IpSummary();
            summary.ProcessedServices = data.Keys.ToList();

            return summary;
        }
    }
}

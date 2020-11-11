
using IpApi.Models;
using IpCommon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
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
        private const string RDAP_NAME = "rdap";
        private const string PING_NAME = "ping";
        private const string RDNS_NAME = "reversedns";


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

            foreach (var kvp in data)
            {
                string service = kvp.Key;

                if (kvp.Value is ServiceError)
                {
                    summary.FailedServices.Add(service);
                    continue;
                }
                else
                {
                    summary.ProcessedServices.Add(service);
                }

                // Serialize and Deserialize so the issue of getting either an ExpandoObject or JsonElement doesn't occcur
                var element = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(kvp.Value));
                JsonElement GetProp(string name) => element.GetProperty(name);

                // GeoIP coords are more accurate so they overwrite IpApi
                if (service == GEOIP_NAME)
                {
                    summary.Latitude = GetProp("latitude").GetDouble();
                    summary.Longitude = GetProp("longitude").GetDouble();
                }

                if (service == IPAPI_NAME)
                {
                    summary.Latitude ??= GetProp("lat").GetDouble();
                    summary.Longitude ??= GetProp("lon").GetDouble();

                    summary.Country = GetProp("country").GetString();
                    summary.Region = GetProp("region").GetString();
                    summary.City = GetProp("city").GetString();
                    summary.Timezone = GetProp("timezone").GetString();
                    summary.ServiceProvider = GetProp("isp").GetString();
                }

                if (service == PING_NAME)
                {
                    var avg = GetProp("Replies")
                        .EnumerateArray()
                        .Average(x => x.GetProperty("Time").GetInt32());
                    summary.AveragePingTime = (int)Math.Round(avg);
                }

                if (service == RDAP_NAME)
                {
                    string GetEvent(string action)
                    {
                        try
                        {
                            return GetProp("events")
                                .EnumerateArray()
                                .SingleOrDefault(x => x.GetProperty("eventAction").GetString() == action)
                                .GetProperty("eventDate")
                                .GetString();
                        }
                        catch
                        {
                            return null;
                        }
                    }

                    summary.DomainStatus = GetProp("status")
                        .EnumerateArray()
                        .FirstOrDefault()
                        .GetString();
                    summary.DomainRegistration = GetEvent("registration");
                    summary.DomainLastChanged = GetEvent("last changed");
                }

                if (service == RDNS_NAME)
                {
                    summary.WebsiteDomain = GetProp("data")
                        .EnumerateArray()
                        .FirstOrDefault()
                        .GetString();
                }

                if (service == WEATHER_NAME)
                {
                    // Fun note: IpApi, GeoIp, and Weather services all give different
                    // lat/longs so....who knows where this weather is...but it demonstrates
                    // workflow so that's good.
                    summary.Weather = GetProp("consolidated_weather")
                        .EnumerateArray()
                        .FirstOrDefault()
                        .GetProperty("weather_state_name")
                        .GetString();
                }
            }

            return summary;
        }
    }
}

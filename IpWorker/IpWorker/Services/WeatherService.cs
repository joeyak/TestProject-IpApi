using IpCommon;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IpWorker.Services
{
    class WeatherService : IService
    {
        private const string SEARCH_URL = "https://www.metaweather.com/api/location/search/?lattlong=";
        private const string LOCATION_URL = "https://www.metaweather.com/api/location/";

        private HttpClient _client;
        public string Name => "weather";

        public WeatherService(HttpClient client)
        {
            _client = client;
        }

        public async Task<object> ProcessData(string data)
        {
            var searchString = await _client.GetStringAsync(SEARCH_URL + data);
            var searchResults = JsonSerializer.Deserialize<List<ExpandoObject>>(searchString);

            if (searchResults.Count == 0)
            {
                return new ServiceError
                {
                    Error = ServiceErrorType.ServiceFailed,
                    Reason = "No search results found from lat and long."
                };
            }

            var woeid = searchResults[0].First(x => x.Key == "woeid").Value;
            var content = await _client.GetStringAsync(LOCATION_URL + woeid);

            return JsonSerializer.Deserialize<ExpandoObject>(content);
        }
    }
}

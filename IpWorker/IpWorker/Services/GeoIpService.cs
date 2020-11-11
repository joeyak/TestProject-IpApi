using System;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace IpWorker.Services
{
    class GeoIpService : IService
    {
        private const string BASE_URL = "https://geoip.me/";
        private HttpClient _client;

        public string Name => "geoip";

        public GeoIpService(HttpClient client)
        {
            _client = client;
        }

        public async Task<object> ProcessIp(string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BASE_URL + ip));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/json"));

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ExpandoObject>(content);
        }
    }
}

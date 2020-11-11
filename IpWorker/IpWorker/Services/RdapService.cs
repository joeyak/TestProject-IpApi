using System;
using System.Dynamic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IpWorker.Services
{
    class RdapService : IService
    {
        private const string BASE_URL = "https://rdap.org/ip/";
        private HttpClient _client;

        public string Name => "rdap";

        public RdapService(HttpClient client)
        {
            _client = client;
        }

        public async Task<object> ProcessIp(string ip)
        {
            var content = await _client.GetStringAsync(new Uri(BASE_URL + ip));
            return JsonSerializer.Deserialize<ExpandoObject>(content);
        }
    }
}

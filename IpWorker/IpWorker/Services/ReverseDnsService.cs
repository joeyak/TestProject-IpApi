using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IpWorker.Services
{
    class ReverseDnsService : IService
    {
        private const string BASE_URL = "https://dnspropagation.net/reverse-dns-lookup/?parameter=PTR&url=";
        private HttpClient _client;

        public string Name => "reversedns";

        public ReverseDnsService(HttpClient client)
        {
            _client = client;
        }

        public async Task<object> ProcessIp(string ip)
        {
            var httpContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "parameter", "PTR" },
                { "url", ip }
            });
            var content = await _client.PostAsync(new Uri(BASE_URL + ip), httpContent);
            return JsonSerializer.Deserialize<ExpandoObject>(await content.Content.ReadAsStringAsync());
        }
    }
}

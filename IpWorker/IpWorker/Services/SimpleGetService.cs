using System;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace IpWorker.Services
{
    abstract class SimpleGetService : IService
    {
        private HttpClient _client;
        protected abstract string BASE_URL { get; }
        public abstract string Name { get; }

        public SimpleGetService(HttpClient client)
        {
            _client = client;
        }

        public async Task<object> ProcessData(string data)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BASE_URL + data));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/json"));

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ExpandoObject>(content);
        }
    }
}

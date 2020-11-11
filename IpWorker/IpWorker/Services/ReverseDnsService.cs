using System.Net.Http;

namespace IpWorker.Services
{
    class ReverseDnsService : SimpleHttpClientService
    {
        protected override string BASE_URL => "https://dnspropagation.net/reverse-dns-lookup/?parameter=PTR&url=";
        protected override HttpMethod Method => HttpMethod.Post;
        public override string Name => "reversedns";

        public ReverseDnsService(HttpClient client) : base(client) { }
    }
}

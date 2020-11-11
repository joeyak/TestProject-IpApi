using System.Net.Http;

namespace IpWorker.Services
{
    class IpApiService : SimpleGetService
    {
        public override string Name => "ipapi";

        protected override string BASE_URL => "http://ip-api.com/json/";

        public IpApiService(HttpClient client) : base(client) { }
    }
}

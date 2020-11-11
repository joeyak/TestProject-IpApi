using System.Net.Http;

namespace IpWorker.Services
{
    class GeoIpService : SimpleGetService
    {
        protected override string BASE_URL => "https://geoip.me/";
        public override string Name => "geoip";

        public GeoIpService(HttpClient client): base(client) { }
    }
}

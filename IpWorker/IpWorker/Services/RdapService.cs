using System.Net.Http;

namespace IpWorker.Services
{
    class RdapService : SimpleGetService
    {
        protected override string BASE_URL => "https://rdap.org/ip/";
        public override string Name => "rdap";

        public RdapService(HttpClient client) : base(client) { }
    }
}

using System.Collections.Generic;

namespace IpApi.Models
{
    public class IpResult
    {
        public IpSummary Summary { get; set; }
        public Dictionary<string, object> DetailedResults { get; set; } = new Dictionary<string, object>();
    }
}

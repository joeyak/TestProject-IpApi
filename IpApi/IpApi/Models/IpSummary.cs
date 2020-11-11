using System.Collections.Generic;

namespace IpApi.Models
{
    public class IpSummary
    {
        public List<string> ProcessedServices { get; set; } = new List<string>();
        public List<string> FailedServices { get; set; } = new List<string>();
        public double? Latitude { get; set; } //ipapi/geoip
        public double? Longitude { get; set; } //ipapi/geip
        public string Country { get; set; } //ipapi
        public string Region { get; set; } //ipapi
        public string City { get; set; } //ipapi
        public string Timezone { get; set; } //ipapi
        public string ServiceProvider { get; set; } //ipapi
        public int? AveragePingTime { get; set; } //ping
        public string DomainStatus { get; set; } //rdap
        public string DomainRegistration { get; set; } //rdap
        public string DomainLastChanged { get; set; } //rdap
        public string WebsiteDomain { get; set; } //reversedns
        public string Weather { get; set; } //weather
    }
}

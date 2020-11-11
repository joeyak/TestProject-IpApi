using System.Text.Json.Serialization;

namespace IpCommon
{
    public class ServiceError
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ServiceErrorType Error { get; set; }
        public string Reason { get; set; }
    }
}

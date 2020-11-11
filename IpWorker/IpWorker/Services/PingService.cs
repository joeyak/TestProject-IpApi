using IpCommon;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace IpWorker.Services
{
    class PingService : IService
    {
        private ILogger<PingService> _logger;
        public string Name => "ping";

        public PingService(ILogger<PingService> logger)
        {
            _logger = logger;
        }

        public Task<object> ProcessData(string data)
        {
            const int PING_COUNT = 3;

            var replies = new List<object>();
            dynamic result = new ExpandoObject();
            using var pinger = new Ping();
            try
            {
                for (int i = 0; i < PING_COUNT; i++)
                {
                    var reply = pinger.Send(data);
                    replies.Add(new
                    {
                        Status = reply.Status.ToString(),
                        TTL = reply.Options?.Ttl,
                        Bytes = reply.Buffer.Length,
                        Time = reply.RoundtripTime
                    });
                }
            }
            catch (PingException e)
            {
                // The ServiceError class isn't used here because there could 
                result.Error = ServiceErrorType.ServiceFailed.ToString();
                result.Reason = $"Failed to sucessfully ping object.";

                _logger.LogError(e, "Failed to send ping for ip " + data);
            }
            result.Replies = replies;

            return Task.FromResult((object)result);
        }
    }
}

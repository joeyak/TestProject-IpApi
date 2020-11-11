using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace IpWorker.Services
{
    class PingService : IService
    {
        public string Name => "ping";

        public Task<object> ProcessIp(string ip)
        {
            const int PING_COUNT = 3;

            var replies = new List<object>();
            dynamic result = new ExpandoObject();
            using var pinger = new Ping();
            try
            {
                for (int i = 0; i < PING_COUNT; i++)
                {
                    var reply = pinger.Send(ip);
                    replies.Add(new
                    {
                        Status = reply.Status.ToString(),
                        TTL = reply.Options?.Ttl,
                        Bytes = reply.Buffer.Length,
                        Time = reply.RoundtripTime
                    });
                }
            }
            catch (Exception)
            {
                result.Error = $"Failed to sucessfully ping object {PING_COUNT} times.";
            }
            result.Replies = replies;

            return Task.FromResult((object)result);
        }
    }
}

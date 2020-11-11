using IpApi.Models;
using IpCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IpApi
{
    public class ServiceManager
    {
        private int _timeoutLength;
        private ILogger<ServiceManager> _logger;
        private IConnection _connection;

        public ServiceManager(IConfiguration configuration, ILogger<ServiceManager> logger, IConnection connection)
        {
            _logger = logger;
            _connection = connection;

            _timeoutLength = Convert.ToInt32(configuration["ServiceTimeout"]);
        }

        public Dictionary<string, object> SendIp(List<string> services, string ip, string sessionId = null)
        {
            var data = new ConcurrentDictionary<string, object>();

            Parallel.ForEach(services, s => data.TryAdd(s, SendMessage(s, ip, sessionId)));

            return new Dictionary<string, object>(data);
        }

        public object SendMessage(string service, string ip, string sessionId = null)
        {
            sessionId ??= Guid.NewGuid().ToString();
            var returnQueue = $"{service}-{sessionId}";

            using var channel = _connection.CreateModel();
            channel.QueueDeclare(
                queue: service,
                durable: false,
                exclusive: false,
                autoDelete: true);
            channel.QueueDeclare(
                queue: returnQueue,
                durable: false,
                exclusive: false,
                autoDelete: true);

            string outMsg = JsonSerializer.Serialize(new ServiceProcessRequest { Data = ip });
            var properties = channel.CreateBasicProperties();
            properties.Expiration = (_timeoutLength * 1.5).ToString();
            properties.ReplyTo = returnQueue;

            using var signal = new ManualResetEvent(false);

            string result = null;
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                result = Encoding.UTF8.GetString(ea.Body.ToArray());
                signal.Set();

                _logger.LogDebug($"[{sessionId}]({ip}, {service}) {result}");
            };

            channel.BasicPublish(
                exchange: "",
                routingKey: service,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(outMsg));

            channel.BasicConsume(returnQueue, true, consumer);
            bool timeout = !signal.WaitOne(TimeSpan.FromMilliseconds(_timeoutLength));

            if (timeout)
            {
                return new { Error = ServiceErrorType.Timeout.ToString() };
            }

            return JsonSerializer.Deserialize<ExpandoObject>(result);
        }
    }
}

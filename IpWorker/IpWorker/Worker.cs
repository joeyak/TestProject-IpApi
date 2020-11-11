using IpCommon;
using IpWorker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IpWorker
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Worker> _logger;
        private readonly IService _service;
        private readonly IConnection _connection;

        public Worker(IConfiguration configuration, ILogger<Worker> logger, IService service)
        {
            _configuration = configuration;
            _logger = logger;
            _service = service;

            _connection = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"],
                Port = Convert.ToInt32(_configuration["RabbitMQ:Port"]),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"]
            }.CreateConnection();
        }

        public override void Dispose()
        {
            _connection.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Starting {_service.Name} service");

            using var channel = _connection.CreateModel();
            channel.QueueDeclare(
                queue: _service.Name,
                durable: false,
                exclusive: false,
                autoDelete: true);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var msg = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<ServiceProcessRequest>(msg);

                object serviceResult = null;
                try
                {
                    serviceResult = await _service.ProcessData(request.Data);
                }
                catch (Exception e)
                {
                    serviceResult = new { Error = $"{_service.Name} failed to process." };
                    _logger.LogWarning(e, $"{_service.Name} failed to process.");
                }

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(serviceResult)) ;

                var properties = channel.CreateBasicProperties();
                properties.Expiration = "60000";

                channel.BasicPublish(
                    exchange: "",
                    routingKey: ea.BasicProperties.ReplyTo,
                    basicProperties: properties,
                    body: body);
            };

            channel.BasicConsume(_service.Name, true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(int.MaxValue, stoppingToken);
            }

            // Clean up consumer connections
            foreach (var tag in consumer.ConsumerTags)
            {
                channel.BasicCancel(tag);
            }

            _logger.LogInformation("Worker ending");
        }
    }
}
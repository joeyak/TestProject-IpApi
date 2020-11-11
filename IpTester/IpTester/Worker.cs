using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace IpTester
{
    public class Worker : BackgroundService
    {
        private string _url;
        private string _ip;
        private List<string> _services;
        private readonly Semaphore _pool;
        private readonly ILogger<Worker> _logger;

        public Worker(IConfiguration configuration, ILogger<Worker> logger)
        {
            _logger = logger;

            var max = configuration.GetValue<int>("MaxConcurrency");
            _pool = new Semaphore(max, max);

            _url = configuration["Url"];
            _ip = configuration["Ip"];
            _services = configuration.GetSection("Services").Get<List<string>>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int count = 0;
            using var httpClient = new HttpClient();
            while (!stoppingToken.IsCancellationRequested)
            {
                count++;
                var t = Task.Run(() =>
                {
                    _logger.LogInformation($"Starting call {count}");
                    var sw = Stopwatch.StartNew();

                    var json = JsonSerializer.Serialize(new { Ip = _ip, Services = _services });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = httpClient.PostAsync(_url, content, stoppingToken).Result;

                    _logger.LogInformation($"[{count}] Returned {response.StatusCode} {DateTime.Now.TimeOfDay} - {sw.Elapsed}");

                    _pool.Release();
                }, stoppingToken);
                _pool.WaitOne();
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}

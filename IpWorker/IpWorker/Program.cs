using IpWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace IpWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddHttpClient();

                    var service = hostContext.Configuration["Service"].ToLower();
                    switch (service)
                    {
                        case "geoip":
                            services.AddTransient<IService, GeoIpService>();
                            break;
                        case "rdap":
                            services.AddTransient<IService, RdapService>();
                            break;
                        case "ping":
                            services.AddTransient<IService, PingService>();
                            break;
                        case "reversedns":
                            services.AddTransient<IService, ReverseDnsService>();
                            break;
                        default:
                            throw new Exception(service + " does not exist.");
                    }
                });
    }
}

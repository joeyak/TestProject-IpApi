using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace IpApi.Controllers
{
    [ApiController]
    [Route("api/services")]
    [Produces("application/json")]
    public class ServicesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServicesController> _logger;
        private readonly ServiceManager _serviceManager;

        public ServicesController(IConfiguration configuration, ILogger<ServicesController> logger, ServiceManager serviceManager)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public List<string> Get()
            => _configuration
                .GetSection("Services")
                .Get<Dictionary<string, bool>>()
                .Select(x => x.Key)
                .ToList();

        [HttpGet]
        [Route("default")]
        public List<string> Default()
            => _configuration
                .GetSection("Services")
                .Get<Dictionary<string, bool>>()
                .Where(x => x.Value)
                .Select(x => x.Key)
                .ToList();
    }
}

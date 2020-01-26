using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerServiceRepo
{
    public interface IServiceA : IService
    {
    }

    /// <summary>
    /// Could be it's own WebService or otherwise
    /// </summary>
    public class ServiceA : MicroServiceBase, IServiceA
    {
        private readonly ILogger<ServiceA> _logger;
        private readonly IServiceB _serviceB;
        private readonly IConfiguration _configuration;

        public ServiceA(ILogger<ServiceA> logger, IConfiguration configuration, IServiceB serviceB)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceB = serviceB;
        }

        public override void Run()
        {
            base.Run();
            _logger.LogInformation("In Service A @ {id}", System.Threading.Thread.CurrentThread.ManagedThreadId);
            _serviceB.Run();
        }
    }
}

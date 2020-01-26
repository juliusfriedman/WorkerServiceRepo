using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerServiceRepo
{
    public interface IServiceB : IService
    {
    }

    /// <summary>
    /// Yet another service
    /// </summary>
    public class ServiceB : MicroServiceBase, IServiceB
    {
        private readonly ILogger<ServiceB> _logger;
        private readonly IConfiguration _configuration;

        public ServiceB(ILogger<ServiceB> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override void Run()
        {
            base.Run();
            _logger.LogInformation("In Service B {id}", System.Threading.Thread.CurrentThread.ManagedThreadId);
        }
    }
}

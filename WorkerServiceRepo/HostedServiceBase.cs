using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerServiceRepo
{
    /// <summary>
    /// Abstract class which manages the boiler plate code
    /// </summary>
    public abstract class HostedServiceBase : BackgroundService, IHostedService
    {       
        internal protected IHostApplicationLifetime _appLifetime;

        public HostedServiceBase (IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
        }

        public override void Dispose()
        {
            //Will call cancel on the CancellationTokenSource from the base class
            base.Dispose();
        }

        protected abstract void OnStarted();
        protected abstract void OnStopping();
        protected abstract void OnStopped();

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            //Register Lifetime Events
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            //Will call ExecuteAsync
            await base.StartAsync(cancellationToken);
        }
    }
}

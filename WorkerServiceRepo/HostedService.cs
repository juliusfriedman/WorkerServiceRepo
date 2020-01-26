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
    public abstract class HostedService : BackgroundService, IHostedService
    {       
        internal protected Task _executingTask;
        internal protected CancellationTokenSource _cancellationTokenSource;
        internal protected IHostApplicationLifetime _appLifetime;

        public HostedService (IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
        }

        public override void Dispose()
        {
            base.Dispose();
            _cancellationTokenSource.Cancel();
        }

        protected abstract void OnStarted();
        protected abstract void OnStopping();
        protected abstract void OnStopped();

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            //Register Lifetime Events
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            // Create a linked token so we can trigger cancellation outside of this token's cancellation
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            _executingTask = ExecuteAsync(_cancellationTokenSource.Token);

            // If the task is completed then return it, otherwise it's running
            return _executingTask.IsCompleted ? _executingTask : base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            // Signal cancellation to the executing method
            _cancellationTokenSource?.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            await base.StopAsync(cancellationToken);
        }
    }
}

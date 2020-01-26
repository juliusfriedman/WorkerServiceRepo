using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerServiceRepo
{
    public class MicroServiceBase : IService
    {
        public bool IsRunning { get; protected set; }

        public virtual void Run()
        {
            IsRunning = true;
        }

        public virtual void Stop()
        {
            IsRunning = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerServiceRepo
{
    public interface IService
    {
        void Run();
        void Stop();
        bool IsRunning { get; }
    }
}

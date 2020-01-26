using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerServiceRepo
{
    public class Destination
    {
        public int Id { get; set; }
        public Microsoft.SqlServer.Types.SqlGeography Location { get; set; }
    }
}

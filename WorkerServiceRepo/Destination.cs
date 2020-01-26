using Microsoft.SqlServer.Types;
using System.ComponentModel.DataAnnotations;

namespace WorkerServiceRepo
{
    public class Destination
    {
        [Key]
        public int Id { get; set; }
        public SqlGeography Location { get; set; }
    }
}

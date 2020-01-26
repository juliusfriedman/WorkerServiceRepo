using Microsoft.SqlServer.Types;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkerServiceRepo
{
    [Table("Destination")]
    public class Destination
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public SqlGeography Location { get; set; }
    }
}

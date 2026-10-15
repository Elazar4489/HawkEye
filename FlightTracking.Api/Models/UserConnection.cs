using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracking.Api.Models
{
    public class UserConnection
    {
        [Key]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("is_connected")]
        public bool IsConnected { get; set; }

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

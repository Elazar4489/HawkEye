using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightTracking.NotificationWorker.Models
{
    [Table("CustomerPriorities")]
    public class CustomerPriority
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AreaType")]
        public string AreaType { get; set; } = string.Empty; // "RADIUS" / "POLYGON"

        // Central geometric field (for polygon or converted circle)
        [Column("GeometryData", TypeName = "GEOMETRY")]
        public Geometry? GeometryData { get; set; }

        // Center point (only relevant for radius)
        [Column("CenterPoint", TypeName = "POINT")]
        public Point? CenterPoint { get; set; }

        [Column("RadiusMeters")]
        public decimal? RadiusMeters { get; set; }
    }
}
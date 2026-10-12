using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightMatching.Models;

[Table("CustomerPriorities")]
public class CustomerPriority
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("Priority")]
    public int Priority { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("AreaType")]
    public string AreaType { get; set; } = string.Empty;

    [Column("GeometryData", TypeName = "GEOMETRY")]
    public Geometry? GeometryData { get; set; }

    [Column("CenterPoint", TypeName = "POINT")]
    public Point? CenterPoint { get; set; }

    [Column("RadiusMeters")]
    public decimal? RadiusMeters { get; set; }
}
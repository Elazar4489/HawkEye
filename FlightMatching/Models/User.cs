using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightMatching.Models;

[Table("Users")]
public class User
{
    [Key]
    [Column("uzer_id")]
    public int UzerId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("fone")]
    public string? Fone { get; set; }

    public ICollection<CustomerPriority> CustomerPriorities { get; set; }
        = new List<CustomerPriority>();
}
using System.ComponentModel.DataAnnotations;

namespace FlightTracking.Api.DTOs
{
    // נתונים שמתקבלים בעת יצירת/הרשמת משתמש
    public class CreateUserDto
    {
        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Fone { get; set; }
    }

    // נתונים שחוזרים ללקוח (ללא סיסמה מטעמי אבטחה)
    public class UserResponseDto
    {
        public int UzerId { get; set; }

        // Same value as UzerId; the frontend uses "id".
        public int Id => UzerId;

        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool HasPushToken { get; set; }
    }
    public class UpdateUserDto
    {
        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Fone { get; set; }

        [MinLength(6), MaxLength(100)]
        public string? Password { get; set; } // אופציונלי - אם ירצה לשנות סיסמה
    }

    public class UpdatePushTokenDto
    {
        // null / empty clears the token
        [MaxLength(500)]
        public string? PushToken { get; set; }
    }

    public class UpdateUserRoleDto
    {
        [Required]
        [RegularExpression("^(Admin|Operator)$")]
        public string Role { get; set; } = string.Empty;
    }
}

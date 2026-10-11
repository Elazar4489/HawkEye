namespace FlightTracking.Api.DTOs
{
    // נתונים שמתקבלים בעת יצירת/הרשמת משתמש
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }

    // נתונים שחוזרים ללקוח (ללא סיסמה מטעמי אבטחה)
    public class UserResponseDto
    {
        public int UzerId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }
    public class UpdateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
        public string? Password { get; set; } // אופציונלי - אם ירצה לשנות סיסמה
    }
}
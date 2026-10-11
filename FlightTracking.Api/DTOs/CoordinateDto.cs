namespace FlightTracking.Api.DTOs
{
    // DTO לקבלת נקודה בודדת (קו אורך וקו רוחב)
    public class CoordinateDto
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    // DTO ליצירת אזור עניין חדש (או רדיוס או פוליגון)
    public class CreateCustomerPriorityDto
    {
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון (רשימת קודקודים)
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }

    // DTO להצגת הנתונים חזרה ללקוח
    public class CustomerPriorityResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty;
        public decimal? RadiusMeters { get; set; }
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
    public class UpdateCustomerPriorityDto
    {
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
}
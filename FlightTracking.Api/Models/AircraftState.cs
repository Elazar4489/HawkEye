using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlightTracking.Api.Models
{
    public class AircraftStateDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("icao24")]
        public string Icao24 { get; set; } = string.Empty;

        [BsonElement("callsign")]
        public string? Callsign { get; set; }

        [BsonElement("origin_country")]
        public string? OriginCountry { get; set; }

        [BsonElement("time_position")]
        public long? TimePosition { get; set; }

        [BsonElement("last_contact")]
        public long? LastContact { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("baro_altitude")]
        public double? BaroAltitude { get; set; }

        [BsonElement("on_ground")]
        public bool OnGround { get; set; }

        [BsonElement("velocity")]
        public double? Velocity { get; set; }

        [BsonElement("true_track")]
        public double? TrueTrack { get; set; }

        [BsonElement("vertical_rate")]
        public double? VerticalRate { get; set; }

        [BsonElement("geo_altitude")]
        public double? GeoAltitude { get; set; }

        [BsonElement("squawk")]
        public string? Squawk { get; set; }

        [BsonElement("spi")]
        public bool Spi { get; set; }

        [BsonElement("position_source")]
        public int PositionSource { get; set; }

        [BsonElement("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
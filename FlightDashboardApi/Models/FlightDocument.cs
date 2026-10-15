using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlightDashboardApi.Models;

// Raw OpenSky state written by FlightProducer to the "flights" collection.
[BsonIgnoreExtraElements]
public class FlightDocument
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

    [BsonElement("latitude")]
    public double? Latitude { get; set; }

    [BsonElement("longitude")]
    public double? Longitude { get; set; }

    [BsonElement("baro_altitude")]
    public double? BaroAltitude { get; set; }

    [BsonElement("velocity")]
    public double? Velocity { get; set; }

    [BsonElement("true_track")]
    public double? TrueTrack { get; set; }

    [BsonElement("vertical_rate")]
    public double? VerticalRate { get; set; }

    [BsonElement("on_ground")]
    public bool OnGround { get; set; }

    [BsonElement("recorded_at")]
    public DateTime RecordedAt { get; set; }
}

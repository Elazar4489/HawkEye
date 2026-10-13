using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class FlightDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("flight_id")]
    public string FlightId { get; set; }

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("altitude")]
    public int Altitude { get; set; }

    [BsonElement("speed")]
    public int Speed { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
}
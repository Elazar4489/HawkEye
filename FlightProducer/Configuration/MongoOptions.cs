namespace FlightProducer.Configuration
{
    public class MongoOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "flight_tracking_db";
        public string CollectionName { get; set; } = "flights";

        // Raw snapshots are kept for this long (TTL index), so the DB does not grow forever.
        public int RetentionHours { get; set; } = 6;
    }
}

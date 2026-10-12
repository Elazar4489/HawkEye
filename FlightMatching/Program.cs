using FlightMatching.Configuration;
using FlightMatching.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var kafkaOptions = new KafkaOptions
{
    BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
    InputTopic = configuration["Kafka:InputTopic"] ?? "flights",
    OutputTopic = configuration["Kafka:OutputTopic"] ?? "flight-matches",
    GroupId = configuration["Kafka:GroupId"] ?? "flight-matching-group"
};

var connectionString =
    configuration["Database:ConnectionString"]
    ?? throw new InvalidOperationException(
        "Database:ConnectionString is missing.");

var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        options => options.UseNetTopologySuite())
    .Options;

using var dbContext = new AppDbContext(dbOptions);

await dbContext.Database.CanConnectAsync();

Console.WriteLine("MySQL connection successful.");
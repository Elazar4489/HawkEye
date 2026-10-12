using FlightMatching.Configuration;
using FlightMatching.Data;
using FlightMatching.Services;
using FlightMatching.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var kafkaOptions = new KafkaOptions
{
    BootstrapServers = configuration["Kafka:BootstrapServers"]
        ?? "localhost:9092",
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

using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

using var dbContext = new AppDbContext(dbOptions);
using var consumer = new KafkaFlightConsumer(kafkaOptions);
using var producer = new KafkaMatchProducer(kafkaOptions);

var matcher = new MySqlFlightMatcher(dbContext);

var service = new FlightMatchingService(
    consumer,
    producer,
    matcher);

try
{
    await dbContext.Database.CanConnectAsync(
        cancellationTokenSource.Token);

    Console.WriteLine("MySQL connection successful.");

    await service.RunAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Shutdown requested.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex}");
    Environment.ExitCode = 1;
}
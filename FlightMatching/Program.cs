using FlightMatching.Configuration;
using FlightMatching.Data;
using FlightMatching.Services;
using FlightMatching.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// appsettings.json holds local defaults; environment variables (Kafka__BootstrapServers,
// Database__ConnectionString, ...) override them inside Docker.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var kafkaOptions = new KafkaOptions
{
    BootstrapServers = configuration["Kafka:BootstrapServers"]
        ?? "localhost:29092",

    InputTopic = configuration["Kafka:InputTopic"]
        ?? "flights",

    OutputTopic = configuration["Kafka:OutputTopic"]
        ?? "flight-alerts",

    GroupId = configuration["Kafka:GroupId"]
        ?? "flight-matching-group"
};

var connectionString =
    configuration["Database:ConnectionString"]
    ?? throw new InvalidOperationException(
        "Database:ConnectionString is missing.");

var cooldownMinutes = configuration.GetValue<int>(
    "Matching:CooldownMinutes",
    10);

if (cooldownMinutes < 0)
{
    throw new InvalidOperationException(
        "Matching:CooldownMinutes cannot be negative.");
}

var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
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
    matcher,
    TimeSpan.FromMinutes(cooldownMinutes));

try
{
    if (!await dbContext.Database.CanConnectAsync(
        cancellationTokenSource.Token))
    {
        throw new InvalidOperationException(
            "Could not connect to MySQL.");
    }

    Console.WriteLine("MySQL connection successful.");
    Console.WriteLine($"Cooldown: {cooldownMinutes} minute(s).");

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
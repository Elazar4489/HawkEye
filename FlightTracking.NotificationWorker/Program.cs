using Confluent.Kafka;
using FlightTracking.NotificationWorker;
using FlightTracking.NotificationWorker.Services;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings"));

//builder.Services.AddDbContext<NotificationDbContext>(options =>
//{
//    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
//});
var mysqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseMySql(mysqlConnection, ServerVersion.AutoDetect(mysqlConnection),
        mySqlOptions => mySqlOptions.UseNetTopologySuite())); // <-- זה מה שמעלים את הבעיה ומלמד את EF Core להתמודד עם NetTopologySuite

builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var kafkaSettings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;

    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaSettings.BootstrapServers,
        GroupId = kafkaSettings.GroupId,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };

    return new ConsumerBuilder<string, string>(config).Build();
});

builder.Services.AddSingleton<ProcessAlertMessage>();
builder.Services.AddHostedService<NotificationWorkerService>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    dbContext.Database.Migrate();
}
try
{
    Log.Information("Starting Notification Worker host...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}
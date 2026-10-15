using Elastic.Clients.Elasticsearch;
using FlightDashboardApi.Services;
using FlightDashboardApi;
using Microsoft.AspNetCore.Hosting.Server.Features;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration["MongoSettings:ConnectionString"];
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = builder.Configuration["MongoSettings:DatabaseName"];
    return client.GetDatabase(dbName);
});

builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var uri = builder.Configuration["ElasticSettings:Uri"] ?? "http://localhost:9200";
    var settings = new ElasticsearchClientSettings(new Uri(uri))
        .DefaultIndex(builder.Configuration["ElasticSettings:IndexName"] ?? "dashboards-index");
    
    return new ElasticsearchClient(settings);
});

builder.Services.AddScoped<FlightDashboardService>();
// 1. הגדרת פוליסת CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        // Reached through the nginx proxy in Docker; any origin is allowed for direct
        // calls from the vite dev server or another machine on the LAN.
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // קריטי עבור SignalR!
    });
});

// ...
builder.Services.AddSignalR();
var app = builder.Build();

// 2. הפעלת ה-CORS בשרשרת ה-Middleware (לפני Routing/Authorization)
app.UseCors("AllowReactApp");

// 3. מיפוי ה-SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

try
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var serverAddressesFeature = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<IServerAddressesFeature>();

        if (serverAddressesFeature != null)
        {
            foreach (var address in serverAddressesFeature.Addresses)
            {
                Log.Information("Server is running and listening on: {Address}", address);
                Console.WriteLine($"-> Server listening on: {address}");
            }
        }
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // HTTP only: TLS (if any) is terminated in front of the container.
    app.UseAuthorization();

    app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}


using Elastic.Clients.Elasticsearch;
using FlightDashboardApi.Services;
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

var app = builder.Build();

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

    app.UseHttpsRedirection();

    app.UseAuthorization();

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


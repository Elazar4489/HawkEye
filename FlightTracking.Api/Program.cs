using FlightTracking.Api.Repositories;
using FlightTracking.Api.Services;
using FlightTracking.Api.Settings;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Setting Serilog as the main log provider and reading from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// 1. Adding MySQL DbContext along with NetTopologySuite support (for points and polygons)
var mysqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mysqlConnection, Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(mysqlConnection),
        mySqlOptions => mySqlOptions.UseNetTopologySuite()));

// 2. Adding MongoDB Client and Database for Dependency Injection
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration["MongoSettings:ConnectionString"]));

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(builder.Configuration["MongoSettings:DatabaseName"]);
});

// Adding standard API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ICustomerPriorityRepository, CustomerPriorityRepository>();
builder.Services.AddScoped<ICustomerPriorityService, CustomerPriorityService>();

var app = builder.Build();

// Middleware definition
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

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
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
using System.Text;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;
using FlightTracking.Api.Services;
using FlightTracking.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// 3. JWT authentication (also used by the SignalR hub through ?access_token=)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names as issued ("sub", "role") instead of the long WS-* URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Browsers cannot set headers on WebSocket requests.
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Adding standard API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ICustomerPriorityRepository, CustomerPriorityRepository>();
builder.Services.AddScoped<ICustomerPriorityService, CustomerPriorityService>();
// 1. הגדרת פוליסת CORS
// In Docker the browser reaches the API through the nginx proxy (same origin), so CORS
// only matters when the API is called directly (vite dev server, another LAN machine).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // קריטי עבור SignalR!
    });
});

var app = builder.Build();

// 2. הפעלת ה-CORS בשרשרת ה-Middleware (לפני Routing/Authorization)
app.UseCors("AllowReactApp");


// Middleware definition
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Migrations run before Kestrel opens its port, so the docker healthcheck only turns
// green once the schema is ready for the Matching service and the NotificationWorker.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
    await userService.EnsureAdminAsync(
        app.Configuration["Seed:AdminEmail"] ?? string.Empty,
        app.Configuration["Seed:AdminPassword"] ?? string.Empty);
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.MapControllers();

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

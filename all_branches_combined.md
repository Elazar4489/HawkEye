# Complete Repository Snapshot (All Branches & Team Code)

This file contains the full source code across all local and remote branches.



==================================================
# BRANCH: data-model/user-management
# Commit Hash: 5ffde9d3157cd95e5e85c1d715063b2c3ec8dad8
==================================================


--- FILE: README.md (Branch Path) ---
```md
# HawkEye

HawkEye היא מערכת מתקדמת לניטור, מעקב והתראות על טיסות בזמן אמת, המבוססת על הגדרות גיאוגרפיות מותאמות אישית (נקודת ציון עם רדיוס או פוליגון מורכב).

המערכת מאפשרת למשתמשים להגדיר אזורי עניין על גבי מפה, לנטר טיסות החוצות אזורים אלו, ולקבל התראות מיידיות בערוצי תקשורת שונים.

---

```

--- FILE: docker-compose.yaml (Branch Path) ---
```yaml
services:
  mysql:
    image: mysql:9.7
    container_name: mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  mongodb:
    image: mongo:8
    container_name: mongodb
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD}
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db

  kafka:
    image: apache/kafka:4.1.0
    container_name: kafka
    restart: unless-stopped
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
    ports:
      - "9092:9092"
    volumes:
      - kafka_data:/var/lib/kafka/data

volumes:
  mysql_data:
  mongodb_data:
  kafka_data:
```

--- FILE: FlightProducer\Program.cs (Branch Path) ---
```cs
﻿using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OpenSkyOptions>(
    builder.Configuration.GetSection("OpenSky"));

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));


builder.Services.AddHttpClient<IOpenSkyClient, OpenSkyClient>();

builder.Services.AddHostedService<FlightPollingService>();

builder.Services.AddSingleton<IFlightProducer, KafkaFlightProducer>();

var host = builder.Build();

await host.RunAsync();
```

--- FILE: FlightProducer\appsettings.json (Branch Path) ---
```json
{
  "OpenSky": {
    "BaseUrl": "https://opensky-network.org",
    "PollingIntervalSeconds": 200000
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "flights"
  }
}
```

--- FILE: FlightProducer\Configuration\KafkaOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}

```

--- FILE: FlightProducer\Configuration\OpenSkyOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class OpenSkyOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; }
    }
}

```

--- FILE: FlightProducer\Interfaces\IFlightProducer.cs (Branch Path) ---
```cs
﻿
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IFlightProducer
{
    Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken);


}

```

--- FILE: FlightProducer\Interfaces\IOpenSkyClient.cs (Branch Path) ---
```cs
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IOpenSkyClient
{
    Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken);
}
```

--- FILE: FlightProducer\Models\FlightState.cs (Branch Path) ---
```cs
﻿namespace FlightProducer.Models;

public class FlightState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string OriginCountry { get; set; } = string.Empty;

    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }

    public bool OnGround { get; set; }
}
```

--- FILE: FlightProducer\Services\FlightPollingService.cs (Branch Path) ---
```cs
﻿using System.Diagnostics;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly IFlightProducer _flightProducer;
    private readonly ILogger<FlightPollingService> _logger;
    private readonly OpenSkyOptions _options;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        IFlightProducer flightProducer,
        ILogger<FlightPollingService> logger,
        IOptions<OpenSkyOptions> options)
    {
        _openSkyClient = openSkyClient;
        _flightProducer = flightProducer;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var flights = await _openSkyClient.GetFlightsAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Received {FlightCount} flights from OpenSky",
                    flights.Count);



                const int batchSize = 500;
                var sentCount = 0;
                var stopwatch = Stopwatch.StartNew();
                var failedCount = 0;

                for (int i = 0; i < flights.Count; i += batchSize)
                {
                    var batch = flights
                        .Skip(i)
                        .Take(batchSize)
                        .Select(flight =>
                            TryProduceAsync(flight, stoppingToken))
                        .ToArray();

                    var results = await Task.WhenAll(batch);

                    sentCount += results.Count(success => success);
                    failedCount += results.Count(success => !success);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Kafka publishing completed. Sent: {SentCount}, Failed: {FailedCount}, Duration: {ElapsedSeconds:F2}s, Rate: {MessagesPerSecond:F0} messages/sec",
                    sentCount,
                    failedCount,
                    stopwatch.Elapsed.TotalSeconds,
                    stopwatch.Elapsed.TotalSeconds > 0
                        ? sentCount / stopwatch.Elapsed.TotalSeconds
                        : 0);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed during flight polling or Kafka publishing");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }


    private async Task<bool> TryProduceAsync(
    FlightState flight,
    CancellationToken cancellationToken)
    {
        try
        {
            await _flightProducer.ProduceAsync(
                flight,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish flight {Icao24}",
                flight.Icao24);

            return false;
        }
    }
}
```

--- FILE: FlightProducer\Services\KafkaFlightProducer.cs (Branch Path) ---
```cs
﻿using Confluent.Kafka;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightProducer.Services;

public class KafkaFlightProducer : IFlightProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFlightProducer> _logger;

    private const int MaxRetries = 2;
    private const int InitialRetryDelayMs = 1000;

    public KafkaFlightProducer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaFlightProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        var tries = 0;
        var delay = InitialRetryDelayMs;

        var message = new Message<string, string>
        {
            Key = flight.Icao24,
            Value = JsonSerializer.Serialize(flight)
        };

        while (tries <= MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _producer.ProduceAsync(
                    _options.Topic,
                    message,
                    cancellationToken);

                _logger.LogDebug(
                    "Delivered flight {Icao24} to {Topic}, partition {Partition}, offset {Offset}",
                    flight.Icao24,
                    result.Topic,
                    result.Partition,
                    result.Offset);

                return;
            }
            catch (ProduceException<string, string> ex)
            {
                if (tries == MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send flight {Icao24} to Kafka after {Attempts} attempts",
                        flight.Icao24,
                        tries + 1);

                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Failed to send flight {Icao24}. Attempt {Attempt} failed. Retrying in {DelayMs} ms",
                    flight.Icao24,
                    tries + 1,
                    delay);

                await Task.Delay(delay, cancellationToken);

                delay *= 2;
                tries++;
            }
        }

        throw new InvalidOperationException(
            $"Failed to publish flight {flight.Icao24} after {MaxRetries + 1} attempts.");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
```

--- FILE: FlightProducer\Services\OpenSkyClient.cs (Branch Path) ---
```cs
using System.Text.Json;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class OpenSkyClient : IOpenSkyClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenSkyOptions _options;

    public OpenSkyClient(
        HttpClient httpClient,
        IOptions<OpenSkyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/states/all";

        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        var root = document.RootElement;
        var flights = new List<FlightState>();

        if (!root.TryGetProperty("states", out var states) ||
            states.ValueKind != JsonValueKind.Array)
        {
            return flights;
        }

        foreach (var state in states.EnumerateArray())
        {
            if (state.ValueKind != JsonValueKind.Array ||
                state.GetArrayLength() < 10)
            {
                continue;
            }

            var icao24 = GetString(state, 0);

            if (string.IsNullOrWhiteSpace(icao24))
            {
                continue;
            }

            flights.Add(new FlightState
            {
                Icao24 = icao24,
                Callsign = GetString(state, 1)?.Trim(),
                OriginCountry = GetString(state, 2) ?? string.Empty,
                Longitude = GetDouble(state, 5),
                Latitude = GetDouble(state, 6),
                BaroAltitude = GetDouble(state, 7),
                OnGround = GetBoolean(state, 8),
                Velocity = GetDouble(state, 9)
            });
        }

        return flights;
    }

    private static string? GetString(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double? GetDouble(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.Number &&
               value.TryGetDouble(out var result)
            ? result
            : null;
    }

    private static bool GetBoolean(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.True;
    }
}

```

--- FILE: FlightTracking.Api\Program.cs (Branch Path) ---
```cs
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

builder.Services.AddSignalR();

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

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
```

--- FILE: FlightTracking.Api\appsettings.Development.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

```

--- FILE: FlightTracking.Api\appsettings.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;"
  },
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "flight_tracking_db"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "flight-tracking-logs-{0:yyyy.MM.dd}",
          "autoRegisterTemplate": true
        }
      }
    ]
  }
}
```

--- FILE: FlightTracking.Api\Controllers\CustomerPrioritiesController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/priorities")]
    public class CustomerPrioritiesController : ControllerBase
    {
        private readonly ICustomerPriorityService _service;

        public CustomerPrioritiesController(ICustomerPriorityService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerPriorityResponseDto>>> GetUserPriorities(int userId)
        {
            var priorities = await _service.GetUserPrioritiesAsync(userId);
            return Ok(priorities);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerPriorityResponseDto>> CreatePriority(int userId, [FromBody] CreateCustomerPriorityDto dto)
        {
            if (userId != dto.UserId)
            {
                return BadRequest(new { message = "UserId in route does not match body." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _service.CreatePriorityAsync(dto);
            return CreatedAtAction(nameof(GetUserPriorities), new { userId = created.UserId }, created);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePriority(int userId, int id, [FromBody] UpdateCustomerPriorityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _service.UpdatePriorityAsync(id, dto);
            if (!updated)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent(); // מחזיר 204 הצלחה
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePriority(int id)
        {
            var deleted = await _service.DeletePriorityAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\Controllers\UsersController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UzerId }, createdUser);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!updated)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\CoordinateDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // DTO לקבלת נקודה בודדת (קו אורך וקו רוחב)
    public class CoordinateDto
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    // DTO ליצירת אזור עניין חדש (או רדיוס או פוליגון)
    public class CreateCustomerPriorityDto
    {
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון (רשימת קודקודים)
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }

    // DTO להצגת הנתונים חזרה ללקוח
    public class CustomerPriorityResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty;
        public decimal? RadiusMeters { get; set; }
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
    public class UpdateCustomerPriorityDto
    {
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\UserDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // נתונים שמתקבלים בעת יצירת/הרשמת משתמש
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }

    // נתונים שחוזרים ללקוח (ללא סיסמה מטעמי אבטחה)
    public class UserResponseDto
    {
        public int UzerId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }
    public class UpdateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
        public string? Password { get; set; } // אופציונלי - אם ירצה לשנות סיסמה
    }
}
```

--- FILE: FlightTracking.Api\Migrations\20261012115136_FlightTrackingApiMigra.Designer.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261012115136_FlightTrackingApiMigra")]
    partial class FlightTrackingApiMigra
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserConnections");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\20261012115136_FlightTrackingApiMigra.cs (Branch Path) ---
```cs
﻿using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class FlightTrackingApiMigra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    uzer_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    push_token = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.uzer_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerPriorities",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AreaType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeometryData = table.Column<Geometry>(type: "geometry", nullable: true),
                    CenterPoint = table.Column<Point>(type: "point", nullable: true),
                    RadiusMeters = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPriorities", x => x.id);
                    table.ForeignKey(
                        name: "FK_CustomerPriorities_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    is_connected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_updated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConnections_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPriorities_user_id",
                table: "CustomerPriorities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserConnections_user_id",
                table: "UserConnections",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPriorities");

            migrationBuilder.DropTable(
                name: "UserConnections");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\AppDbContextModelSnapshot.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserConnections");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Models\AircraftState.cs (Branch Path) ---
```cs
﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlightTracking.Api.Models
{
    public class AircraftStateDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("icao24")]
        public string Icao24 { get; set; } = string.Empty;

        [BsonElement("callsign")]
        public string? Callsign { get; set; }

        [BsonElement("origin_country")]
        public string? OriginCountry { get; set; }

        [BsonElement("time_position")]
        public long? TimePosition { get; set; }

        [BsonElement("last_contact")]
        public long? LastContact { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("baro_altitude")]
        public double? BaroAltitude { get; set; }

        [BsonElement("on_ground")]
        public bool OnGround { get; set; }

        [BsonElement("velocity")]
        public double? Velocity { get; set; }

        [BsonElement("true_track")]
        public double? TrueTrack { get; set; }

        [BsonElement("vertical_rate")]
        public double? VerticalRate { get; set; }

        [BsonElement("geo_altitude")]
        public double? GeoAltitude { get; set; }

        [BsonElement("squawk")]
        public string? Squawk { get; set; }

        [BsonElement("spi")]
        public bool Spi { get; set; }

        [BsonElement("position_source")]
        public int PositionSource { get; set; }

        [BsonElement("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
```

--- FILE: FlightTracking.Api\Models\CustomerPriority.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Models
{
    [Table("CustomerPriorities")]
    public class CustomerPriority
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AreaType")]
        public string AreaType { get; set; } = string.Empty; // "RADIUS" / "POLYGON"

        // Central geometric field (for polygon or converted circle)
        [Column("GeometryData", TypeName = "GEOMETRY")]
        public Geometry? GeometryData { get; set; }

        // Center point (only relevant for radius)
        [Column("CenterPoint", TypeName = "POINT")]
        public Point? CenterPoint { get; set; }

        [Column("RadiusMeters")]
        public decimal? RadiusMeters { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\Models\User.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightTracking.Api.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("uzer_id")]
        public int UzerId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("user_name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Column("email")] 
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string? Fone { get; set; }

        [MaxLength(500)]
        [Column("push_token")]
        public string? PushToken { get; set; }
        public ICollection<CustomerPriority> CustomerPriorities { get; set; } = new List<CustomerPriority>();
    }
}
```

--- FILE: FlightTracking.Api\Models\UserConnection.cs (Branch Path) ---
```cs
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracking.Api.Models
{
    public class UserConnection
    {
        [Key]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("is_connected")]
        public bool IsConnected { get; set; }

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

```

--- FILE: FlightTracking.Api\Properties\launchSettings.json (Branch Path) ---
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:44321",
      "sslPort": 44321
    }
  },
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

--- FILE: FlightTracking.Api\Repositories\CustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class CustomerPriorityRepository : ICustomerPriorityRepository
    {
        private readonly AppDbContext _context;

        public CustomerPriorityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustomerPriority>> GetAllAsync()
        {
            return await _context.CustomerPriorities.ToListAsync();
        }

        public async Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId)
        {
            return await _context.CustomerPriorities
                .Where(cp => cp.UserId == userId)
                .ToListAsync();
        }

        public async Task<CustomerPriority?> GetByIdAsync(int id)
        {
            return await _context.CustomerPriorities.FindAsync(id);
        }

        public async Task<CustomerPriority> AddAsync(CustomerPriority customerPriority)
        {
            await _context.CustomerPriorities.AddAsync(customerPriority);
            return customerPriority;
        }

        // <-- הוסף את מתודת העדכון לכאן
        public void Update(CustomerPriority customerPriority)
        {
            _context.CustomerPriorities.Update(customerPriority);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.CustomerPriorities.Remove(entity);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Repositories\ICustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface ICustomerPriorityRepository
    {
        Task<IEnumerable<CustomerPriority>> GetAllAsync();
        Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId);
        Task<CustomerPriority?> GetByIdAsync(int id);
        Task<CustomerPriority> AddAsync(CustomerPriority customerPriority);
        void Update(CustomerPriority customerPriority); // <-- זה מה שצריך להוסיף כאן
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\IUserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\UserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\CustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Services
{
    public class CustomerPriorityService : ICustomerPriorityService
    {
        private readonly ICustomerPriorityRepository _repository;
        // יצירת Factory גיאומטרי עם SRID 4326 (מערכת קואורדינטות GPS עולמית)
        private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public CustomerPriorityService(ICustomerPriorityRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId)
        {
            var priorities = await _repository.GetByUserIdAsync(userId);
            return priorities.Select(MapToDto);
        }

        public async Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto)
        {
            var entity = new CustomerPriority
            {
                UserId = dto.UserId,
                Priority = dto.Priority,
                AreaType = dto.AreaType.ToUpper(),
                RadiusMeters = dto.RadiusMeters
            };

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                // יצירת נקודת מרכז (X = קו אורך, Y = קו רוחב)
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                // פוליגון חייב להיות סגור - אם הנקודה האחרונה שונה מהראשונה, נוסיף אותה לסוף
                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToDto(entity);
        }
        public async Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            entity.Priority = dto.Priority;
            entity.AreaType = dto.AreaType.ToUpper();
            entity.RadiusMeters = dto.RadiusMeters;

            // איפוס גיאומטריות ישנות כדי לבנות את החדשות נכון
            entity.CenterPoint = null;
            entity.GeometryData = null;

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            // ה-Repository כבר מנהל את ה-Update של ה-Entity, נשאר רק לשמור שינויים
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeletePriorityAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            await _repository.DeleteAsync(id);
            return await _repository.SaveChangesAsync();
        }

        // פונקציית עזר להמרת Entity ל-DTO חזרה
        private static CustomerPriorityResponseDto MapToDto(CustomerPriority entity)
        {
            var dto = new CustomerPriorityResponseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Priority = entity.Priority,
                AreaType = entity.AreaType,
                RadiusMeters = entity.RadiusMeters
            };

            if (entity.CenterPoint != null)
            {
                dto.CenterLongitude = entity.CenterPoint.X;
                dto.CenterLatitude = entity.CenterPoint.Y;
            }

            if (entity.GeometryData is Polygon polygon)
            {
                dto.PolygonCoordinates = polygon.Coordinates
                    .Select(c => new CoordinateDto { Longitude = c.X, Latitude = c.Y })
                    .ToList();
            }

            return dto;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\ICustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface ICustomerPriorityService
    {
        Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId);
        Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto);
        Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto);
        Task<bool> DeletePriorityAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\IUserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\NotificationHub.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.SignalR;
using FlightTracking.Api.Settings;
using FlightTracking.Api.Models;
using Microsoft.EntityFrameworkCore;

public class NotificationHub : Hub
{
    private readonly AppDbContext _context;

    public NotificationHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        // כאן הלקוח מתחבר - מעדכנים ב-DB שהמשתמש מחובר
        // (ניתן לשלוף את ה-UserId מתוך ה-Context.User אם יש אימות)
        var userIdString = Context.User?.FindFirst("sub")?.Value;
        if (int.TryParse(userIdString, out int userId))
        {
            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId);
            if (connection == null)
            {
                _context.UserConnections.Add(new UserConnection { UserId = userId, IsConnected = true });
            }
            else
            {
                connection.IsConnected = true;
                connection.LastUpdated = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // כאן הלקוח מתנתק - מעדכנים ב-DB שהמשתמש התנתק
        var userIdString = Context.User?.FindFirst("sub")?.Value;
        if (int.TryParse(userIdString, out int userId))
        {
            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId);
            if (connection != null)
            {
                connection.IsConnected = false;
                connection.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

--- FILE: FlightTracking.Api\Services\UserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;

namespace FlightTracking.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            });
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return null;

            return new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            };
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new User
            {
                UserName = dto.UserName,
                Password = dto.Password,// Note: In a real project it is recommended to hash the password
                Email = dto.Email,
                Fone = dto.Fone
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new UserResponseDto
            {
                UzerId = user.UzerId,
                UserName = user.UserName,
                Email = user.Email,
                Fone = user.Fone
            };
        }
        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.Fone = dto.Fone;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.Password = dto.Password;
            }

            await _userRepository.UpdateAsync(user);
            return await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return await _userRepository.SaveChangesAsync();
        }
    }
}
```

--- FILE: FlightTracking.Api\Settings\AppDbContext.cs (Branch Path) ---
```cs
﻿using Microsoft.EntityFrameworkCore;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Settings
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserConnection>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Program.cs (Branch Path) ---
```cs
﻿using Confluent.Kafka;
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
```

--- FILE: FlightTracking.NotificationWorker\appsettings.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:5000"
  },
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "notification-worker-group",
    "Topic": "flight-alerts"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "flight-tracking-notification-logs-{0:yyyy.MM.dd}",
          "autoRegisterTemplate": true
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  },
  "AllowedHosts": "*"
}
```

--- FILE: FlightTracking.NotificationWorker\Migrations\20261012115251_FlightTrackingNotificationWorkerMigra.Designer.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.NotificationWorker.Migrations
{
    [DbContext(typeof(NotificationDbContext))]
    [Migration("20261012115251_FlightTrackingNotificationWorkerMigra")]
    partial class FlightTrackingNotificationWorkerMigra
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsRead")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("NotificationHistories");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.Property<int?>("UserUzerId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("UserUzerId")
                        .IsUnique();

                    b.ToTable("UserConnections", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithOne("UserConnection")
                        .HasForeignKey("FlightTracking.NotificationWorker.Models.UserConnection", "UserUzerId");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");

                    b.Navigation("UserConnection");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Migrations\20261012115251_FlightTrackingNotificationWorkerMigra.cs (Branch Path) ---
```cs
﻿using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracking.NotificationWorker.Migrations
{
    /// <inheritdoc />
    public partial class FlightTrackingNotificationWorkerMigra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotificationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistories_UserId",
                table: "NotificationHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationHistories");
        }
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Migrations\NotificationDbContextModelSnapshot.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.NotificationWorker.Migrations
{
    [DbContext(typeof(NotificationDbContext))]
    partial class NotificationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsRead")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("NotificationHistories");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.Property<int?>("UserUzerId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("UserUzerId")
                        .IsUnique();

                    b.ToTable("UserConnections", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithOne("UserConnection")
                        .HasForeignKey("FlightTracking.NotificationWorker.Models.UserConnection", "UserUzerId");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");

                    b.Navigation("UserConnection");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Models\CustomerPriority.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightTracking.NotificationWorker.Models
{
    [Table("CustomerPriorities")]
    public class CustomerPriority
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AreaType")]
        public string AreaType { get; set; } = string.Empty; // "RADIUS" / "POLYGON"

        // Central geometric field (for polygon or converted circle)
        [Column("GeometryData", TypeName = "GEOMETRY")]
        public Geometry? GeometryData { get; set; }

        // Center point (only relevant for radius)
        [Column("CenterPoint", TypeName = "POINT")]
        public Point? CenterPoint { get; set; }

        [Column("RadiusMeters")]
        public decimal? RadiusMeters { get; set; }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\FlightAlertMessage.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.NotificationWorker.Models
{
    public class FlightAlertMessage
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FlightNumber { get; set; } = string.Empty;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\NotificationHistoryItem.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.NotificationWorker.Models
{
    public class NotificationHistoryItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\User.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightTracking.NotificationWorker.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("uzer_id")]
        public int UzerId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("user_name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string? Fone { get; set; }

        [MaxLength(500)]
        [Column("push_token")]
        public string? PushToken { get; set; }
        public UserConnection? UserConnection { get; set; }
        public ICollection<CustomerPriority> CustomerPriorities { get; set; } = new List<CustomerPriority>();
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\UserConnection.cs (Branch Path) ---
```cs
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracking.NotificationWorker.Models
{
    public class UserConnection
    {
        [Key]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("is_connected")]
        public bool IsConnected { get; set; }

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Services\NotificationWorkerService.cs (Branch Path) ---
```cs
using Confluent.Kafka;
using FlightTracking.NotificationWorker.Services;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightTracking.NotificationWorker
{
    public class NotificationWorkerService : BackgroundService
    {
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly IConsumer<string, string> _consumer;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ProcessAlertMessage _processAlertMessage;

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            IConsumer<string, string> consumer,
            IOptions<KafkaSettings> kafkaSettings,
            ProcessAlertMessage processAlertMessage)
        {
            _logger = logger;
            _consumer = consumer;
            _kafkaSettings = kafkaSettings.Value;
            _processAlertMessage = processAlertMessage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationWorkerService is starting, listening to topic: {Topic}", _kafkaSettings.Topic);

            _consumer.Subscribe(_kafkaSettings.Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                        if (consumeResult != null)
                        {
                            _logger.LogInformation("Received alert message from Kafka: {Message} at Key: {Key}",
                                consumeResult.Message.Value, consumeResult.Message.Key);

                            await _processAlertMessage.ProcessAlertMessageAsync(consumeResult.Message.Value, stoppingToken);

                            _consumer.Commit(consumeResult);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error occurred while consuming message from Kafka.");
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
        }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Services\ProcessAlertMessage.cs (Branch Path) ---
```cs
﻿using FirebaseAdmin.Messaging;
using FlightTracking.NotificationWorker.Models;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlightTracking.NotificationWorker.Services
{
    public class ProcessAlertMessage
    {
        private readonly ILogger<ProcessAlertMessage> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ApiSettings _apiSettings;

        public ProcessAlertMessage(
            ILogger<ProcessAlertMessage> logger,
            IServiceScopeFactory scopeFactory, 
            IOptions<ApiSettings> apiSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _apiSettings = apiSettings.Value;
        }

        public async Task ProcessAlertMessageAsync(string messagePayload, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            try
            {
                var alertData = JsonSerializer.Deserialize<FlightAlertMessage>(messagePayload);
                if (alertData == null)
                {
                    _logger.LogWarning("Failed to deserialize alert payload: {Payload}", messagePayload);
                    return;
                }

                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.UzerId == alertData.UserId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found in database.", alertData.UserId);
                    return;
                }

                bool notificationSent = false;

                if (!string.IsNullOrEmpty(user.PushToken))
                {
                    notificationSent = await SendPushNotificationAsync(user.PushToken, alertData.Message);
                    if (notificationSent)
                    {
                        _logger.LogInformation("Push notification successfully sent to device token for User ID {UserId}", user.UzerId);
                    }
                }

                if (!notificationSent)
                {
                    bool isUserConnectedRealtime = CheckIfUserIsConnectedActive(dbContext, user.UzerId);

                    if (isUserConnectedRealtime)
                    {
                        notificationSent = await SendInAppWebSocketMessageAsync(user.UzerId, alertData.Message);
                        _logger.LogInformation("In-app real-time message sent to active connection for User ID {UserId}", user.UzerId);
                    }
                    else
                    {
                        _logger.LogInformation("User ID {UserId} is offline and has no push token. Fallback to DB history only.", user.UzerId);
                    }
                }

                var historyItem = new NotificationHistoryItem
                {
                    UserId = user.UzerId,
                    Message = alertData.Message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                dbContext.NotificationHistories.Add(historyItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Alert successfully saved to NotificationHistories for User ID {UserId}", user.UzerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alert message payload.");
            }
        }

        private async Task<bool> SendPushNotificationAsync(string pushToken, string message)
        {
            try
            {
                var firebaseMessage = new Message()
                {
                    Fid = pushToken,
                    Notification = new Notification()
                    {
                        Title = "Flight alert",
                        Body = message
                    }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(firebaseMessage);
                _logger.LogInformation("FCM sent successfully: {Response}", response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM push notification.");
                return false;
            }
        }

        private bool CheckIfUserIsConnectedActive(NotificationDbContext dbContext, int userId)
        {
            var connection = dbContext.UserConnections
                .FirstOrDefault(c => c.UserId == userId);

            return connection != null && connection.IsConnected;
        }

        private async Task<bool> SendInAppWebSocketMessageAsync(int userId, string message)
        {
            try
            {
                using var client = new HttpClient();

                var payload = new
                {
                    UserId = userId,
                    Message = message
                };

                var response = await client.PostAsJsonAsync($"{_apiSettings.BaseUrl}/api/notifications/send-signalr", payload);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SignalR real-time message successfully triggered via API for User ID {UserId}", userId);
                    return true;
                }

                _logger.LogWarning("Failed to trigger SignalR message via API. Status code: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send in-app WebSocket message.");
                return false;
            }
        }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Settings\ApiSettings.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.NotificationWorker.Settings
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Settings\KafkaSettings.cs (Branch Path) ---
```cs
namespace FlightTracking.NotificationWorker.Settings
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Settings\NotificationDbContext.cs (Branch Path) ---
```cs
using Microsoft.EntityFrameworkCore;
using FlightTracking.NotificationWorker.Models;

namespace FlightTracking.NotificationWorker.Settings
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }

        public DbSet<NotificationHistoryItem> NotificationHistories { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<CustomerPriority>(entity =>
            {
                entity.ToTable("CustomerPriorities", tb => tb.ExcludeFromMigrations());
            });
            modelBuilder.Entity<UserConnection>(entity =>
            {
                entity.ToTable("UserConnections", tb => tb.ExcludeFromMigrations());
            });
            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationHistoryItem>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserConnection>()
                .HasOne<User>() 
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```


==================================================
# BRANCH: dev
# Commit Hash: 5eea1a9ae0476cd75007ef34e72e7afd1b3ded29
==================================================


--- FILE: README.md (Branch Path) ---
```md
Rar! ϐs  
       
```


==================================================
# BRANCH: main
# Commit Hash: 5eea1a9ae0476cd75007ef34e72e7afd1b3ded29
==================================================


--- FILE: README.md (Branch Path) ---
```md
Rar! ϐs  
       
```


==================================================
# BRANCH: remote/data-model/user-management
# Commit Hash: 5ffde9d3157cd95e5e85c1d715063b2c3ec8dad8
==================================================


--- FILE: README.md (Branch Path) ---
```md
# HawkEye

HawkEye היא מערכת מתקדמת לניטור, מעקב והתראות על טיסות בזמן אמת, המבוססת על הגדרות גיאוגרפיות מותאמות אישית (נקודת ציון עם רדיוס או פוליגון מורכב).

המערכת מאפשרת למשתמשים להגדיר אזורי עניין על גבי מפה, לנטר טיסות החוצות אזורים אלו, ולקבל התראות מיידיות בערוצי תקשורת שונים.

---

```

--- FILE: docker-compose.yaml (Branch Path) ---
```yaml
services:
  mysql:
    image: mysql:9.7
    container_name: mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  mongodb:
    image: mongo:8
    container_name: mongodb
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD}
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db

  kafka:
    image: apache/kafka:4.1.0
    container_name: kafka
    restart: unless-stopped
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
    ports:
      - "9092:9092"
    volumes:
      - kafka_data:/var/lib/kafka/data

volumes:
  mysql_data:
  mongodb_data:
  kafka_data:
```

--- FILE: FlightProducer\Program.cs (Branch Path) ---
```cs
﻿using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OpenSkyOptions>(
    builder.Configuration.GetSection("OpenSky"));

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));


builder.Services.AddHttpClient<IOpenSkyClient, OpenSkyClient>();

builder.Services.AddHostedService<FlightPollingService>();

builder.Services.AddSingleton<IFlightProducer, KafkaFlightProducer>();

var host = builder.Build();

await host.RunAsync();
```

--- FILE: FlightProducer\appsettings.json (Branch Path) ---
```json
{
  "OpenSky": {
    "BaseUrl": "https://opensky-network.org",
    "PollingIntervalSeconds": 200000
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "flights"
  }
}
```

--- FILE: FlightProducer\Configuration\KafkaOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}

```

--- FILE: FlightProducer\Configuration\OpenSkyOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class OpenSkyOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; }
    }
}

```

--- FILE: FlightProducer\Interfaces\IFlightProducer.cs (Branch Path) ---
```cs
﻿
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IFlightProducer
{
    Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken);


}

```

--- FILE: FlightProducer\Interfaces\IOpenSkyClient.cs (Branch Path) ---
```cs
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IOpenSkyClient
{
    Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken);
}
```

--- FILE: FlightProducer\Models\FlightState.cs (Branch Path) ---
```cs
﻿namespace FlightProducer.Models;

public class FlightState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string OriginCountry { get; set; } = string.Empty;

    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }

    public bool OnGround { get; set; }
}
```

--- FILE: FlightProducer\Services\FlightPollingService.cs (Branch Path) ---
```cs
﻿using System.Diagnostics;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly IFlightProducer _flightProducer;
    private readonly ILogger<FlightPollingService> _logger;
    private readonly OpenSkyOptions _options;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        IFlightProducer flightProducer,
        ILogger<FlightPollingService> logger,
        IOptions<OpenSkyOptions> options)
    {
        _openSkyClient = openSkyClient;
        _flightProducer = flightProducer;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var flights = await _openSkyClient.GetFlightsAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Received {FlightCount} flights from OpenSky",
                    flights.Count);



                const int batchSize = 500;
                var sentCount = 0;
                var stopwatch = Stopwatch.StartNew();
                var failedCount = 0;

                for (int i = 0; i < flights.Count; i += batchSize)
                {
                    var batch = flights
                        .Skip(i)
                        .Take(batchSize)
                        .Select(flight =>
                            TryProduceAsync(flight, stoppingToken))
                        .ToArray();

                    var results = await Task.WhenAll(batch);

                    sentCount += results.Count(success => success);
                    failedCount += results.Count(success => !success);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Kafka publishing completed. Sent: {SentCount}, Failed: {FailedCount}, Duration: {ElapsedSeconds:F2}s, Rate: {MessagesPerSecond:F0} messages/sec",
                    sentCount,
                    failedCount,
                    stopwatch.Elapsed.TotalSeconds,
                    stopwatch.Elapsed.TotalSeconds > 0
                        ? sentCount / stopwatch.Elapsed.TotalSeconds
                        : 0);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed during flight polling or Kafka publishing");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }


    private async Task<bool> TryProduceAsync(
    FlightState flight,
    CancellationToken cancellationToken)
    {
        try
        {
            await _flightProducer.ProduceAsync(
                flight,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish flight {Icao24}",
                flight.Icao24);

            return false;
        }
    }
}
```

--- FILE: FlightProducer\Services\KafkaFlightProducer.cs (Branch Path) ---
```cs
﻿using Confluent.Kafka;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightProducer.Services;

public class KafkaFlightProducer : IFlightProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFlightProducer> _logger;

    private const int MaxRetries = 2;
    private const int InitialRetryDelayMs = 1000;

    public KafkaFlightProducer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaFlightProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        var tries = 0;
        var delay = InitialRetryDelayMs;

        var message = new Message<string, string>
        {
            Key = flight.Icao24,
            Value = JsonSerializer.Serialize(flight)
        };

        while (tries <= MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _producer.ProduceAsync(
                    _options.Topic,
                    message,
                    cancellationToken);

                _logger.LogDebug(
                    "Delivered flight {Icao24} to {Topic}, partition {Partition}, offset {Offset}",
                    flight.Icao24,
                    result.Topic,
                    result.Partition,
                    result.Offset);

                return;
            }
            catch (ProduceException<string, string> ex)
            {
                if (tries == MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send flight {Icao24} to Kafka after {Attempts} attempts",
                        flight.Icao24,
                        tries + 1);

                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Failed to send flight {Icao24}. Attempt {Attempt} failed. Retrying in {DelayMs} ms",
                    flight.Icao24,
                    tries + 1,
                    delay);

                await Task.Delay(delay, cancellationToken);

                delay *= 2;
                tries++;
            }
        }

        throw new InvalidOperationException(
            $"Failed to publish flight {flight.Icao24} after {MaxRetries + 1} attempts.");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
```

--- FILE: FlightProducer\Services\OpenSkyClient.cs (Branch Path) ---
```cs
using System.Text.Json;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class OpenSkyClient : IOpenSkyClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenSkyOptions _options;

    public OpenSkyClient(
        HttpClient httpClient,
        IOptions<OpenSkyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/states/all";

        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        var root = document.RootElement;
        var flights = new List<FlightState>();

        if (!root.TryGetProperty("states", out var states) ||
            states.ValueKind != JsonValueKind.Array)
        {
            return flights;
        }

        foreach (var state in states.EnumerateArray())
        {
            if (state.ValueKind != JsonValueKind.Array ||
                state.GetArrayLength() < 10)
            {
                continue;
            }

            var icao24 = GetString(state, 0);

            if (string.IsNullOrWhiteSpace(icao24))
            {
                continue;
            }

            flights.Add(new FlightState
            {
                Icao24 = icao24,
                Callsign = GetString(state, 1)?.Trim(),
                OriginCountry = GetString(state, 2) ?? string.Empty,
                Longitude = GetDouble(state, 5),
                Latitude = GetDouble(state, 6),
                BaroAltitude = GetDouble(state, 7),
                OnGround = GetBoolean(state, 8),
                Velocity = GetDouble(state, 9)
            });
        }

        return flights;
    }

    private static string? GetString(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double? GetDouble(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.Number &&
               value.TryGetDouble(out var result)
            ? result
            : null;
    }

    private static bool GetBoolean(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.True;
    }
}

```

--- FILE: FlightTracking.Api\Program.cs (Branch Path) ---
```cs
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

builder.Services.AddSignalR();

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

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
```

--- FILE: FlightTracking.Api\appsettings.Development.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

```

--- FILE: FlightTracking.Api\appsettings.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;"
  },
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "flight_tracking_db"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "flight-tracking-logs-{0:yyyy.MM.dd}",
          "autoRegisterTemplate": true
        }
      }
    ]
  }
}
```

--- FILE: FlightTracking.Api\Controllers\CustomerPrioritiesController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/priorities")]
    public class CustomerPrioritiesController : ControllerBase
    {
        private readonly ICustomerPriorityService _service;

        public CustomerPrioritiesController(ICustomerPriorityService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerPriorityResponseDto>>> GetUserPriorities(int userId)
        {
            var priorities = await _service.GetUserPrioritiesAsync(userId);
            return Ok(priorities);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerPriorityResponseDto>> CreatePriority(int userId, [FromBody] CreateCustomerPriorityDto dto)
        {
            if (userId != dto.UserId)
            {
                return BadRequest(new { message = "UserId in route does not match body." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _service.CreatePriorityAsync(dto);
            return CreatedAtAction(nameof(GetUserPriorities), new { userId = created.UserId }, created);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePriority(int userId, int id, [FromBody] UpdateCustomerPriorityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _service.UpdatePriorityAsync(id, dto);
            if (!updated)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent(); // מחזיר 204 הצלחה
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePriority(int id)
        {
            var deleted = await _service.DeletePriorityAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\Controllers\UsersController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UzerId }, createdUser);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!updated)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\CoordinateDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // DTO לקבלת נקודה בודדת (קו אורך וקו רוחב)
    public class CoordinateDto
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    // DTO ליצירת אזור עניין חדש (או רדיוס או פוליגון)
    public class CreateCustomerPriorityDto
    {
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון (רשימת קודקודים)
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }

    // DTO להצגת הנתונים חזרה ללקוח
    public class CustomerPriorityResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty;
        public decimal? RadiusMeters { get; set; }
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
    public class UpdateCustomerPriorityDto
    {
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\UserDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // נתונים שמתקבלים בעת יצירת/הרשמת משתמש
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }

    // נתונים שחוזרים ללקוח (ללא סיסמה מטעמי אבטחה)
    public class UserResponseDto
    {
        public int UzerId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }
    public class UpdateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
        public string? Password { get; set; } // אופציונלי - אם ירצה לשנות סיסמה
    }
}
```

--- FILE: FlightTracking.Api\Migrations\20261012115136_FlightTrackingApiMigra.Designer.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261012115136_FlightTrackingApiMigra")]
    partial class FlightTrackingApiMigra
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserConnections");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\20261012115136_FlightTrackingApiMigra.cs (Branch Path) ---
```cs
﻿using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class FlightTrackingApiMigra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    uzer_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    push_token = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.uzer_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerPriorities",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AreaType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeometryData = table.Column<Geometry>(type: "geometry", nullable: true),
                    CenterPoint = table.Column<Point>(type: "point", nullable: true),
                    RadiusMeters = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPriorities", x => x.id);
                    table.ForeignKey(
                        name: "FK_CustomerPriorities_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    is_connected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_updated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConnections_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPriorities_user_id",
                table: "CustomerPriorities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserConnections_user_id",
                table: "UserConnections",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPriorities");

            migrationBuilder.DropTable(
                name: "UserConnections");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\AppDbContextModelSnapshot.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserConnections");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Models\AircraftState.cs (Branch Path) ---
```cs
﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlightTracking.Api.Models
{
    public class AircraftStateDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("icao24")]
        public string Icao24 { get; set; } = string.Empty;

        [BsonElement("callsign")]
        public string? Callsign { get; set; }

        [BsonElement("origin_country")]
        public string? OriginCountry { get; set; }

        [BsonElement("time_position")]
        public long? TimePosition { get; set; }

        [BsonElement("last_contact")]
        public long? LastContact { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("baro_altitude")]
        public double? BaroAltitude { get; set; }

        [BsonElement("on_ground")]
        public bool OnGround { get; set; }

        [BsonElement("velocity")]
        public double? Velocity { get; set; }

        [BsonElement("true_track")]
        public double? TrueTrack { get; set; }

        [BsonElement("vertical_rate")]
        public double? VerticalRate { get; set; }

        [BsonElement("geo_altitude")]
        public double? GeoAltitude { get; set; }

        [BsonElement("squawk")]
        public string? Squawk { get; set; }

        [BsonElement("spi")]
        public bool Spi { get; set; }

        [BsonElement("position_source")]
        public int PositionSource { get; set; }

        [BsonElement("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
```

--- FILE: FlightTracking.Api\Models\CustomerPriority.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Models
{
    [Table("CustomerPriorities")]
    public class CustomerPriority
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AreaType")]
        public string AreaType { get; set; } = string.Empty; // "RADIUS" / "POLYGON"

        // Central geometric field (for polygon or converted circle)
        [Column("GeometryData", TypeName = "GEOMETRY")]
        public Geometry? GeometryData { get; set; }

        // Center point (only relevant for radius)
        [Column("CenterPoint", TypeName = "POINT")]
        public Point? CenterPoint { get; set; }

        [Column("RadiusMeters")]
        public decimal? RadiusMeters { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\Models\User.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightTracking.Api.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("uzer_id")]
        public int UzerId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("user_name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Column("email")] 
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string? Fone { get; set; }

        [MaxLength(500)]
        [Column("push_token")]
        public string? PushToken { get; set; }
        public ICollection<CustomerPriority> CustomerPriorities { get; set; } = new List<CustomerPriority>();
    }
}
```

--- FILE: FlightTracking.Api\Models\UserConnection.cs (Branch Path) ---
```cs
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracking.Api.Models
{
    public class UserConnection
    {
        [Key]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("is_connected")]
        public bool IsConnected { get; set; }

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

```

--- FILE: FlightTracking.Api\Properties\launchSettings.json (Branch Path) ---
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:44321",
      "sslPort": 44321
    }
  },
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

--- FILE: FlightTracking.Api\Repositories\CustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class CustomerPriorityRepository : ICustomerPriorityRepository
    {
        private readonly AppDbContext _context;

        public CustomerPriorityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustomerPriority>> GetAllAsync()
        {
            return await _context.CustomerPriorities.ToListAsync();
        }

        public async Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId)
        {
            return await _context.CustomerPriorities
                .Where(cp => cp.UserId == userId)
                .ToListAsync();
        }

        public async Task<CustomerPriority?> GetByIdAsync(int id)
        {
            return await _context.CustomerPriorities.FindAsync(id);
        }

        public async Task<CustomerPriority> AddAsync(CustomerPriority customerPriority)
        {
            await _context.CustomerPriorities.AddAsync(customerPriority);
            return customerPriority;
        }

        // <-- הוסף את מתודת העדכון לכאן
        public void Update(CustomerPriority customerPriority)
        {
            _context.CustomerPriorities.Update(customerPriority);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.CustomerPriorities.Remove(entity);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Repositories\ICustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface ICustomerPriorityRepository
    {
        Task<IEnumerable<CustomerPriority>> GetAllAsync();
        Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId);
        Task<CustomerPriority?> GetByIdAsync(int id);
        Task<CustomerPriority> AddAsync(CustomerPriority customerPriority);
        void Update(CustomerPriority customerPriority); // <-- זה מה שצריך להוסיף כאן
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\IUserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\UserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\CustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Services
{
    public class CustomerPriorityService : ICustomerPriorityService
    {
        private readonly ICustomerPriorityRepository _repository;
        // יצירת Factory גיאומטרי עם SRID 4326 (מערכת קואורדינטות GPS עולמית)
        private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public CustomerPriorityService(ICustomerPriorityRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId)
        {
            var priorities = await _repository.GetByUserIdAsync(userId);
            return priorities.Select(MapToDto);
        }

        public async Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto)
        {
            var entity = new CustomerPriority
            {
                UserId = dto.UserId,
                Priority = dto.Priority,
                AreaType = dto.AreaType.ToUpper(),
                RadiusMeters = dto.RadiusMeters
            };

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                // יצירת נקודת מרכז (X = קו אורך, Y = קו רוחב)
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                // פוליגון חייב להיות סגור - אם הנקודה האחרונה שונה מהראשונה, נוסיף אותה לסוף
                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToDto(entity);
        }
        public async Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            entity.Priority = dto.Priority;
            entity.AreaType = dto.AreaType.ToUpper();
            entity.RadiusMeters = dto.RadiusMeters;

            // איפוס גיאומטריות ישנות כדי לבנות את החדשות נכון
            entity.CenterPoint = null;
            entity.GeometryData = null;

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            // ה-Repository כבר מנהל את ה-Update של ה-Entity, נשאר רק לשמור שינויים
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeletePriorityAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            await _repository.DeleteAsync(id);
            return await _repository.SaveChangesAsync();
        }

        // פונקציית עזר להמרת Entity ל-DTO חזרה
        private static CustomerPriorityResponseDto MapToDto(CustomerPriority entity)
        {
            var dto = new CustomerPriorityResponseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Priority = entity.Priority,
                AreaType = entity.AreaType,
                RadiusMeters = entity.RadiusMeters
            };

            if (entity.CenterPoint != null)
            {
                dto.CenterLongitude = entity.CenterPoint.X;
                dto.CenterLatitude = entity.CenterPoint.Y;
            }

            if (entity.GeometryData is Polygon polygon)
            {
                dto.PolygonCoordinates = polygon.Coordinates
                    .Select(c => new CoordinateDto { Longitude = c.X, Latitude = c.Y })
                    .ToList();
            }

            return dto;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\ICustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface ICustomerPriorityService
    {
        Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId);
        Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto);
        Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto);
        Task<bool> DeletePriorityAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\IUserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\NotificationHub.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.SignalR;
using FlightTracking.Api.Settings;
using FlightTracking.Api.Models;
using Microsoft.EntityFrameworkCore;

public class NotificationHub : Hub
{
    private readonly AppDbContext _context;

    public NotificationHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        // כאן הלקוח מתחבר - מעדכנים ב-DB שהמשתמש מחובר
        // (ניתן לשלוף את ה-UserId מתוך ה-Context.User אם יש אימות)
        var userIdString = Context.User?.FindFirst("sub")?.Value;
        if (int.TryParse(userIdString, out int userId))
        {
            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId);
            if (connection == null)
            {
                _context.UserConnections.Add(new UserConnection { UserId = userId, IsConnected = true });
            }
            else
            {
                connection.IsConnected = true;
                connection.LastUpdated = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // כאן הלקוח מתנתק - מעדכנים ב-DB שהמשתמש התנתק
        var userIdString = Context.User?.FindFirst("sub")?.Value;
        if (int.TryParse(userIdString, out int userId))
        {
            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId);
            if (connection != null)
            {
                connection.IsConnected = false;
                connection.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

--- FILE: FlightTracking.Api\Services\UserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;

namespace FlightTracking.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            });
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return null;

            return new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            };
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new User
            {
                UserName = dto.UserName,
                Password = dto.Password,// Note: In a real project it is recommended to hash the password
                Email = dto.Email,
                Fone = dto.Fone
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new UserResponseDto
            {
                UzerId = user.UzerId,
                UserName = user.UserName,
                Email = user.Email,
                Fone = user.Fone
            };
        }
        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.Fone = dto.Fone;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.Password = dto.Password;
            }

            await _userRepository.UpdateAsync(user);
            return await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return await _userRepository.SaveChangesAsync();
        }
    }
}
```

--- FILE: FlightTracking.Api\Settings\AppDbContext.cs (Branch Path) ---
```cs
﻿using Microsoft.EntityFrameworkCore;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Settings
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserConnection>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Program.cs (Branch Path) ---
```cs
﻿using Confluent.Kafka;
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
```

--- FILE: FlightTracking.NotificationWorker\appsettings.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:5000"
  },
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "notification-worker-group",
    "Topic": "flight-alerts"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "flight-tracking-notification-logs-{0:yyyy.MM.dd}",
          "autoRegisterTemplate": true
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  },
  "AllowedHosts": "*"
}
```

--- FILE: FlightTracking.NotificationWorker\Migrations\20261012115251_FlightTrackingNotificationWorkerMigra.Designer.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.NotificationWorker.Migrations
{
    [DbContext(typeof(NotificationDbContext))]
    [Migration("20261012115251_FlightTrackingNotificationWorkerMigra")]
    partial class FlightTrackingNotificationWorkerMigra
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsRead")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("NotificationHistories");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.Property<int?>("UserUzerId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("UserUzerId")
                        .IsUnique();

                    b.ToTable("UserConnections", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithOne("UserConnection")
                        .HasForeignKey("FlightTracking.NotificationWorker.Models.UserConnection", "UserUzerId");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");

                    b.Navigation("UserConnection");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Migrations\20261012115251_FlightTrackingNotificationWorkerMigra.cs (Branch Path) ---
```cs
﻿using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracking.NotificationWorker.Migrations
{
    /// <inheritdoc />
    public partial class FlightTrackingNotificationWorkerMigra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotificationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistories_UserId",
                table: "NotificationHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationHistories");
        }
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Migrations\NotificationDbContextModelSnapshot.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using System;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.NotificationWorker.Migrations
{
    [DbContext(typeof(NotificationDbContext))]
    partial class NotificationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsRead")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("NotificationHistories");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("PushToken")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("push_token");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsConnected")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_connected");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("last_updated");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.Property<int?>("UserUzerId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("UserUzerId")
                        .IsUnique();

                    b.ToTable("UserConnections", null, t =>
                        {
                            t.ExcludeFromMigrations();
                        });
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.NotificationHistoryItem", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.UserConnection", b =>
                {
                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FlightTracking.NotificationWorker.Models.User", null)
                        .WithOne("UserConnection")
                        .HasForeignKey("FlightTracking.NotificationWorker.Models.UserConnection", "UserUzerId");
                });

            modelBuilder.Entity("FlightTracking.NotificationWorker.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");

                    b.Navigation("UserConnection");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Models\CustomerPriority.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightTracking.NotificationWorker.Models
{
    [Table("CustomerPriorities")]
    public class CustomerPriority
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AreaType")]
        public string AreaType { get; set; } = string.Empty; // "RADIUS" / "POLYGON"

        // Central geometric field (for polygon or converted circle)
        [Column("GeometryData", TypeName = "GEOMETRY")]
        public Geometry? GeometryData { get; set; }

        // Center point (only relevant for radius)
        [Column("CenterPoint", TypeName = "POINT")]
        public Point? CenterPoint { get; set; }

        [Column("RadiusMeters")]
        public decimal? RadiusMeters { get; set; }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\FlightAlertMessage.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.NotificationWorker.Models
{
    public class FlightAlertMessage
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FlightNumber { get; set; } = string.Empty;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\NotificationHistoryItem.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.NotificationWorker.Models
{
    public class NotificationHistoryItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\User.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightTracking.NotificationWorker.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("uzer_id")]
        public int UzerId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("user_name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string? Fone { get; set; }

        [MaxLength(500)]
        [Column("push_token")]
        public string? PushToken { get; set; }
        public UserConnection? UserConnection { get; set; }
        public ICollection<CustomerPriority> CustomerPriorities { get; set; } = new List<CustomerPriority>();
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Models\UserConnection.cs (Branch Path) ---
```cs
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracking.NotificationWorker.Models
{
    public class UserConnection
    {
        [Key]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("is_connected")]
        public bool IsConnected { get; set; }

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

```

--- FILE: FlightTracking.NotificationWorker\Services\NotificationWorkerService.cs (Branch Path) ---
```cs
using Confluent.Kafka;
using FlightTracking.NotificationWorker.Services;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightTracking.NotificationWorker
{
    public class NotificationWorkerService : BackgroundService
    {
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly IConsumer<string, string> _consumer;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ProcessAlertMessage _processAlertMessage;

        public NotificationWorkerService(
            ILogger<NotificationWorkerService> logger,
            IConsumer<string, string> consumer,
            IOptions<KafkaSettings> kafkaSettings,
            ProcessAlertMessage processAlertMessage)
        {
            _logger = logger;
            _consumer = consumer;
            _kafkaSettings = kafkaSettings.Value;
            _processAlertMessage = processAlertMessage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationWorkerService is starting, listening to topic: {Topic}", _kafkaSettings.Topic);

            _consumer.Subscribe(_kafkaSettings.Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                        if (consumeResult != null)
                        {
                            _logger.LogInformation("Received alert message from Kafka: {Message} at Key: {Key}",
                                consumeResult.Message.Value, consumeResult.Message.Key);

                            await _processAlertMessage.ProcessAlertMessageAsync(consumeResult.Message.Value, stoppingToken);

                            _consumer.Commit(consumeResult);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error occurred while consuming message from Kafka.");
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
        }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Services\ProcessAlertMessage.cs (Branch Path) ---
```cs
﻿using FirebaseAdmin.Messaging;
using FlightTracking.NotificationWorker.Models;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlightTracking.NotificationWorker.Services
{
    public class ProcessAlertMessage
    {
        private readonly ILogger<ProcessAlertMessage> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ApiSettings _apiSettings;

        public ProcessAlertMessage(
            ILogger<ProcessAlertMessage> logger,
            IServiceScopeFactory scopeFactory, 
            IOptions<ApiSettings> apiSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _apiSettings = apiSettings.Value;
        }

        public async Task ProcessAlertMessageAsync(string messagePayload, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            try
            {
                var alertData = JsonSerializer.Deserialize<FlightAlertMessage>(messagePayload);
                if (alertData == null)
                {
                    _logger.LogWarning("Failed to deserialize alert payload: {Payload}", messagePayload);
                    return;
                }

                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.UzerId == alertData.UserId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found in database.", alertData.UserId);
                    return;
                }

                bool notificationSent = false;

                if (!string.IsNullOrEmpty(user.PushToken))
                {
                    notificationSent = await SendPushNotificationAsync(user.PushToken, alertData.Message);
                    if (notificationSent)
                    {
                        _logger.LogInformation("Push notification successfully sent to device token for User ID {UserId}", user.UzerId);
                    }
                }

                if (!notificationSent)
                {
                    bool isUserConnectedRealtime = CheckIfUserIsConnectedActive(dbContext, user.UzerId);

                    if (isUserConnectedRealtime)
                    {
                        notificationSent = await SendInAppWebSocketMessageAsync(user.UzerId, alertData.Message);
                        _logger.LogInformation("In-app real-time message sent to active connection for User ID {UserId}", user.UzerId);
                    }
                    else
                    {
                        _logger.LogInformation("User ID {UserId} is offline and has no push token. Fallback to DB history only.", user.UzerId);
                    }
                }

                var historyItem = new NotificationHistoryItem
                {
                    UserId = user.UzerId,
                    Message = alertData.Message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                dbContext.NotificationHistories.Add(historyItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Alert successfully saved to NotificationHistories for User ID {UserId}", user.UzerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alert message payload.");
            }
        }

        private async Task<bool> SendPushNotificationAsync(string pushToken, string message)
        {
            try
            {
                var firebaseMessage = new Message()
                {
                    Fid = pushToken,
                    Notification = new Notification()
                    {
                        Title = "Flight alert",
                        Body = message
                    }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(firebaseMessage);
                _logger.LogInformation("FCM sent successfully: {Response}", response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM push notification.");
                return false;
            }
        }

        private bool CheckIfUserIsConnectedActive(NotificationDbContext dbContext, int userId)
        {
            var connection = dbContext.UserConnections
                .FirstOrDefault(c => c.UserId == userId);

            return connection != null && connection.IsConnected;
        }

        private async Task<bool> SendInAppWebSocketMessageAsync(int userId, string message)
        {
            try
            {
                using var client = new HttpClient();

                var payload = new
                {
                    UserId = userId,
                    Message = message
                };

                var response = await client.PostAsJsonAsync($"{_apiSettings.BaseUrl}/api/notifications/send-signalr", payload);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SignalR real-time message successfully triggered via API for User ID {UserId}", userId);
                    return true;
                }

                _logger.LogWarning("Failed to trigger SignalR message via API. Status code: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send in-app WebSocket message.");
                return false;
            }
        }
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Settings\ApiSettings.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.NotificationWorker.Settings
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Settings\KafkaSettings.cs (Branch Path) ---
```cs
namespace FlightTracking.NotificationWorker.Settings
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}
```

--- FILE: FlightTracking.NotificationWorker\Settings\NotificationDbContext.cs (Branch Path) ---
```cs
using Microsoft.EntityFrameworkCore;
using FlightTracking.NotificationWorker.Models;

namespace FlightTracking.NotificationWorker.Settings
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }

        public DbSet<NotificationHistoryItem> NotificationHistories { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<CustomerPriority>(entity =>
            {
                entity.ToTable("CustomerPriorities", tb => tb.ExcludeFromMigrations());
            });
            modelBuilder.Entity<UserConnection>(entity =>
            {
                entity.ToTable("UserConnections", tb => tb.ExcludeFromMigrations());
            });
            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationHistoryItem>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserConnection>()
                .HasOne<User>() 
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```


==================================================
# BRANCH: remote/dev
# Commit Hash: f0fb3390d4b0d0aa43be2f43114ae36365dd13e7
==================================================


--- FILE: README.md (Branch Path) ---
```md
# HawkEye

HawkEye היא מערכת מתקדמת לניטור, מעקב והתראות על טיסות בזמן אמת, המבוססת על הגדרות גיאוגרפיות מותאמות אישית (נקודת ציון עם רדיוס או פוליגון מורכב).

המערכת מאפשרת למשתמשים להגדיר אזורי עניין על גבי מפה, לנטר טיסות החוצות אזורים אלו, ולקבל התראות מיידיות בערוצי תקשורת שונים.

---

```

--- FILE: docker-compose.yaml (Branch Path) ---
```yaml
services:
  mysql:
    image: mysql:9.7
    container_name: mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  mongodb:
    image: mongo:8
    container_name: mongodb
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD}
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db

  kafka:
    image: apache/kafka:4.1.0
    container_name: kafka
    restart: unless-stopped
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
    ports:
      - "9092:9092"
    volumes:
      - kafka_data:/var/lib/kafka/data

volumes:
  mysql_data:
  mongodb_data:
  kafka_data:
```

--- FILE: FlightProducer\Program.cs (Branch Path) ---
```cs
﻿using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OpenSkyOptions>(
    builder.Configuration.GetSection("OpenSky"));

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));


builder.Services.AddHttpClient<IOpenSkyClient, OpenSkyClient>();

builder.Services.AddHostedService<FlightPollingService>();

builder.Services.AddSingleton<IFlightProducer, KafkaFlightProducer>();

var host = builder.Build();

await host.RunAsync();
```

--- FILE: FlightProducer\appsettings.json (Branch Path) ---
```json
{
  "OpenSky": {
    "BaseUrl": "https://opensky-network.org",
    "PollingIntervalSeconds": 200000
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "flights"
  }
}
```

--- FILE: FlightProducer\Configuration\KafkaOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}

```

--- FILE: FlightProducer\Configuration\OpenSkyOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class OpenSkyOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; }
    }
}

```

--- FILE: FlightProducer\Interfaces\IFlightProducer.cs (Branch Path) ---
```cs
﻿
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IFlightProducer
{
    Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken);


}

```

--- FILE: FlightProducer\Interfaces\IOpenSkyClient.cs (Branch Path) ---
```cs
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IOpenSkyClient
{
    Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken);
}
```

--- FILE: FlightProducer\Models\FlightState.cs (Branch Path) ---
```cs
﻿namespace FlightProducer.Models;

public class FlightState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string OriginCountry { get; set; } = string.Empty;

    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }

    public bool OnGround { get; set; }
}
```

--- FILE: FlightProducer\Services\FlightPollingService.cs (Branch Path) ---
```cs
﻿using System.Diagnostics;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly IFlightProducer _flightProducer;
    private readonly ILogger<FlightPollingService> _logger;
    private readonly OpenSkyOptions _options;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        IFlightProducer flightProducer,
        ILogger<FlightPollingService> logger,
        IOptions<OpenSkyOptions> options)
    {
        _openSkyClient = openSkyClient;
        _flightProducer = flightProducer;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var flights = await _openSkyClient.GetFlightsAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Received {FlightCount} flights from OpenSky",
                    flights.Count);



                const int batchSize = 500;
                var sentCount = 0;
                var stopwatch = Stopwatch.StartNew();
                var failedCount = 0;

                for (int i = 0; i < flights.Count; i += batchSize)
                {
                    var batch = flights
                        .Skip(i)
                        .Take(batchSize)
                        .Select(flight =>
                            TryProduceAsync(flight, stoppingToken))
                        .ToArray();

                    var results = await Task.WhenAll(batch);

                    sentCount += results.Count(success => success);
                    failedCount += results.Count(success => !success);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Kafka publishing completed. Sent: {SentCount}, Failed: {FailedCount}, Duration: {ElapsedSeconds:F2}s, Rate: {MessagesPerSecond:F0} messages/sec",
                    sentCount,
                    failedCount,
                    stopwatch.Elapsed.TotalSeconds,
                    stopwatch.Elapsed.TotalSeconds > 0
                        ? sentCount / stopwatch.Elapsed.TotalSeconds
                        : 0);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed during flight polling or Kafka publishing");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }


    private async Task<bool> TryProduceAsync(
    FlightState flight,
    CancellationToken cancellationToken)
    {
        try
        {
            await _flightProducer.ProduceAsync(
                flight,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish flight {Icao24}",
                flight.Icao24);

            return false;
        }
    }
}
```

--- FILE: FlightProducer\Services\KafkaFlightProducer.cs (Branch Path) ---
```cs
﻿using Confluent.Kafka;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightProducer.Services;

public class KafkaFlightProducer : IFlightProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFlightProducer> _logger;

    private const int MaxRetries = 2;
    private const int InitialRetryDelayMs = 1000;

    public KafkaFlightProducer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaFlightProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        var tries = 0;
        var delay = InitialRetryDelayMs;

        var message = new Message<string, string>
        {
            Key = flight.Icao24,
            Value = JsonSerializer.Serialize(flight)
        };

        while (tries <= MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _producer.ProduceAsync(
                    _options.Topic,
                    message,
                    cancellationToken);

                _logger.LogDebug(
                    "Delivered flight {Icao24} to {Topic}, partition {Partition}, offset {Offset}",
                    flight.Icao24,
                    result.Topic,
                    result.Partition,
                    result.Offset);

                return;
            }
            catch (ProduceException<string, string> ex)
            {
                if (tries == MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send flight {Icao24} to Kafka after {Attempts} attempts",
                        flight.Icao24,
                        tries + 1);

                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Failed to send flight {Icao24}. Attempt {Attempt} failed. Retrying in {DelayMs} ms",
                    flight.Icao24,
                    tries + 1,
                    delay);

                await Task.Delay(delay, cancellationToken);

                delay *= 2;
                tries++;
            }
        }

        throw new InvalidOperationException(
            $"Failed to publish flight {flight.Icao24} after {MaxRetries + 1} attempts.");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
```

--- FILE: FlightProducer\Services\OpenSkyClient.cs (Branch Path) ---
```cs
using System.Text.Json;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class OpenSkyClient : IOpenSkyClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenSkyOptions _options;

    public OpenSkyClient(
        HttpClient httpClient,
        IOptions<OpenSkyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/states/all";

        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        var root = document.RootElement;
        var flights = new List<FlightState>();

        if (!root.TryGetProperty("states", out var states) ||
            states.ValueKind != JsonValueKind.Array)
        {
            return flights;
        }

        foreach (var state in states.EnumerateArray())
        {
            if (state.ValueKind != JsonValueKind.Array ||
                state.GetArrayLength() < 10)
            {
                continue;
            }

            var icao24 = GetString(state, 0);

            if (string.IsNullOrWhiteSpace(icao24))
            {
                continue;
            }

            flights.Add(new FlightState
            {
                Icao24 = icao24,
                Callsign = GetString(state, 1)?.Trim(),
                OriginCountry = GetString(state, 2) ?? string.Empty,
                Longitude = GetDouble(state, 5),
                Latitude = GetDouble(state, 6),
                BaroAltitude = GetDouble(state, 7),
                OnGround = GetBoolean(state, 8),
                Velocity = GetDouble(state, 9)
            });
        }

        return flights;
    }

    private static string? GetString(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double? GetDouble(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.Number &&
               value.TryGetDouble(out var result)
            ? result
            : null;
    }

    private static bool GetBoolean(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.True;
    }
}

```

--- FILE: FlightTracking.Api\Program.cs (Branch Path) ---
```cs
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
```

--- FILE: FlightTracking.Api\appsettings.Development.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

```

--- FILE: FlightTracking.Api\appsettings.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;"
  },
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "flight_tracking_db"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "flight-tracking-logs-{0:yyyy.MM.dd}",
          "autoRegisterTemplate": true
        }
      }
    ]
  }
}
```

--- FILE: FlightTracking.Api\Controllers\CustomerPrioritiesController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/priorities")]
    public class CustomerPrioritiesController : ControllerBase
    {
        private readonly ICustomerPriorityService _service;

        public CustomerPrioritiesController(ICustomerPriorityService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerPriorityResponseDto>>> GetUserPriorities(int userId)
        {
            var priorities = await _service.GetUserPrioritiesAsync(userId);
            return Ok(priorities);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerPriorityResponseDto>> CreatePriority(int userId, [FromBody] CreateCustomerPriorityDto dto)
        {
            if (userId != dto.UserId)
            {
                return BadRequest(new { message = "UserId in route does not match body." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _service.CreatePriorityAsync(dto);
            return CreatedAtAction(nameof(GetUserPriorities), new { userId = created.UserId }, created);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePriority(int userId, int id, [FromBody] UpdateCustomerPriorityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _service.UpdatePriorityAsync(id, dto);
            if (!updated)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent(); // מחזיר 204 הצלחה
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePriority(int id)
        {
            var deleted = await _service.DeletePriorityAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\Controllers\UsersController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UzerId }, createdUser);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!updated)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\CoordinateDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // DTO לקבלת נקודה בודדת (קו אורך וקו רוחב)
    public class CoordinateDto
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    // DTO ליצירת אזור עניין חדש (או רדיוס או פוליגון)
    public class CreateCustomerPriorityDto
    {
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון (רשימת קודקודים)
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }

    // DTO להצגת הנתונים חזרה ללקוח
    public class CustomerPriorityResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty;
        public decimal? RadiusMeters { get; set; }
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
    public class UpdateCustomerPriorityDto
    {
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\UserDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // נתונים שמתקבלים בעת יצירת/הרשמת משתמש
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }

    // נתונים שחוזרים ללקוח (ללא סיסמה מטעמי אבטחה)
    public class UserResponseDto
    {
        public int UzerId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }
    public class UpdateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
        public string? Password { get; set; } // אופציונלי - אם ירצה לשנות סיסמה
    }
}
```

--- FILE: FlightTracking.Api\Migrations\20261011115315_UpdateUserAndPriorities.Designer.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261011115315_UpdateUserAndPriorities")]
    partial class UpdateUserAndPriorities
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("fone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\20261011115315_UpdateUserAndPriorities.cs (Branch Path) ---
```cs
﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndPriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    uzer_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.uzer_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerPriorities",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AreaType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeometryData = table.Column<Geometry>(type: "geometry", nullable: true),
                    CenterPoint = table.Column<Point>(type: "point", nullable: true),
                    RadiusMeters = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPriorities", x => x.id);
                    table.ForeignKey(
                        name: "FK_CustomerPriorities_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPriorities_user_id",
                table: "CustomerPriorities",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPriorities");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\AppDbContextModelSnapshot.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("fone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Models\AircraftState.cs (Branch Path) ---
```cs
﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlightTracking.Api.Models
{
    public class AircraftStateDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("icao24")]
        public string Icao24 { get; set; } = string.Empty;

        [BsonElement("callsign")]
        public string? Callsign { get; set; }

        [BsonElement("origin_country")]
        public string? OriginCountry { get; set; }

        [BsonElement("time_position")]
        public long? TimePosition { get; set; }

        [BsonElement("last_contact")]
        public long? LastContact { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("baro_altitude")]
        public double? BaroAltitude { get; set; }

        [BsonElement("on_ground")]
        public bool OnGround { get; set; }

        [BsonElement("velocity")]
        public double? Velocity { get; set; }

        [BsonElement("true_track")]
        public double? TrueTrack { get; set; }

        [BsonElement("vertical_rate")]
        public double? VerticalRate { get; set; }

        [BsonElement("geo_altitude")]
        public double? GeoAltitude { get; set; }

        [BsonElement("squawk")]
        public string? Squawk { get; set; }

        [BsonElement("spi")]
        public bool Spi { get; set; }

        [BsonElement("position_source")]
        public int PositionSource { get; set; }

        [BsonElement("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
```

--- FILE: FlightTracking.Api\Models\CustomerPriority.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Models
{
    [Table("CustomerPriorities")]
    public class CustomerPriority
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AreaType")]
        public string AreaType { get; set; } = string.Empty; // "RADIUS" / "POLYGON"

        // Central geometric field (for polygon or converted circle)
        [Column("GeometryData", TypeName = "GEOMETRY")]
        public Geometry? GeometryData { get; set; }

        // Center point (only relevant for radius)
        [Column("CenterPoint", TypeName = "POINT")]
        public Point? CenterPoint { get; set; }

        [Column("RadiusMeters")]
        public decimal? RadiusMeters { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\Models\User.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightTracking.Api.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("uzer_id")]
        public int UzerId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("user_name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Column("email")] 
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("fone")]
        public string? Fone { get; set; }
        public ICollection<CustomerPriority> CustomerPriorities { get; set; } = new List<CustomerPriority>();
    }
}
```

--- FILE: FlightTracking.Api\Properties\launchSettings.json (Branch Path) ---
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:44321",
      "sslPort": 44321
    }
  },
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

--- FILE: FlightTracking.Api\Repositories\CustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class CustomerPriorityRepository : ICustomerPriorityRepository
    {
        private readonly AppDbContext _context;

        public CustomerPriorityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustomerPriority>> GetAllAsync()
        {
            return await _context.CustomerPriorities.ToListAsync();
        }

        public async Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId)
        {
            return await _context.CustomerPriorities
                .Where(cp => cp.UserId == userId)
                .ToListAsync();
        }

        public async Task<CustomerPriority?> GetByIdAsync(int id)
        {
            return await _context.CustomerPriorities.FindAsync(id);
        }

        public async Task<CustomerPriority> AddAsync(CustomerPriority customerPriority)
        {
            await _context.CustomerPriorities.AddAsync(customerPriority);
            return customerPriority;
        }

        // <-- הוסף את מתודת העדכון לכאן
        public void Update(CustomerPriority customerPriority)
        {
            _context.CustomerPriorities.Update(customerPriority);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.CustomerPriorities.Remove(entity);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Repositories\ICustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface ICustomerPriorityRepository
    {
        Task<IEnumerable<CustomerPriority>> GetAllAsync();
        Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId);
        Task<CustomerPriority?> GetByIdAsync(int id);
        Task<CustomerPriority> AddAsync(CustomerPriority customerPriority);
        void Update(CustomerPriority customerPriority); // <-- זה מה שצריך להוסיף כאן
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\IUserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\UserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\CustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Services
{
    public class CustomerPriorityService : ICustomerPriorityService
    {
        private readonly ICustomerPriorityRepository _repository;
        // יצירת Factory גיאומטרי עם SRID 4326 (מערכת קואורדינטות GPS עולמית)
        private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public CustomerPriorityService(ICustomerPriorityRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId)
        {
            var priorities = await _repository.GetByUserIdAsync(userId);
            return priorities.Select(MapToDto);
        }

        public async Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto)
        {
            var entity = new CustomerPriority
            {
                UserId = dto.UserId,
                Priority = dto.Priority,
                AreaType = dto.AreaType.ToUpper(),
                RadiusMeters = dto.RadiusMeters
            };

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                // יצירת נקודת מרכז (X = קו אורך, Y = קו רוחב)
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                // פוליגון חייב להיות סגור - אם הנקודה האחרונה שונה מהראשונה, נוסיף אותה לסוף
                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToDto(entity);
        }
        public async Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            entity.Priority = dto.Priority;
            entity.AreaType = dto.AreaType.ToUpper();
            entity.RadiusMeters = dto.RadiusMeters;

            // איפוס גיאומטריות ישנות כדי לבנות את החדשות נכון
            entity.CenterPoint = null;
            entity.GeometryData = null;

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            // ה-Repository כבר מנהל את ה-Update של ה-Entity, נשאר רק לשמור שינויים
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeletePriorityAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            await _repository.DeleteAsync(id);
            return await _repository.SaveChangesAsync();
        }

        // פונקציית עזר להמרת Entity ל-DTO חזרה
        private static CustomerPriorityResponseDto MapToDto(CustomerPriority entity)
        {
            var dto = new CustomerPriorityResponseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Priority = entity.Priority,
                AreaType = entity.AreaType,
                RadiusMeters = entity.RadiusMeters
            };

            if (entity.CenterPoint != null)
            {
                dto.CenterLongitude = entity.CenterPoint.X;
                dto.CenterLatitude = entity.CenterPoint.Y;
            }

            if (entity.GeometryData is Polygon polygon)
            {
                dto.PolygonCoordinates = polygon.Coordinates
                    .Select(c => new CoordinateDto { Longitude = c.X, Latitude = c.Y })
                    .ToList();
            }

            return dto;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\ICustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface ICustomerPriorityService
    {
        Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId);
        Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto);
        Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto);
        Task<bool> DeletePriorityAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\IUserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\UserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;

namespace FlightTracking.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            });
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return null;

            return new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            };
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new User
            {
                UserName = dto.UserName,
                Password = dto.Password,// Note: In a real project it is recommended to hash the password
                Email = dto.Email,
                Fone = dto.Fone
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new UserResponseDto
            {
                UzerId = user.UzerId,
                UserName = user.UserName,
                Email = user.Email,
                Fone = user.Fone
            };
        }
        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.Fone = dto.Fone;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.Password = dto.Password;
            }

            await _userRepository.UpdateAsync(user);
            return await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return await _userRepository.SaveChangesAsync();
        }
    }
}
```

--- FILE: FlightTracking.Api\Settings\AppDbContext.cs (Branch Path) ---
```cs
﻿using Microsoft.EntityFrameworkCore;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Settings
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```


==================================================
# BRANCH: remote/docker-compose-setup
# Commit Hash: 5fcafcb12b7a822fe396fa0ecf128eb318785d3d
==================================================


--- FILE: README.md (Branch Path) ---
```md
Rar! ϐs  
       
```

--- FILE: docker-compose.yaml (Branch Path) ---
```yaml
services:
  mysql:
    image: mysql:9.7
    container_name: mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  mongodb:
    image: mongo:8
    container_name: mongodb
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD}
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db

  kafka:
    image: apache/kafka:4.1.0
    container_name: kafka
    restart: unless-stopped
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
    ports:
      - "9092:9092"
    volumes:
      - kafka_data:/var/lib/kafka/data

volumes:
  mysql_data:
  mongodb_data:
  kafka_data:
```


==================================================
# BRANCH: remote/feature/flight-matching
# Commit Hash: 9b89d03bcbec194deb2adff2648cd9c846f88475
==================================================


--- FILE: README.md (Branch Path) ---
```md
# HawkEye

HawkEye היא מערכת מתקדמת לניטור, מעקב והתראות על טיסות בזמן אמת, המבוססת על הגדרות גיאוגרפיות מותאמות אישית (נקודת ציון עם רדיוס או פוליגון מורכב).

המערכת מאפשרת למשתמשים להגדיר אזורי עניין על גבי מפה, לנטר טיסות החוצות אזורים אלו, ולקבל התראות מיידיות בערוצי תקשורת שונים.

---

```

--- FILE: docker-compose.yaml (Branch Path) ---
```yaml
services:
  mysql:
    image: mysql:9.7
    container_name: mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  mongodb:
    image: mongo:8
    container_name: mongodb
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD}
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db

  kafka:
    image: apache/kafka:4.1.0
    container_name: kafka
    restart: unless-stopped
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
    ports:
      - "9092:9092"
    volumes:
      - kafka_data:/var/lib/kafka/data

volumes:
  mysql_data:
  mongodb_data:
  kafka_data:
```

--- FILE: FlightMatching\Program.cs (Branch Path) ---
```cs
﻿using FlightMatching.Configuration;
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

    InputTopic = configuration["Kafka:InputTopic"]
        ?? "flights",

    OutputTopic = configuration["Kafka:OutputTopic"]
        ?? "flight-matches",

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
```

--- FILE: FlightMatching\appsettings.json (Branch Path) ---
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "InputTopic": "flights",
    "OutputTopic": "flight-matches",
    "GroupId": "flight-matching-group"
  },
  "Database": {
    "ConnectionString": "Server=localhost;Port=3306;Database=flight_tracking_db;User ID=root;Password=root;"
  },

  "Matching": {
    "CooldownMinutes": 10
  }
}

```

--- FILE: FlightMatching\Configuration\DatabaseOptions.cs (Branch Path) ---
```cs
﻿
namespace FlightMatching.Configuration;

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

```

--- FILE: FlightMatching\Configuration\KafkaOptions.cs (Branch Path) ---
```cs
﻿
namespace FlightMatching.Configuration;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string InputTopic { get; set; } = "flights";
    public string OutputTopic { get; set; } = "flight-matches";
    public string GroupId { get; set; } = "flight-matching-group";
}

```

--- FILE: FlightMatching\Data\IFlightConsumer.cs (Branch Path) ---
```cs
﻿using FlightMatching.Models;


namespace FlightMatching.Data
{
    public interface IFlightConsumer
    {
        Task<FlightState?> Consume(CancellationToken cancellationToken);
    }
}

```

--- FILE: FlightMatching\Data\KafkaFlightConsumer.cs (Branch Path) ---
```cs
﻿
using Confluent.Kafka;
using FlightMatching.Configuration;

namespace FlightMatching.Data;

public sealed class KafkaFlightConsumer : IDisposable
{
    private readonly IConsumer<string, string> _consumer;

    public KafkaFlightConsumer(KafkaOptions options)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(options.InputTopic);
    }

    public ConsumeResult<string, string> Consume(
        CancellationToken cancellationToken)
    {
        return _consumer.Consume(cancellationToken);
    }

    public void Commit(ConsumeResult<string, string> result)
    {
        _consumer.Commit(result);
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }
}

```

--- FILE: FlightMatching\Data\KafkaMatchProducer.cs (Branch Path) ---
```cs
﻿
using System.Text.Json;
using Confluent.Kafka;
using FlightMatching.Configuration;
using FlightMatching.Models;

namespace FlightMatching.Data;

public sealed class KafkaMatchProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaMatchProducer(KafkaOptions options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _topic = options.OutputTopic;
    }

    public async Task ProduceAsync(
        FlightMatchMessage message,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);

        await _producer.ProduceAsync(
            _topic,
            new Message<string, string>
            {
                Key = $"{message.Icao24}:{message.UserId}",
                Value = json
            },
            cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}

```

--- FILE: FlightMatching\Data\MySqlFlightMatcher.cs (Branch Path) ---
```cs
﻿using System.Data;
using System.Data.Common;
using FlightMatching.Models;
using FlightMatching.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightMatching.Data;

public sealed class MySqlFlightMatcher
{
    private readonly AppDbContext _dbContext;

    public MySqlFlightMatcher(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<int>> FindMatchingUserIdsAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        if (flight.Longitude is null || flight.Latitude is null)
            return new List<int>();

        var pointWkt =
            $"POINT({flight.Longitude.Value} {flight.Latitude.Value})";

        var connection = _dbContext.Database.GetDbConnection();

        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT DISTINCT cp.user_id
                FROM CustomerPriorities cp
                WHERE
                    (
                        cp.AreaType = 'RADIUS'
                        AND ST_Distance(
                            cp.CenterPoint,
                            ST_GeomFromText(@flightPoint, 4326),
                            'metre'
                        ) <= cp.RadiusMeters
                    )
                    OR
                    (
                        cp.AreaType = 'POLYGON'
                        AND ST_Contains(
                            cp.GeometryData,
                            ST_GeomFromText(@flightPoint, 4326)
                        )
                    );
                """;

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@flightPoint";
            parameter.Value = pointWkt;
            parameter.DbType = DbType.String;

            command.Parameters.Add(parameter);

            var userIds = new List<int>();

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                userIds.Add(reader.GetInt32(0));
            }

            return userIds;
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
```

--- FILE: FlightMatching\Models\CustomerPriority.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightMatching.Models;

[Table("CustomerPriorities")]
public class CustomerPriority
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("Priority")]
    public int Priority { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("AreaType")]
    public string AreaType { get; set; } = string.Empty;

    [Column("GeometryData", TypeName = "GEOMETRY")]
    public Geometry? GeometryData { get; set; }

    [Column("CenterPoint", TypeName = "POINT")]
    public Point? CenterPoint { get; set; }

    [Column("RadiusMeters")]
    public decimal? RadiusMeters { get; set; }
}
```

--- FILE: FlightMatching\Models\FlightMatchMessage.cs (Branch Path) ---
```cs
﻿
namespace FlightMatching.Models;

public class FlightMatchMessage
{
    public int UserId { get; set; }
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public DateTime MatchedAtUtc { get; set; }
}

```

--- FILE: FlightMatching\Models\FlightState.cs (Branch Path) ---
```cs
﻿
namespace FlightMatching.Models;

public class FlightState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string? OriginCountry { get; set; }

    public double? Longitude { get; set; }
    public double? Latitude { get; set; }

    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }
    public bool OnGround { get; set; }
}

```

--- FILE: FlightMatching\Models\User.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightMatching.Models;

[Table("Users")]
public class User
{
    [Key]
    [Column("uzer_id")]
    public int UzerId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("fone")]
    public string? Fone { get; set; }

    public ICollection<CustomerPriority> CustomerPriorities { get; set; }
        = new List<CustomerPriority>();
}
```

--- FILE: FlightMatching\Services\FlightMatchingService.cs (Branch Path) ---
```cs
﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using FlightMatching.Data;
using FlightMatching.Models;

namespace FlightMatching.Services;

public sealed class FlightMatchingService
{
    private readonly KafkaFlightConsumer _consumer;
    private readonly KafkaMatchProducer _producer;
    private readonly MySqlFlightMatcher _matcher;
    private readonly TimeSpan _cooldown;
    private readonly Dictionary<(string Icao24, int UserId), DateTime> _lastNotifications = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public FlightMatchingService(
        KafkaFlightConsumer consumer,
        KafkaMatchProducer producer,
        MySqlFlightMatcher matcher,
        TimeSpan cooldown)
    {
        _consumer = consumer;
        _producer = producer;
        _matcher = matcher;
        _cooldown = cooldown;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("FlightMatching started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, string> result;

            try
            {
                result = _consumer.Consume(cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            FlightState? flight;

            try
            {
                flight = JsonSerializer.Deserialize<FlightState>(
                    result.Message.Value,
                    JsonOptions);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Invalid JSON: {ex.Message}");
                _consumer.Commit(result);
                continue;
            }

            if (flight is null ||
                string.IsNullOrWhiteSpace(flight.Icao24) ||
                flight.Longitude is null ||
                flight.Latitude is null)
            {
                Console.WriteLine("Skipping invalid flight message.");
                _consumer.Commit(result);
                continue;
            }

            var userIds = await _matcher.FindMatchingUserIdsAsync(
                flight,
                cancellationToken);

            foreach (var userId in userIds)
            {
                var key = (flight.Icao24, userId);
                var now = DateTime.UtcNow;

                if (_lastNotifications.TryGetValue(key, out var lastSent) &&
                    now - lastSent < _cooldown)
                {
                    Console.WriteLine(
                        $"Skipping notification for {flight.Icao24}, " +
                        $"user {userId}: cooldown active.");

                    continue;
                }

                var message = new FlightMatchMessage
                {
                    UserId = userId,
                    Icao24 = flight.Icao24,
                    Callsign = flight.Callsign,
                    Longitude = flight.Longitude.Value,
                    Latitude = flight.Latitude.Value,
                    MatchedAtUtc = now
                };

                await _producer.ProduceAsync(message, cancellationToken);

                _lastNotifications[key] = now;
            }

            _consumer.Commit(result);

            Console.WriteLine(
                $"Flight {flight.Icao24}: matched {userIds.Count} user(s).");
        }

        Console.WriteLine("FlightMatching stopped.");
    }
}
```

--- FILE: FlightMatching\Settings\AppDbContext.cs (Branch Path) ---
```cs
﻿using FlightMatching.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightMatching.Settings;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<CustomerPriority> CustomerPriorities
        => Set<CustomerPriority>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerPriority>()
            .HasOne(cp => cp.User)
            .WithMany(user => user.CustomerPriorities)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

--- FILE: FlightProducer\Program.cs (Branch Path) ---
```cs
﻿using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OpenSkyOptions>(
    builder.Configuration.GetSection("OpenSky"));

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));


builder.Services.AddHttpClient<IOpenSkyClient, OpenSkyClient>();

builder.Services.AddHostedService<FlightPollingService>();

builder.Services.AddSingleton<IFlightProducer, KafkaFlightProducer>();

var host = builder.Build();

await host.RunAsync();
```

--- FILE: FlightProducer\appsettings.json (Branch Path) ---
```json
{
  "OpenSky": {
    "BaseUrl": "https://opensky-network.org",
    "PollingIntervalSeconds": 200000
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "flights"
  }
}
```

--- FILE: FlightProducer\Configuration\KafkaOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}

```

--- FILE: FlightProducer\Configuration\OpenSkyOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class OpenSkyOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; }
    }
}

```

--- FILE: FlightProducer\Interfaces\IFlightProducer.cs (Branch Path) ---
```cs
﻿
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IFlightProducer
{
    Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken);


}

```

--- FILE: FlightProducer\Interfaces\IOpenSkyClient.cs (Branch Path) ---
```cs
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IOpenSkyClient
{
    Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken);
}
```

--- FILE: FlightProducer\Models\FlightState.cs (Branch Path) ---
```cs
﻿namespace FlightProducer.Models;

public class FlightState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string OriginCountry { get; set; } = string.Empty;

    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }

    public bool OnGround { get; set; }
}
```

--- FILE: FlightProducer\Services\FlightPollingService.cs (Branch Path) ---
```cs
﻿using System.Diagnostics;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly IFlightProducer _flightProducer;
    private readonly ILogger<FlightPollingService> _logger;
    private readonly OpenSkyOptions _options;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        IFlightProducer flightProducer,
        ILogger<FlightPollingService> logger,
        IOptions<OpenSkyOptions> options)
    {
        _openSkyClient = openSkyClient;
        _flightProducer = flightProducer;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var flights = await _openSkyClient.GetFlightsAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Received {FlightCount} flights from OpenSky",
                    flights.Count);



                const int batchSize = 500;
                var sentCount = 0;
                var stopwatch = Stopwatch.StartNew();
                var failedCount = 0;

                for (int i = 0; i < flights.Count; i += batchSize)
                {
                    var batch = flights
                        .Skip(i)
                        .Take(batchSize)
                        .Select(flight =>
                            TryProduceAsync(flight, stoppingToken))
                        .ToArray();

                    var results = await Task.WhenAll(batch);

                    sentCount += results.Count(success => success);
                    failedCount += results.Count(success => !success);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Kafka publishing completed. Sent: {SentCount}, Failed: {FailedCount}, Duration: {ElapsedSeconds:F2}s, Rate: {MessagesPerSecond:F0} messages/sec",
                    sentCount,
                    failedCount,
                    stopwatch.Elapsed.TotalSeconds,
                    stopwatch.Elapsed.TotalSeconds > 0
                        ? sentCount / stopwatch.Elapsed.TotalSeconds
                        : 0);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed during flight polling or Kafka publishing");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }


    private async Task<bool> TryProduceAsync(
    FlightState flight,
    CancellationToken cancellationToken)
    {
        try
        {
            await _flightProducer.ProduceAsync(
                flight,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish flight {Icao24}",
                flight.Icao24);

            return false;
        }
    }
}
```

--- FILE: FlightProducer\Services\KafkaFlightProducer.cs (Branch Path) ---
```cs
﻿using Confluent.Kafka;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightProducer.Services;

public class KafkaFlightProducer : IFlightProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFlightProducer> _logger;

    private const int MaxRetries = 2;
    private const int InitialRetryDelayMs = 1000;

    public KafkaFlightProducer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaFlightProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        var tries = 0;
        var delay = InitialRetryDelayMs;

        var message = new Message<string, string>
        {
            Key = flight.Icao24,
            Value = JsonSerializer.Serialize(flight)
        };

        while (tries <= MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _producer.ProduceAsync(
                    _options.Topic,
                    message,
                    cancellationToken);

                _logger.LogDebug(
                    "Delivered flight {Icao24} to {Topic}, partition {Partition}, offset {Offset}",
                    flight.Icao24,
                    result.Topic,
                    result.Partition,
                    result.Offset);

                return;
            }
            catch (ProduceException<string, string> ex)
            {
                if (tries == MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send flight {Icao24} to Kafka after {Attempts} attempts",
                        flight.Icao24,
                        tries + 1);

                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Failed to send flight {Icao24}. Attempt {Attempt} failed. Retrying in {DelayMs} ms",
                    flight.Icao24,
                    tries + 1,
                    delay);

                await Task.Delay(delay, cancellationToken);

                delay *= 2;
                tries++;
            }
        }

        throw new InvalidOperationException(
            $"Failed to publish flight {flight.Icao24} after {MaxRetries + 1} attempts.");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
```

--- FILE: FlightProducer\Services\OpenSkyClient.cs (Branch Path) ---
```cs
using System.Text.Json;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class OpenSkyClient : IOpenSkyClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenSkyOptions _options;

    public OpenSkyClient(
        HttpClient httpClient,
        IOptions<OpenSkyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/states/all";

        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        var root = document.RootElement;
        var flights = new List<FlightState>();

        if (!root.TryGetProperty("states", out var states) ||
            states.ValueKind != JsonValueKind.Array)
        {
            return flights;
        }

        foreach (var state in states.EnumerateArray())
        {
            if (state.ValueKind != JsonValueKind.Array ||
                state.GetArrayLength() < 10)
            {
                continue;
            }

            var icao24 = GetString(state, 0);

            if (string.IsNullOrWhiteSpace(icao24))
            {
                continue;
            }

            flights.Add(new FlightState
            {
                Icao24 = icao24,
                Callsign = GetString(state, 1)?.Trim(),
                OriginCountry = GetString(state, 2) ?? string.Empty,
                Longitude = GetDouble(state, 5),
                Latitude = GetDouble(state, 6),
                BaroAltitude = GetDouble(state, 7),
                OnGround = GetBoolean(state, 8),
                Velocity = GetDouble(state, 9)
            });
        }

        return flights;
    }

    private static string? GetString(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double? GetDouble(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.Number &&
               value.TryGetDouble(out var result)
            ? result
            : null;
    }

    private static bool GetBoolean(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.True;
    }
}

```

--- FILE: FlightTracking.Api\Program.cs (Branch Path) ---
```cs
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
```

--- FILE: FlightTracking.Api\appsettings.Development.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

```

--- FILE: FlightTracking.Api\appsettings.json (Branch Path) ---
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;"
  },
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "flight_tracking_db"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "flight-tracking-logs-{0:yyyy.MM.dd}",
          "autoRegisterTemplate": true
        }
      }
    ]
  }
}
```

--- FILE: FlightTracking.Api\Controllers\CustomerPrioritiesController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/priorities")]
    public class CustomerPrioritiesController : ControllerBase
    {
        private readonly ICustomerPriorityService _service;

        public CustomerPrioritiesController(ICustomerPriorityService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerPriorityResponseDto>>> GetUserPriorities(int userId)
        {
            var priorities = await _service.GetUserPrioritiesAsync(userId);
            return Ok(priorities);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerPriorityResponseDto>> CreatePriority(int userId, [FromBody] CreateCustomerPriorityDto dto)
        {
            if (userId != dto.UserId)
            {
                return BadRequest(new { message = "UserId in route does not match body." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _service.CreatePriorityAsync(dto);
            return CreatedAtAction(nameof(GetUserPriorities), new { userId = created.UserId }, created);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePriority(int userId, int id, [FromBody] UpdateCustomerPriorityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _service.UpdatePriorityAsync(id, dto);
            if (!updated)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent(); // מחזיר 204 הצלחה
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePriority(int id)
        {
            var deleted = await _service.DeletePriorityAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Priority area with ID {id} not found." });
            }

            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\Controllers\UsersController.cs (Branch Path) ---
```cs
﻿using Microsoft.AspNetCore.Mvc;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UzerId }, createdUser);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!updated)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return NoContent();
        }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\CoordinateDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // DTO לקבלת נקודה בודדת (קו אורך וקו רוחב)
    public class CoordinateDto
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    // DTO ליצירת אזור עניין חדש (או רדיוס או פוליגון)
    public class CreateCustomerPriorityDto
    {
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון (רשימת קודקודים)
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }

    // DTO להצגת הנתונים חזרה ללקוח
    public class CustomerPriorityResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty;
        public decimal? RadiusMeters { get; set; }
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
    public class UpdateCustomerPriorityDto
    {
        public int Priority { get; set; }
        public string AreaType { get; set; } = string.Empty; // "RADIUS" או "POLYGON"

        // רלוונטי רק לרדיוס
        public double? CenterLongitude { get; set; }
        public double? CenterLatitude { get; set; }
        public decimal? RadiusMeters { get; set; }

        // רלוונטי רק לפוליגון
        public List<CoordinateDto>? PolygonCoordinates { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\DTOs\UserDto.cs (Branch Path) ---
```cs
﻿namespace FlightTracking.Api.DTOs
{
    // נתונים שמתקבלים בעת יצירת/הרשמת משתמש
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }

    // נתונים שחוזרים ללקוח (ללא סיסמה מטעמי אבטחה)
    public class UserResponseDto
    {
        public int UzerId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
    }
    public class UpdateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fone { get; set; }
        public string? Password { get; set; } // אופציונלי - אם ירצה לשנות סיסמה
    }
}
```

--- FILE: FlightTracking.Api\Migrations\20261011115315_UpdateUserAndPriorities.Designer.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261011115315_UpdateUserAndPriorities")]
    partial class UpdateUserAndPriorities
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("fone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\20261011115315_UpdateUserAndPriorities.cs (Branch Path) ---
```cs
﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndPriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    uzer_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.uzer_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerPriorities",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AreaType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeometryData = table.Column<Geometry>(type: "geometry", nullable: true),
                    CenterPoint = table.Column<Point>(type: "point", nullable: true),
                    RadiusMeters = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPriorities", x => x.id);
                    table.ForeignKey(
                        name: "FK_CustomerPriorities_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "uzer_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPriorities_user_id",
                table: "CustomerPriorities",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPriorities");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

```

--- FILE: FlightTracking.Api\Migrations\AppDbContextModelSnapshot.cs (Branch Path) ---
```cs
﻿// <auto-generated />
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AreaType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("AreaType");

                    b.Property<Point>("CenterPoint")
                        .HasColumnType("POINT")
                        .HasColumnName("CenterPoint");

                    b.Property<Geometry>("GeometryData")
                        .HasColumnType("GEOMETRY")
                        .HasColumnName("GeometryData");

                    b.Property<int>("Priority")
                        .HasColumnType("int")
                        .HasColumnName("Priority");

                    b.Property<decimal?>("RadiusMeters")
                        .HasColumnType("decimal(65,30)")
                        .HasColumnName("RadiusMeters");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("CustomerPriorities");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Property<int>("UzerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("uzer_id");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UzerId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("email");

                    b.Property<string>("Fone")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("fone");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("user_name");

                    b.HasKey("UzerId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.CustomerPriority", b =>
                {
                    b.HasOne("FlightTracking.Api.Models.User", "User")
                        .WithMany("CustomerPriorities")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FlightTracking.Api.Models.User", b =>
                {
                    b.Navigation("CustomerPriorities");
                });
#pragma warning restore 612, 618
        }
    }
}

```

--- FILE: FlightTracking.Api\Models\AircraftState.cs (Branch Path) ---
```cs
﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlightTracking.Api.Models
{
    public class AircraftStateDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("icao24")]
        public string Icao24 { get; set; } = string.Empty;

        [BsonElement("callsign")]
        public string? Callsign { get; set; }

        [BsonElement("origin_country")]
        public string? OriginCountry { get; set; }

        [BsonElement("time_position")]
        public long? TimePosition { get; set; }

        [BsonElement("last_contact")]
        public long? LastContact { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("baro_altitude")]
        public double? BaroAltitude { get; set; }

        [BsonElement("on_ground")]
        public bool OnGround { get; set; }

        [BsonElement("velocity")]
        public double? Velocity { get; set; }

        [BsonElement("true_track")]
        public double? TrueTrack { get; set; }

        [BsonElement("vertical_rate")]
        public double? VerticalRate { get; set; }

        [BsonElement("geo_altitude")]
        public double? GeoAltitude { get; set; }

        [BsonElement("squawk")]
        public string? Squawk { get; set; }

        [BsonElement("spi")]
        public bool Spi { get; set; }

        [BsonElement("position_source")]
        public int PositionSource { get; set; }

        [BsonElement("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
```

--- FILE: FlightTracking.Api\Models\CustomerPriority.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Models
{
    [Table("CustomerPriorities")]
    public class CustomerPriority
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("AreaType")]
        public string AreaType { get; set; } = string.Empty; // "RADIUS" / "POLYGON"

        // Central geometric field (for polygon or converted circle)
        [Column("GeometryData", TypeName = "GEOMETRY")]
        public Geometry? GeometryData { get; set; }

        // Center point (only relevant for radius)
        [Column("CenterPoint", TypeName = "POINT")]
        public Point? CenterPoint { get; set; }

        [Column("RadiusMeters")]
        public decimal? RadiusMeters { get; set; }
    }
}
```

--- FILE: FlightTracking.Api\Models\User.cs (Branch Path) ---
```cs
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightTracking.Api.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("uzer_id")]
        public int UzerId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("user_name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Column("email")] 
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("fone")]
        public string? Fone { get; set; }
        public ICollection<CustomerPriority> CustomerPriorities { get; set; } = new List<CustomerPriority>();
    }
}
```

--- FILE: FlightTracking.Api\Properties\launchSettings.json (Branch Path) ---
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:44321",
      "sslPort": 44321
    }
  },
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

--- FILE: FlightTracking.Api\Repositories\CustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class CustomerPriorityRepository : ICustomerPriorityRepository
    {
        private readonly AppDbContext _context;

        public CustomerPriorityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustomerPriority>> GetAllAsync()
        {
            return await _context.CustomerPriorities.ToListAsync();
        }

        public async Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId)
        {
            return await _context.CustomerPriorities
                .Where(cp => cp.UserId == userId)
                .ToListAsync();
        }

        public async Task<CustomerPriority?> GetByIdAsync(int id)
        {
            return await _context.CustomerPriorities.FindAsync(id);
        }

        public async Task<CustomerPriority> AddAsync(CustomerPriority customerPriority)
        {
            await _context.CustomerPriorities.AddAsync(customerPriority);
            return customerPriority;
        }

        // <-- הוסף את מתודת העדכון לכאן
        public void Update(CustomerPriority customerPriority)
        {
            _context.CustomerPriorities.Update(customerPriority);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.CustomerPriorities.Remove(entity);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Repositories\ICustomerPriorityRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface ICustomerPriorityRepository
    {
        Task<IEnumerable<CustomerPriority>> GetAllAsync();
        Task<IEnumerable<CustomerPriority>> GetByUserIdAsync(int userId);
        Task<CustomerPriority?> GetByIdAsync(int id);
        Task<CustomerPriority> AddAsync(CustomerPriority customerPriority);
        void Update(CustomerPriority customerPriority); // <-- זה מה שצריך להוסיף כאן
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\IUserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
    }
}
```

--- FILE: FlightTracking.Api\Repositories\UserRepository.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\CustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Services
{
    public class CustomerPriorityService : ICustomerPriorityService
    {
        private readonly ICustomerPriorityRepository _repository;
        // יצירת Factory גיאומטרי עם SRID 4326 (מערכת קואורדינטות GPS עולמית)
        private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public CustomerPriorityService(ICustomerPriorityRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId)
        {
            var priorities = await _repository.GetByUserIdAsync(userId);
            return priorities.Select(MapToDto);
        }

        public async Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto)
        {
            var entity = new CustomerPriority
            {
                UserId = dto.UserId,
                Priority = dto.Priority,
                AreaType = dto.AreaType.ToUpper(),
                RadiusMeters = dto.RadiusMeters
            };

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                // יצירת נקודת מרכז (X = קו אורך, Y = קו רוחב)
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                // פוליגון חייב להיות סגור - אם הנקודה האחרונה שונה מהראשונה, נוסיף אותה לסוף
                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToDto(entity);
        }
        public async Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            entity.Priority = dto.Priority;
            entity.AreaType = dto.AreaType.ToUpper();
            entity.RadiusMeters = dto.RadiusMeters;

            // איפוס גיאומטריות ישנות כדי לבנות את החדשות נכון
            entity.CenterPoint = null;
            entity.GeometryData = null;

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            // ה-Repository כבר מנהל את ה-Update של ה-Entity, נשאר רק לשמור שינויים
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeletePriorityAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            await _repository.DeleteAsync(id);
            return await _repository.SaveChangesAsync();
        }

        // פונקציית עזר להמרת Entity ל-DTO חזרה
        private static CustomerPriorityResponseDto MapToDto(CustomerPriority entity)
        {
            var dto = new CustomerPriorityResponseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Priority = entity.Priority,
                AreaType = entity.AreaType,
                RadiusMeters = entity.RadiusMeters
            };

            if (entity.CenterPoint != null)
            {
                dto.CenterLongitude = entity.CenterPoint.X;
                dto.CenterLatitude = entity.CenterPoint.Y;
            }

            if (entity.GeometryData is Polygon polygon)
            {
                dto.PolygonCoordinates = polygon.Coordinates
                    .Select(c => new CoordinateDto { Longitude = c.X, Latitude = c.Y })
                    .ToList();
            }

            return dto;
        }
    }
}
```

--- FILE: FlightTracking.Api\Services\ICustomerPriorityService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface ICustomerPriorityService
    {
        Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId);
        Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto);
        Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto);
        Task<bool> DeletePriorityAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\IUserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;

namespace FlightTracking.Api.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);
    }
}
```

--- FILE: FlightTracking.Api\Services\UserService.cs (Branch Path) ---
```cs
﻿using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;

namespace FlightTracking.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            });
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return null;

            return new UserResponseDto
            {
                UzerId = u.UzerId,
                UserName = u.UserName,
                Email = u.Email,
                Fone = u.Fone
            };
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
        {
            var user = new User
            {
                UserName = dto.UserName,
                Password = dto.Password,// Note: In a real project it is recommended to hash the password
                Email = dto.Email,
                Fone = dto.Fone
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new UserResponseDto
            {
                UzerId = user.UzerId,
                UserName = user.UserName,
                Email = user.Email,
                Fone = user.Fone
            };
        }
        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.Fone = dto.Fone;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.Password = dto.Password;
            }

            await _userRepository.UpdateAsync(user);
            return await _userRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return await _userRepository.SaveChangesAsync();
        }
    }
}
```

--- FILE: FlightTracking.Api\Settings\AppDbContext.cs (Branch Path) ---
```cs
﻿using Microsoft.EntityFrameworkCore;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Settings
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```


==================================================
# BRANCH: remote/feature/flight-producer
# Commit Hash: 77978905c762d506e8fd40e1940ef3951e251bb2
==================================================


--- FILE: README.md (Branch Path) ---
```md
# HawkEye

HawkEye היא מערכת מתקדמת לניטור, מעקב והתראות על טיסות בזמן אמת, המבוססת על הגדרות גיאוגרפיות מותאמות אישית (נקודת ציון עם רדיוס או פוליגון מורכב).

המערכת מאפשרת למשתמשים להגדיר אזורי עניין על גבי מפה, לנטר טיסות החוצות אזורים אלו, ולקבל התראות מיידיות בערוצי תקשורת שונים.

---

```

--- FILE: docker-compose.yaml (Branch Path) ---
```yaml
services:
  mysql:
    image: mysql:9.7
    container_name: mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  mongodb:
    image: mongo:8
    container_name: mongodb
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD}
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db

  kafka:
    image: apache/kafka:4.1.0
    container_name: kafka
    restart: unless-stopped
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
    ports:
      - "9092:9092"
    volumes:
      - kafka_data:/var/lib/kafka/data

volumes:
  mysql_data:
  mongodb_data:
  kafka_data:
```

--- FILE: FlightProducer\Program.cs (Branch Path) ---
```cs
﻿using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OpenSkyOptions>(
    builder.Configuration.GetSection("OpenSky"));

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));


builder.Services.AddHttpClient<IOpenSkyClient, OpenSkyClient>();

builder.Services.AddHostedService<FlightPollingService>();

builder.Services.AddSingleton<IFlightProducer, KafkaFlightProducer>();

var host = builder.Build();

await host.RunAsync();
```

--- FILE: FlightProducer\appsettings.json (Branch Path) ---
```json
{
  "OpenSky": {
    "BaseUrl": "https://opensky-network.org",
    "PollingIntervalSeconds": 200000
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "flights"
  }
}
```

--- FILE: FlightProducer\Configuration\KafkaOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}

```

--- FILE: FlightProducer\Configuration\OpenSkyOptions.cs (Branch Path) ---
```cs
﻿

namespace FlightProducer.Configuration
{
    public class OpenSkyOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; }
    }
}

```

--- FILE: FlightProducer\Interfaces\IFlightProducer.cs (Branch Path) ---
```cs
﻿
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IFlightProducer
{
    Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken);


}

```

--- FILE: FlightProducer\Interfaces\IOpenSkyClient.cs (Branch Path) ---
```cs
using FlightProducer.Models;

namespace FlightProducer.Interfaces;

public interface IOpenSkyClient
{
    Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken);
}
```

--- FILE: FlightProducer\Models\FlightState.cs (Branch Path) ---
```cs
﻿namespace FlightProducer.Models;

public class FlightState
{
    public string Icao24 { get; set; } = string.Empty;
    public string? Callsign { get; set; }
    public string OriginCountry { get; set; } = string.Empty;

    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public double? BaroAltitude { get; set; }
    public double? Velocity { get; set; }

    public bool OnGround { get; set; }
}
```

--- FILE: FlightProducer\Services\FlightPollingService.cs (Branch Path) ---
```cs
﻿using System.Diagnostics;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class FlightPollingService : BackgroundService
{
    private readonly IOpenSkyClient _openSkyClient;
    private readonly IFlightProducer _flightProducer;
    private readonly ILogger<FlightPollingService> _logger;
    private readonly OpenSkyOptions _options;

    public FlightPollingService(
        IOpenSkyClient openSkyClient,
        IFlightProducer flightProducer,
        ILogger<FlightPollingService> logger,
        IOptions<OpenSkyOptions> options)
    {
        _openSkyClient = openSkyClient;
        _flightProducer = flightProducer;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var flights = await _openSkyClient.GetFlightsAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Received {FlightCount} flights from OpenSky",
                    flights.Count);



                const int batchSize = 500;
                var sentCount = 0;
                var stopwatch = Stopwatch.StartNew();
                var failedCount = 0;

                for (int i = 0; i < flights.Count; i += batchSize)
                {
                    var batch = flights
                        .Skip(i)
                        .Take(batchSize)
                        .Select(flight =>
                            TryProduceAsync(flight, stoppingToken))
                        .ToArray();

                    var results = await Task.WhenAll(batch);

                    sentCount += results.Count(success => success);
                    failedCount += results.Count(success => !success);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Kafka publishing completed. Sent: {SentCount}, Failed: {FailedCount}, Duration: {ElapsedSeconds:F2}s, Rate: {MessagesPerSecond:F0} messages/sec",
                    sentCount,
                    failedCount,
                    stopwatch.Elapsed.TotalSeconds,
                    stopwatch.Elapsed.TotalSeconds > 0
                        ? sentCount / stopwatch.Elapsed.TotalSeconds
                        : 0);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed during flight polling or Kafka publishing");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }


    private async Task<bool> TryProduceAsync(
    FlightState flight,
    CancellationToken cancellationToken)
    {
        try
        {
            await _flightProducer.ProduceAsync(
                flight,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish flight {Icao24}",
                flight.Icao24);

            return false;
        }
    }
}
```

--- FILE: FlightProducer\Services\KafkaFlightProducer.cs (Branch Path) ---
```cs
﻿using Confluent.Kafka;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlightProducer.Services;

public class KafkaFlightProducer : IFlightProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFlightProducer> _logger;

    private const int MaxRetries = 2;
    private const int InitialRetryDelayMs = 1000;

    public KafkaFlightProducer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaFlightProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        var tries = 0;
        var delay = InitialRetryDelayMs;

        var message = new Message<string, string>
        {
            Key = flight.Icao24,
            Value = JsonSerializer.Serialize(flight)
        };

        while (tries <= MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _producer.ProduceAsync(
                    _options.Topic,
                    message,
                    cancellationToken);

                _logger.LogDebug(
                    "Delivered flight {Icao24} to {Topic}, partition {Partition}, offset {Offset}",
                    flight.Icao24,
                    result.Topic,
                    result.Partition,
                    result.Offset);

                return;
            }
            catch (ProduceException<string, string> ex)
            {
                if (tries == MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send flight {Icao24} to Kafka after {Attempts} attempts",
                        flight.Icao24,
                        tries + 1);

                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Failed to send flight {Icao24}. Attempt {Attempt} failed. Retrying in {DelayMs} ms",
                    flight.Icao24,
                    tries + 1,
                    delay);

                await Task.Delay(delay, cancellationToken);

                delay *= 2;
                tries++;
            }
        }

        throw new InvalidOperationException(
            $"Failed to publish flight {flight.Icao24} after {MaxRetries + 1} attempts.");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
```

--- FILE: FlightProducer\Services\OpenSkyClient.cs (Branch Path) ---
```cs
using System.Text.Json;
using FlightProducer.Configuration;
using FlightProducer.Interfaces;
using FlightProducer.Models;
using Microsoft.Extensions.Options;

namespace FlightProducer.Services;

public class OpenSkyClient : IOpenSkyClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenSkyOptions _options;

    public OpenSkyClient(
        HttpClient httpClient,
        IOptions<OpenSkyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<FlightState>> GetFlightsAsync(
        CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/states/all";

        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        var root = document.RootElement;
        var flights = new List<FlightState>();

        if (!root.TryGetProperty("states", out var states) ||
            states.ValueKind != JsonValueKind.Array)
        {
            return flights;
        }

        foreach (var state in states.EnumerateArray())
        {
            if (state.ValueKind != JsonValueKind.Array ||
                state.GetArrayLength() < 10)
            {
                continue;
            }

            var icao24 = GetString(state, 0);

            if (string.IsNullOrWhiteSpace(icao24))
            {
                continue;
            }

            flights.Add(new FlightState
            {
                Icao24 = icao24,
                Callsign = GetString(state, 1)?.Trim(),
                OriginCountry = GetString(state, 2) ?? string.Empty,
                Longitude = GetDouble(state, 5),
                Latitude = GetDouble(state, 6),
                BaroAltitude = GetDouble(state, 7),
                OnGround = GetBoolean(state, 8),
                Velocity = GetDouble(state, 9)
            });
        }

        return flights;
    }

    private static string? GetString(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double? GetDouble(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.Number &&
               value.TryGetDouble(out var result)
            ? result
            : null;
    }

    private static bool GetBoolean(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.True;
    }
}

```


==================================================
# BRANCH: remote/main
# Commit Hash: 5eea1a9ae0476cd75007ef34e72e7afd1b3ded29
==================================================


--- FILE: README.md (Branch Path) ---
```md
Rar! ϐs  
       
```

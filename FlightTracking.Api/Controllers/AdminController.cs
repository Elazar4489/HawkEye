using System.Net.Sockets;
using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMongoDatabase _mongo;
        private readonly IConfiguration _configuration;

        public AdminController(AppDbContext context, IMongoDatabase mongo, IConfiguration configuration)
        {
            _context = context;
            _mongo = mongo;
            _configuration = configuration;
        }

        [HttpGet("health")]
        public async Task<ActionResult<SystemHealthDto>> Health()
        {
            var health = new SystemHealthDto();

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    health.MySqlStatus = "Online";
                    health.TotalUsers = await _context.Users.CountAsync();
                    health.TotalZones = await _context.CustomerPriorities.CountAsync();
                    health.ActiveSignalRConnections = await _context.UserConnections.CountAsync(c => c.IsConnected);
                }
            }
            catch
            {
                health.MySqlStatus = "Offline";
            }

            try
            {
                await _mongo.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                health.MongoStatus = "Online";

                var flights = _mongo.GetCollection<AircraftStateDocument>("flights");
                var latest = await flights
                    .Find(FilterDefinition<AircraftStateDocument>.Empty)
                    .SortByDescending(f => f.RecordedAt)
                    .Project(f => f.RecordedAt)
                    .FirstOrDefaultAsync();

                if (latest != default)
                {
                    health.LastIngestionUtc = DateTime.SpecifyKind(latest, DateTimeKind.Utc);
                    health.LastSnapshotFlights = (int)await flights.CountDocumentsAsync(f => f.RecordedAt == latest);
                }
            }
            catch
            {
                health.MongoStatus = "Offline";
            }

            health.KafkaStatus = await IsKafkaReachableAsync() ? "Online" : "Offline";

            return Ok(health);
        }

        private async Task<bool> IsKafkaReachableAsync()
        {
            var bootstrap = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
            var first = bootstrap.Split(',')[0];
            var parts = first.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port)) return false;

            try
            {
                using var client = new TcpClient();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await client.ConnectAsync(parts[0], port, timeout.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FlightsController : ControllerBase
    {
        private const string CollectionName = "flights";
        private readonly IMongoCollection<AircraftStateDocument> _flights;

        public FlightsController(IMongoDatabase database)
        {
            _flights = database.GetCollection<AircraftStateDocument>(CollectionName);
        }

        // Latest OpenSky snapshot stored by FlightProducer, optionally limited to the map view.
        [HttpGet("live")]
        public async Task<ActionResult<LiveFlightsResponseDto>> GetLive(
            [FromQuery] double? minLat,
            [FromQuery] double? maxLat,
            [FromQuery] double? minLon,
            [FromQuery] double? maxLon,
            [FromQuery] int limit = 3000)
        {
            limit = Math.Clamp(limit, 1, 10000);

            var latest = await _flights
                .Find(FilterDefinition<AircraftStateDocument>.Empty)
                .SortByDescending(f => f.RecordedAt)
                .Project(f => f.RecordedAt)
                .FirstOrDefaultAsync();

            if (latest == default)
            {
                return Ok(new LiveFlightsResponseDto());
            }

            var builder = Builders<AircraftStateDocument>.Filter;
            var snapshotFilter = builder.Eq(f => f.RecordedAt, latest)
                & builder.Ne(f => f.Latitude, null)
                & builder.Ne(f => f.Longitude, null);

            var filter = snapshotFilter;
            if (minLat.HasValue) filter &= builder.Gte(f => f.Latitude, minLat);
            if (maxLat.HasValue) filter &= builder.Lte(f => f.Latitude, maxLat);
            if (minLon.HasValue) filter &= builder.Gte(f => f.Longitude, minLon);
            if (maxLon.HasValue) filter &= builder.Lte(f => f.Longitude, maxLon);

            var total = await _flights.CountDocumentsAsync(snapshotFilter);
            var documents = await _flights.Find(filter).Limit(limit).ToListAsync();

            return Ok(new LiveFlightsResponseDto
            {
                RecordedAtUtc = DateTime.SpecifyKind(latest, DateTimeKind.Utc),
                TotalInSnapshot = (int)total,
                Flights = documents.Select(d => new LiveFlightDto
                {
                    Icao24 = d.Icao24,
                    Callsign = d.Callsign?.Trim() ?? string.Empty,
                    OriginCountry = d.OriginCountry ?? string.Empty,
                    Latitude = d.Latitude!.Value,
                    Longitude = d.Longitude!.Value,
                    BaroAltitude = d.BaroAltitude,
                    Velocity = d.Velocity,
                    TrueTrack = d.TrueTrack,
                    VerticalRate = d.VerticalRate,
                    OnGround = d.OnGround,
                    TimePosition = d.TimePosition,
                    LastContact = d.LastContact
                }).ToList()
            });
        }
    }
}

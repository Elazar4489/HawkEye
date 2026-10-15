using System.Data;
using System.Data.Common;
using System.Globalization;
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

    public async Task<List<ZoneMatch>> FindMatchingZonesAsync(
        FlightState flight,
        CancellationToken cancellationToken)
    {
        if (flight.Longitude is null || flight.Latitude is null)
            return new List<ZoneMatch>();

        // WKT is written lon-lat; the query tells MySQL so explicitly (SRID 4326
        // defaults to lat-long axis order). Invariant culture keeps the decimal point.
        var pointWkt = string.Create(
            CultureInfo.InvariantCulture,
            $"POINT({flight.Longitude.Value} {flight.Latitude.Value})");

        var connection = _dbContext.Database.GetDbConnection();

        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT cp.user_id, cp.id, cp.Name, cp.Priority
                FROM CustomerPriorities cp
                WHERE
                    (
                        cp.AreaType = 'RADIUS'
                        AND cp.CenterPoint IS NOT NULL
                        AND cp.RadiusMeters IS NOT NULL
                        AND ST_SRID(cp.CenterPoint) = 4326
                        AND ST_Distance(
                            cp.CenterPoint,
                            ST_GeomFromText(@flightPoint, 4326, 'axis-order=long-lat'),
                            'metre'
                        ) <= cp.RadiusMeters
                    )
                    OR
                    (
                        cp.AreaType = 'POLYGON'
                        AND cp.GeometryData IS NOT NULL
                        AND ST_SRID(cp.GeometryData) = 4326
                        AND ST_Contains(
                            cp.GeometryData,
                            ST_GeomFromText(@flightPoint, 4326, 'axis-order=long-lat')
                        )
                    )
                ORDER BY cp.user_id, cp.Priority, cp.id;
                """;

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@flightPoint";
            parameter.Value = pointWkt;
            parameter.DbType = DbType.String;

            command.Parameters.Add(parameter);

            var matches = new List<ZoneMatch>();

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                matches.Add(new ZoneMatch(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetInt32(3)));
            }

            return matches;
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
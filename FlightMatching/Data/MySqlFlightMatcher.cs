using System.Data;
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
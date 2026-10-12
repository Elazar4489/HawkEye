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

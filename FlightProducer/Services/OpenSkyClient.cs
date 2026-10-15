using System.Net;
using System.Net.Http.Headers;
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

    private string? _accessToken;
    private DateTime _accessTokenExpiresAtUtc;

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

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is not null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException(
                "OpenSky rate limit reached (HTTP 429). Increase OpenSky__PollingIntervalSeconds " +
                "or configure OpenSky__ClientId/ClientSecret.");
        }

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
                TimePosition = GetLong(state, 3),
                LastContact = GetLong(state, 4),
                Longitude = GetDouble(state, 5),
                Latitude = GetDouble(state, 6),
                BaroAltitude = GetDouble(state, 7),
                OnGround = GetBoolean(state, 8),
                Velocity = GetDouble(state, 9),
                TrueTrack = state.GetArrayLength() > 10 ? GetDouble(state, 10) : null,
                VerticalRate = state.GetArrayLength() > 11 ? GetDouble(state, 11) : null
            });
        }

        return flights;
    }

    private async Task<string?> GetAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            return null;
        }

        if (_accessToken is not null &&
            DateTime.UtcNow < _accessTokenExpiresAtUtc)
        {
            return _accessToken;
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        });

        using var response = await _httpClient.PostAsync(
            _options.TokenUrl,
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        _accessToken = document.RootElement.GetProperty("access_token").GetString();

        var expiresIn = document.RootElement.TryGetProperty("expires_in", out var expires)
            ? expires.GetInt32()
            : 300;

        // Refresh a minute early so a poll never uses an expired token.
        _accessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(expiresIn - 60, 30));

        return _accessToken;
    }

    private static long? GetLong(JsonElement array, int index)
    {
        var value = array[index];

        return value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt64(out var result)
            ? result
            : null;
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

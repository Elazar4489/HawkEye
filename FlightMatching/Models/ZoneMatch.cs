namespace FlightMatching.Models;

public sealed record ZoneMatch(int UserId, int ZoneId, string? ZoneName, int Priority);

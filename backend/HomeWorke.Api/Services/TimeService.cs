using System.Text.Json;

namespace HomeWorke.Api.Services;

public class TimeService : ITimeService
{
    private readonly ILogger<TimeService> _logger;

    // Cache for a short duration to avoid excessive API calls
    private DateTime? _cachedTime;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    // Multiple time API sources in priority order (tried until one succeeds)
    private static readonly TimeApiSource[] ApiSources =
    [
        new()
        {
            Name = "timeapi.io",
            Url = "https://timeapi.io/api/time/current/zone?timeZone=Europe/Zurich",
            Parser = ParseTimeApiIo
        },
        new()
        {
            Name = "WorldTimeAPI (HTTP)",
            Url = "http://worldtimeapi.org/api/timezone/Europe/Zurich",
            Parser = ParseWorldTimeApi
        },
        new()
        {
            Name = "WorldTimeAPI (HTTPS)",
            Url = "https://worldtimeapi.org/api/timezone/Europe/Zurich",
            Parser = ParseWorldTimeApi
        }
    ];

    public TimeService(ILogger<TimeService> logger)
    {
        _logger = logger;
    }

    public async Task<DateTime> GetZurichTimeAsync()
    {
        // Return cached time if still valid
        if (_cachedTime.HasValue && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedTime.Value;
        }

        // Try each API source in order until one succeeds
        var errors = new List<string>();
        foreach (var source in ApiSources)
        {
            try
            {
                var result = await TryFetchTimeAsync(source);
                _cachedTime = result;
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                _logger.LogDebug("Zurich time fetched from {Source}: {Time}", source.Name, result);
                return result;
            }
            catch (Exception ex)
            {
                var msg = $"{source.Name}: {ex.Message}";
                errors.Add(msg);
                _logger.LogWarning("Time API {Source} failed: {Error}", source.Name, ex.Message);
            }
        }

        // All sources failed
        var allErrors = string.Join("; ", errors);
        _logger.LogError("All {Count} time API sources failed: {Errors}", ApiSources.Length, allErrors);
        throw new TimeServiceException(
            $"Unable to retrieve Zurich time from any external API. Tried: {allErrors}");
    }

    public async Task<DateTime> GetZurichDateAsync()
    {
        var time = await GetZurichTimeAsync();
        return time.Date;
    }

    /// <summary>
    /// Try a single API source with a fresh HttpClient (not the Polly-wrapped one
    /// that may have a tripped circuit breaker from the DI container).
    /// </summary>
    private static async Task<DateTime> TryFetchTimeAsync(TimeApiSource source)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "HomeWorke/1.0");

        var response = await client.GetAsync(source.Url, cts.Token);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cts.Token);
        var result = source.Parser(content);

        if (result == null)
            throw new InvalidOperationException($"Failed to parse response from {source.Name}.");

        return result.Value;
    }

    // ── Parsers for each API format ──────────────────────────

    private static DateTime? ParseTimeApiIo(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<TimeApiIoResponse>(json, options);
        if (data == null) return null;

        return new DateTime(
            data.Year, data.Month, data.Day,
            data.Hour, data.Minute, data.Seconds,
            data.MilliSeconds, DateTimeKind.Unspecified);
    }

    private static DateTime? ParseWorldTimeApi(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<WorldTimeApiResponse>(json, options);
        return data?.Datetime;
    }

    // ── Nested type ──────────────────────────────────────────

    private class TimeApiSource
    {
        public string Name { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public Func<string, DateTime?> Parser { get; init; } = _ => null;
    }
}

/// <summary>
/// Custom exception for time service failures.
/// </summary>
public class TimeServiceException : Exception
{
    public TimeServiceException(string message, Exception? inner = null)
        : base(message, inner) { }
}

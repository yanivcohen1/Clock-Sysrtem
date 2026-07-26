namespace HomeWorke.Api.Services;

/// <summary>
/// Response from WorldTimeAPI.org for a timezone query.
/// </summary>
public class WorldTimeApiResponse
{
    public string Timezone { get; set; } = string.Empty;
    public DateTime Datetime { get; set; }
    public string UtcOffset { get; set; } = string.Empty;
}

/// <summary>
/// Response from timeapi.io.
/// </summary>
public class TimeApiIoResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Seconds { get; set; }
    public int MilliSeconds { get; set; }
    public string DateTime { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
}

public interface ITimeService
{
    /// <summary>
    /// Gets the current date and time in Europe/Zurich from an external API.
    /// Never uses local server or browser time.
    /// </summary>
    Task<DateTime> GetZurichTimeAsync();

    /// <summary>
    /// Gets just the Zurich date (for shift-date determination).
    /// </summary>
    Task<DateTime> GetZurichDateAsync();
}

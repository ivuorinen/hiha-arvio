using System.Globalization;

namespace HihaArvio.Converters;

/// <summary>
/// Converts a DateTimeOffset to a relative timestamp string (e.g., "just now", "5 minutes ago").
/// Per spec §3.4.2: History page should display relative timestamps.
/// </summary>
public class RelativeTimestampConverter : IValueConverter
{
    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> to a human-readable relative timestamp.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset timestamp)
        {
            return string.Empty;
        }

        var elapsed = DateTimeOffset.UtcNow - timestamp;

        return elapsed.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => $"{(int)elapsed.TotalMinutes} minute{((int)elapsed.TotalMinutes == 1 ? "" : "s")} ago",
            < 86400 => $"{(int)elapsed.TotalHours} hour{((int)elapsed.TotalHours == 1 ? "" : "s")} ago",
            < 172800 => "yesterday",
            _ => $"{(int)elapsed.TotalDays} days ago"
        };
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

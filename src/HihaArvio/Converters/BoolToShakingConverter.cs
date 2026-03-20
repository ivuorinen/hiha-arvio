using System.Globalization;

namespace HihaArvio.Converters;

/// <summary>
/// Converts a boolean shaking state to a human-readable status string ("Shaking" or "Idle").
/// </summary>
public class BoolToShakingConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to a status string. Returns "Shaking" if <c>true</c>, "Idle" if <c>false</c>.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isShaking)
        {
            return isShaking ? "Shaking" : "Idle";
        }
        return "Unknown";
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

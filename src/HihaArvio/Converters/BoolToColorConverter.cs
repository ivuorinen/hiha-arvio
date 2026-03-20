using System.Globalization;

namespace HihaArvio.Converters;

/// <summary>
/// Converts a boolean shaking state to a corresponding UI color (green when shaking, gray when idle).
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to a <see cref="Color"/>. Returns green if <c>true</c>, gray otherwise.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isShaking)
        {
            return isShaking ? Colors.Green : Colors.Gray;
        }
        return Colors.Gray;
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

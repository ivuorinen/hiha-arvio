using System.Globalization;

namespace HihaArvio.Converters;

/// <summary>
/// Inverts a boolean value for use in XAML data bindings.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    /// <summary>
    /// Returns the logical negation of the input boolean value.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    /// <summary>
    /// Returns the logical negation of the input boolean value (reverse conversion).
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}

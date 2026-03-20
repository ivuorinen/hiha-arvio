using System.Globalization;

namespace HihaArvio.Converters;

/// <summary>
/// Returns <c>true</c> when the bound value is <c>null</c>, for conditional visibility in XAML.
/// </summary>
public class IsNullConverter : IValueConverter
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="value"/> is <c>null</c>.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null;
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns <c>true</c> when the bound value is not <c>null</c>, for conditional visibility in XAML.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="value"/> is not <c>null</c>.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

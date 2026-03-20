using System.Globalization;
using HihaArvio.Models;

namespace HihaArvio.Converters;

/// <summary>
/// Converts an EstimateMode to a color for the mode badge.
/// Per spec §3.4.2: Mode badge should have color coding per mode.
/// </summary>
public class ModeToColorConverter : IValueConverter
{
    /// <summary>
    /// Converts an <see cref="EstimateMode"/> to a badge <see cref="Color"/>.
    /// Work = blue, Generic = teal, Humorous = purple.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EstimateMode mode)
        {
            return Colors.Gray;
        }

        return mode switch
        {
            EstimateMode.Work => Color.FromArgb("#4A90D9"),
            EstimateMode.Generic => Color.FromArgb("#2AA198"),
            EstimateMode.Humorous => Color.FromArgb("#9B59B6"),
            _ => Colors.Gray
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

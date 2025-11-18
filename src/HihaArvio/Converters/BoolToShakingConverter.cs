using System.Globalization;

namespace HihaArvio.Converters;

public class BoolToShakingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isShaking)
        {
            return isShaking ? "Shaking" : "Idle";
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

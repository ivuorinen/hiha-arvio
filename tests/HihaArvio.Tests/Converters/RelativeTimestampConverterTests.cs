using System.Globalization;
using FluentAssertions;
using HihaArvio.Converters;

namespace HihaArvio.Tests.Converters;

/// <summary>
/// Tests for the RelativeTimestampConverter covering future timestamps, just now, minutes, hours, yesterday, and days ago.
/// </summary>
public class RelativeTimestampConverterTests
{
    private readonly RelativeTimestampConverter _converter = new();

    [Fact]
    public void Convert_FutureTimestamp_ShouldReturnInTheFuture()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        var result = _converter.Convert(futureTime, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("in the future");
    }

    [Fact]
    public void Convert_JustNow_ShouldReturnJustNow()
    {
        // Arrange
        var recentTime = DateTimeOffset.UtcNow.AddSeconds(-30);

        // Act
        var result = _converter.Convert(recentTime, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("just now");
    }

    [Fact]
    public void Convert_MinutesAgo_ShouldReturnMinutesAgo()
    {
        // Arrange
        var minutesAgo = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Act
        var result = _converter.Convert(minutesAgo, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        ((string)result!).Should().Contain("minute").And.Contain("ago");
    }

    [Fact]
    public void Convert_OneMinuteAgo_ShouldUseSingularForm()
    {
        // Arrange
        var oneMinuteAgo = DateTimeOffset.UtcNow.AddSeconds(-90);

        // Act
        var result = _converter.Convert(oneMinuteAgo, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("1 minute ago");
    }

    [Fact]
    public void Convert_HoursAgo_ShouldReturnHoursAgo()
    {
        // Arrange
        var hoursAgo = DateTimeOffset.UtcNow.AddHours(-3);

        // Act
        var result = _converter.Convert(hoursAgo, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        ((string)result!).Should().Contain("hour").And.Contain("ago");
    }

    [Fact]
    public void Convert_OneHourAgo_ShouldUseSingularForm()
    {
        // Arrange
        var oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1).AddMinutes(-10);

        // Act
        var result = _converter.Convert(oneHourAgo, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("1 hour ago");
    }

    [Fact]
    public void Convert_Yesterday_ShouldReturnYesterday()
    {
        // Arrange
        var yesterday = DateTimeOffset.UtcNow.AddHours(-30);

        // Act
        var result = _converter.Convert(yesterday, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("yesterday");
    }

    [Fact]
    public void Convert_DaysAgo_ShouldReturnDaysAgo()
    {
        // Arrange
        var daysAgo = DateTimeOffset.UtcNow.AddDays(-5);

        // Act
        var result = _converter.Convert(daysAgo, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        ((string)result!).Should().Contain("days ago");
    }

    [Fact]
    public void Convert_NonDateTimeOffset_ShouldReturnEmptyString()
    {
        // Act
        var result = _converter.Convert("not a date", typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_Null_ShouldReturnEmptyString()
    {
        // Act
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotImplementedException()
    {
        // Act & Assert
        var act = () => _converter.ConvertBack("test", typeof(DateTimeOffset), null, CultureInfo.InvariantCulture);
        act.Should().Throw<NotImplementedException>();
    }
}

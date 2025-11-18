using FluentAssertions;
using HihaArvio.Models;

namespace HihaArvio.Tests.Models;

public class EstimateModeTests
{
    [Fact]
    public void EstimateMode_ShouldHaveWorkValue()
    {
        // Act
        var mode = EstimateMode.Work;

        // Assert
        mode.Should().Be(EstimateMode.Work);
        ((int)mode).Should().Be(0);
    }

    [Fact]
    public void EstimateMode_ShouldHaveGenericValue()
    {
        // Act
        var mode = EstimateMode.Generic;

        // Assert
        mode.Should().Be(EstimateMode.Generic);
        ((int)mode).Should().Be(1);
    }

    [Fact]
    public void EstimateMode_ShouldHaveHumorousValue()
    {
        // Act
        var mode = EstimateMode.Humorous;

        // Assert
        mode.Should().Be(EstimateMode.Humorous);
        ((int)mode).Should().Be(2);
    }

    [Fact]
    public void EstimateMode_ShouldHaveExactlyThreeValues()
    {
        // Act
        var values = Enum.GetValues<EstimateMode>();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain(new[] { EstimateMode.Work, EstimateMode.Generic, EstimateMode.Humorous });
    }
}

using FluentAssertions;
using HihaArvio.Models;

namespace HihaArvio.Tests.Models;

/// <summary>
/// Tests for the <see cref="EstimateMode"/> enum, verifying its members and their integer values.
/// </summary>
public class EstimateModeTests
{
    /// <summary>
    /// Verifies that the Work enum member exists and has integer value 0.
    /// </summary>
    [Fact]
    public void EstimateMode_ShouldHaveWorkValue()
    {
        // Act
        var mode = EstimateMode.Work;

        // Assert
        mode.Should().Be(EstimateMode.Work);
        ((int)mode).Should().Be(0);
    }

    /// <summary>
    /// Verifies that the Generic enum member exists and has integer value 1.
    /// </summary>
    [Fact]
    public void EstimateMode_ShouldHaveGenericValue()
    {
        // Act
        var mode = EstimateMode.Generic;

        // Assert
        mode.Should().Be(EstimateMode.Generic);
        ((int)mode).Should().Be(1);
    }

    /// <summary>
    /// Verifies that the Humorous enum member exists and has integer value 2.
    /// </summary>
    [Fact]
    public void EstimateMode_ShouldHaveHumorousValue()
    {
        // Act
        var mode = EstimateMode.Humorous;

        // Assert
        mode.Should().Be(EstimateMode.Humorous);
        ((int)mode).Should().Be(2);
    }

    /// <summary>
    /// Verifies that the EstimateMode enum contains exactly three members: Work, Generic, and Humorous.
    /// </summary>
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

using System.Collections.ObjectModel;
using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using HihaArvio.ViewModels;
using NSubstitute;

namespace HihaArvio.Tests.ViewModels;

public class HistoryViewModelTests
{
    private readonly IStorageService _storageService;
    private readonly HistoryViewModel _viewModel;

    public HistoryViewModelTests()
    {
        _storageService = Substitute.For<IStorageService>();
        _viewModel = new HistoryViewModel(_storageService);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyHistory()
    {
        // Assert
        _viewModel.History.Should().NotBeNull();
        _viewModel.History.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeIsEmptyAsTrue()
    {
        // Assert
        _viewModel.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldHaveLoadHistoryCommand()
    {
        // Assert
        _viewModel.LoadHistoryCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldHaveClearHistoryCommand()
    {
        // Assert
        _viewModel.ClearHistoryCommand.Should().NotBeNull();
    }

    #endregion

    #region Load History Tests

    [Fact]
    public async Task LoadHistoryCommand_ShouldLoadHistoryFromStorage()
    {
        // Arrange
        var estimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5)),
            EstimateResult.Create("1 day", EstimateMode.Work, 0.3, TimeSpan.FromSeconds(3))
        };
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(estimates));

        // Act
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        // Assert
        await _storageService.Received(1).GetHistoryAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task LoadHistoryCommand_ShouldPopulateHistoryCollection()
    {
        // Arrange
        var estimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5)),
            EstimateResult.Create("1 day", EstimateMode.Work, 0.3, TimeSpan.FromSeconds(3)),
            EstimateResult.Create("3 months", EstimateMode.Work, 0.8, TimeSpan.FromSeconds(8))
        };
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(estimates));

        // Act
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.History.Should().HaveCount(3);
        _viewModel.History[0].EstimateText.Should().Be("2 weeks");
        _viewModel.History[1].EstimateText.Should().Be("1 day");
        _viewModel.History[2].EstimateText.Should().Be("3 months");
    }

    [Fact]
    public async Task LoadHistoryCommand_WhenHistoryEmpty_ShouldSetIsEmptyToTrue()
    {
        // Arrange
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(new List<EstimateResult>()));

        // Act
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadHistoryCommand_WhenHistoryHasItems_ShouldSetIsEmptyToFalse()
    {
        // Arrange
        var estimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5))
        };
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(estimates));

        // Act
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadHistoryCommand_CalledMultipleTimes_ShouldReplaceHistory()
    {
        // Arrange
        var firstEstimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5))
        };
        var secondEstimates = new List<EstimateResult>
        {
            EstimateResult.Create("1 day", EstimateMode.Work, 0.3, TimeSpan.FromSeconds(3)),
            EstimateResult.Create("3 months", EstimateMode.Work, 0.8, TimeSpan.FromSeconds(8))
        };

        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(
            Task.FromResult(firstEstimates),
            Task.FromResult(secondEstimates));

        // Act
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.History.Should().HaveCount(2);
        _viewModel.History[0].EstimateText.Should().Be("1 day");
    }

    #endregion

    #region Clear History Tests

    [Fact]
    public async Task ClearHistoryCommand_ShouldCallStorageServiceClear()
    {
        // Act
        await _viewModel.ClearHistoryCommand.ExecuteAsync(null);

        // Assert
        await _storageService.Received(1).ClearHistoryAsync();
    }

    [Fact]
    public async Task ClearHistoryCommand_ShouldClearHistoryCollection()
    {
        // Arrange - Load some history first
        var estimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5)),
            EstimateResult.Create("1 day", EstimateMode.Work, 0.3, TimeSpan.FromSeconds(3))
        };
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(estimates));
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
        _viewModel.History.Should().HaveCount(2);

        // Act
        await _viewModel.ClearHistoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.History.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearHistoryCommand_ShouldSetIsEmptyToTrue()
    {
        // Arrange - Load some history first
        var estimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5))
        };
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(estimates));
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
        _viewModel.IsEmpty.Should().BeFalse();

        // Act
        await _viewModel.ClearHistoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsEmpty.Should().BeTrue();
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public async Task History_WhenLoadHistoryCalled_ShouldPopulateCollection()
    {
        // Arrange
        var collectionChangedCount = 0;
        _viewModel.History.CollectionChanged += (sender, args) => collectionChangedCount++;

        var estimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5))
        };
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(estimates));

        // Act
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);

        // Assert
        collectionChangedCount.Should().BeGreaterThan(0);
        _viewModel.History.Should().HaveCount(1);
    }

    [Fact]
    public async Task IsEmpty_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var propertyChangedCount = 0;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(HistoryViewModel.IsEmpty))
                propertyChangedCount++;
        };

        var estimates = new List<EstimateResult>
        {
            EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5))
        };
        _storageService.GetHistoryAsync(Arg.Any<int>()).Returns(Task.FromResult(estimates));

        // Act
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null); // IsEmpty changes from true to false
        await _viewModel.ClearHistoryCommand.ExecuteAsync(null); // IsEmpty changes from false to true

        // Assert
        propertyChangedCount.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Observable Collection Tests

    [Fact]
    public void History_ShouldBeObservableCollection()
    {
        // Assert
        _viewModel.History.Should().BeOfType<ObservableCollection<EstimateResult>>();
    }

    #endregion
}

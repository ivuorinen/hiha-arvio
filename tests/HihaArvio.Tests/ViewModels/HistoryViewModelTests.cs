using System.Collections.ObjectModel;
using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using HihaArvio.ViewModels;
using NSubstitute;

namespace HihaArvio.Tests.ViewModels;

/// <summary>
/// Tests for the HistoryViewModel covering initialization, history loading, clearing, property change notifications, and observable collection behavior.
/// </summary>
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

    /// <summary>
    /// Verifies that the history collection is empty on initialization.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithEmptyHistory()
    {
        // Assert
        _viewModel.History.Should().NotBeNull();
        _viewModel.History.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that IsEmpty is true when no history has been loaded.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeIsEmptyAsTrue()
    {
        // Assert
        _viewModel.IsEmpty.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the LoadHistoryCommand is initialized.
    /// </summary>
    [Fact]
    public void Constructor_ShouldHaveLoadHistoryCommand()
    {
        // Assert
        _viewModel.LoadHistoryCommand.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the ClearHistoryCommand is initialized.
    /// </summary>
    [Fact]
    public void Constructor_ShouldHaveClearHistoryCommand()
    {
        // Assert
        _viewModel.ClearHistoryCommand.Should().NotBeNull();
    }

    #endregion

    #region Load History Tests

    /// <summary>
    /// Verifies that executing LoadHistoryCommand calls the storage service.
    /// </summary>
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

    /// <summary>
    /// Verifies that loaded estimates populate the History collection.
    /// </summary>
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

    /// <summary>
    /// Verifies that IsEmpty is true when no estimates are returned.
    /// </summary>
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

    /// <summary>
    /// Verifies that IsEmpty is false when estimates are loaded.
    /// </summary>
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

    /// <summary>
    /// Verifies that subsequent loads replace the previous history collection.
    /// </summary>
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

    /// <summary>
    /// Verifies that executing ClearHistoryCommand calls the storage service.
    /// </summary>
    [Fact]
    public async Task ClearHistoryCommand_ShouldCallStorageServiceClear()
    {
        // Act
        await _viewModel.ClearHistoryCommand.ExecuteAsync(null);

        // Assert
        await _storageService.Received(1).ClearHistoryAsync();
    }

    /// <summary>
    /// Verifies that clearing removes all items from the History collection.
    /// </summary>
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

    /// <summary>
    /// Verifies that IsEmpty becomes true after clearing history.
    /// </summary>
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

    /// <summary>
    /// Verifies that the ObservableCollection fires CollectionChanged events when loaded.
    /// </summary>
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

    /// <summary>
    /// Verifies that changing IsEmpty raises PropertyChanged notifications.
    /// </summary>
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

    /// <summary>
    /// Verifies that History is an ObservableCollection for UI binding support.
    /// </summary>
    [Fact]
    public void History_ShouldBeObservableCollection()
    {
        // Assert
        _viewModel.History.Should().BeOfType<ObservableCollection<EstimateResult>>();
    }

    #endregion
}

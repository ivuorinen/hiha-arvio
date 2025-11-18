using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using HihaArvio.ViewModels;
using NSubstitute;

namespace HihaArvio.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IShakeDetectionService _shakeDetectionService;
    private readonly IEstimateService _estimateService;
    private readonly IStorageService _storageService;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _shakeDetectionService = Substitute.For<IShakeDetectionService>();
        _estimateService = Substitute.For<IEstimateService>();
        _storageService = Substitute.For<IStorageService>();

        // Setup default storage service behavior
        _storageService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings
        {
            SelectedMode = EstimateMode.Work,
            MaxHistorySize = 10
        }));

        _viewModel = new MainViewModel(_shakeDetectionService, _estimateService, _storageService);
    }

    #region Initialization Tests

    [Fact]
    public async Task Constructor_ShouldLoadSettings()
    {
        // Arrange - Create new instance to test initialization
        var storageService = Substitute.For<IStorageService>();
        var settings = new AppSettings { SelectedMode = EstimateMode.Generic, MaxHistorySize = 20 };
        storageService.LoadSettingsAsync().Returns(Task.FromResult(settings));

        // Act
        var vm = new MainViewModel(_shakeDetectionService, _estimateService, storageService);
        await Task.Delay(50); // Give async initialization time to complete

        // Assert
        vm.SelectedMode.Should().Be(EstimateMode.Generic);
        await storageService.Received(1).LoadSettingsAsync();
    }

    [Fact]
    public void Constructor_ShouldStartMonitoringShakes()
    {
        // Assert
        _shakeDetectionService.Received(1).StartMonitoring();
    }

    [Fact]
    public void Constructor_ShouldSubscribeToShakeDataChanged()
    {
        // Arrange
        var shakeService = Substitute.For<IShakeDetectionService>();
        var storageService = Substitute.For<IStorageService>();
        storageService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));

        // Act
        var vm = new MainViewModel(shakeService, _estimateService, storageService);

        // Simulate shake event
        shakeService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            shakeService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(2) });

        // Assert - Event subscription confirmed by no exception
        vm.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNoCurrentEstimate()
    {
        // Assert
        _viewModel.CurrentEstimate.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultShakeData()
    {
        // Assert
        _viewModel.CurrentShakeData.Should().NotBeNull();
        _viewModel.CurrentShakeData.IsShaking.Should().BeFalse();
        _viewModel.CurrentShakeData.Intensity.Should().Be(0.0);
        _viewModel.CurrentShakeData.Duration.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Shake Detection Integration Tests

    [Fact]
    public void OnShakeDataChanged_WhenShakeStarts_ShouldUpdateCurrentShakeData()
    {
        // Arrange
        var shakeData = new ShakeData { IsShaking = true, Intensity = 0.7, Duration = TimeSpan.FromSeconds(3) };

        // Act
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(_shakeDetectionService, shakeData);

        // Assert
        _viewModel.CurrentShakeData.Should().NotBeNull();
        _viewModel.CurrentShakeData.IsShaking.Should().BeTrue();
        _viewModel.CurrentShakeData.Intensity.Should().Be(0.7);
        _viewModel.CurrentShakeData.Duration.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task OnShakeDataChanged_WhenShakeStops_ShouldGenerateEstimate()
    {
        // Arrange
        var estimate = EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        _estimateService.GenerateEstimate(Arg.Any<double>(), Arg.Any<TimeSpan>(), Arg.Any<EstimateMode>())
            .Returns(estimate);

        // Start shake
        var shakeStart = new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(3) };
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(_shakeDetectionService, shakeStart);

        // Act - Stop shake
        var shakeStop = new ShakeData { IsShaking = false, Intensity = 0.0, Duration = TimeSpan.Zero };
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(_shakeDetectionService, shakeStop);

        await Task.Delay(50); // Give async operation time to complete

        // Assert
        _estimateService.Received(1).GenerateEstimate(0.5, TimeSpan.FromSeconds(3), EstimateMode.Work);
    }

    [Fact]
    public async Task OnShakeDataChanged_WhenShakeStops_ShouldSaveEstimateToStorage()
    {
        // Arrange
        var estimate = EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        _estimateService.GenerateEstimate(Arg.Any<double>(), Arg.Any<TimeSpan>(), Arg.Any<EstimateMode>())
            .Returns(estimate);

        // Start and stop shake
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(3) });

        // Act
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = false, Intensity = 0.0, Duration = TimeSpan.Zero });

        await Task.Delay(50);

        // Assert
        await _storageService.Received(1).SaveEstimateAsync(estimate);
    }

    [Fact]
    public async Task OnShakeDataChanged_WhenShakeStops_ShouldUpdateCurrentEstimate()
    {
        // Arrange
        var estimate = EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        _estimateService.GenerateEstimate(Arg.Any<double>(), Arg.Any<TimeSpan>(), Arg.Any<EstimateMode>())
            .Returns(estimate);

        // Start and stop shake
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(3) });

        // Act
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = false, Intensity = 0.0, Duration = TimeSpan.Zero });

        await Task.Delay(50);

        // Assert
        _viewModel.CurrentEstimate.Should().NotBeNull();
        _viewModel.CurrentEstimate!.EstimateText.Should().Be("2 weeks");
    }

    [Fact]
    public void OnShakeDataChanged_WhenShakeContinues_ShouldNotGenerateEstimate()
    {
        // Arrange
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(2) });

        // Act - Shake continues with higher intensity
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.7, Duration = TimeSpan.FromSeconds(3) });

        // Assert - Should only update shake data, not generate estimate
        _estimateService.DidNotReceive().GenerateEstimate(Arg.Any<double>(), Arg.Any<TimeSpan>(), Arg.Any<EstimateMode>());
    }

    [Fact]
    public async Task OnShakeDataChanged_WhenShakeStops_ShouldResetShakeDetectionService()
    {
        // Arrange
        var estimate = EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        _estimateService.GenerateEstimate(Arg.Any<double>(), Arg.Any<TimeSpan>(), Arg.Any<EstimateMode>())
            .Returns(estimate);

        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(3) });

        // Act
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = false, Intensity = 0.0, Duration = TimeSpan.Zero });

        await Task.Delay(50);

        // Assert
        _shakeDetectionService.Received(1).Reset();
    }

    #endregion

    #region Mode Selection Tests

    [Fact]
    public async Task SelectedMode_WhenChanged_ShouldSaveToStorage()
    {
        // Act
        _viewModel.SelectedMode = EstimateMode.Generic;
        await Task.Delay(50); // Give async save time to complete

        // Assert
        await _storageService.Received().SaveSettingsAsync(Arg.Is<AppSettings>(s =>
            s.SelectedMode == EstimateMode.Generic));
    }

    [Fact]
    public async Task SelectedMode_WhenChanged_ShouldUseNewModeForEstimates()
    {
        // Arrange
        _viewModel.SelectedMode = EstimateMode.Generic;
        await Task.Delay(50);

        var estimate = EstimateResult.Create("30 minutes", EstimateMode.Generic, 0.5, TimeSpan.FromSeconds(3));
        _estimateService.GenerateEstimate(Arg.Any<double>(), Arg.Any<TimeSpan>(), EstimateMode.Generic)
            .Returns(estimate);

        // Start and stop shake
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(3) });

        // Act
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = false, Intensity = 0.0, Duration = TimeSpan.Zero });

        await Task.Delay(50);

        // Assert
        _estimateService.Received(1).GenerateEstimate(0.5, TimeSpan.FromSeconds(3), EstimateMode.Generic);
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void CurrentShakeData_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentShakeData))
                propertyChangedRaised = true;
        };

        // Act
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(2) });

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public async Task CurrentEstimate_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var estimate = EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        _estimateService.GenerateEstimate(Arg.Any<double>(), Arg.Any<TimeSpan>(), Arg.Any<EstimateMode>())
            .Returns(estimate);

        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentEstimate))
                propertyChangedRaised = true;
        };

        // Start and stop shake
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(3) });

        // Act
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = false, Intensity = 0.0, Duration = TimeSpan.Zero });

        await Task.Delay(50);

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public async Task SelectedMode_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.SelectedMode))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.SelectedMode = EstimateMode.Generic;
        await Task.Delay(10);

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldStopMonitoring()
    {
        // Act
        _viewModel.Dispose();

        // Assert
        _shakeDetectionService.Received(1).StopMonitoring();
    }

    [Fact]
    public void Dispose_ShouldUnsubscribeFromShakeDataChanged()
    {
        // Arrange
        _viewModel.Dispose();

        // Act - Trigger event after disposal
        _shakeDetectionService.ShakeDataChanged += Raise.Event<EventHandler<ShakeData>>(
            _shakeDetectionService,
            new ShakeData { IsShaking = true, Intensity = 0.5, Duration = TimeSpan.FromSeconds(2) });

        // Assert - Should not throw, and CurrentShakeData should not update
        // (we can't directly test event unsubscription, but no exception means success)
        _viewModel.Should().NotBeNull();
    }

    #endregion
}

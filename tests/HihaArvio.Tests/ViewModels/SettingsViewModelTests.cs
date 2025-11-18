using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using HihaArvio.ViewModels;
using NSubstitute;

namespace HihaArvio.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly IStorageService _storageService;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _storageService = Substitute.For<IStorageService>();

        // Setup default storage service behavior
        _storageService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings
        {
            SelectedMode = EstimateMode.Work,
            MaxHistorySize = 10
        }));

        _viewModel = new SettingsViewModel(_storageService);
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
        var vm = new SettingsViewModel(storageService);
        await Task.Delay(50); // Give async initialization time to complete

        // Assert
        vm.SelectedMode.Should().Be(EstimateMode.Generic);
        vm.MaxHistorySize.Should().Be(20);
        await storageService.Received(1).LoadSettingsAsync();
    }

    [Fact]
    public void Constructor_ShouldHaveSaveSettingsCommand()
    {
        // Assert
        _viewModel.SaveSettingsCommand.Should().NotBeNull();
    }

    #endregion

    #region Save Settings Tests

    [Fact]
    public async Task SaveSettingsCommand_ShouldSaveToStorage()
    {
        // Act
        await _viewModel.SaveSettingsCommand.ExecuteAsync(null);

        // Assert
        await _storageService.Received(1).SaveSettingsAsync(Arg.Any<AppSettings>());
    }

    [Fact]
    public async Task SaveSettingsCommand_ShouldSaveCurrentSettings()
    {
        // Arrange
        _viewModel.SelectedMode = EstimateMode.Generic;
        _viewModel.MaxHistorySize = 25;

        // Act
        await _viewModel.SaveSettingsCommand.ExecuteAsync(null);

        // Assert
        await _storageService.Received(1).SaveSettingsAsync(Arg.Is<AppSettings>(s =>
            s.SelectedMode == EstimateMode.Generic &&
            s.MaxHistorySize == 25));
    }

    #endregion

    #region Selected Mode Tests

    [Fact]
    public void SelectedMode_WhenChanged_ShouldUpdateProperty()
    {
        // Act
        _viewModel.SelectedMode = EstimateMode.Generic;

        // Assert
        _viewModel.SelectedMode.Should().Be(EstimateMode.Generic);
    }

    [Fact]
    public void SelectedMode_ShouldSupportWorkMode()
    {
        // Act
        _viewModel.SelectedMode = EstimateMode.Work;

        // Assert
        _viewModel.SelectedMode.Should().Be(EstimateMode.Work);
    }

    [Fact]
    public void SelectedMode_ShouldSupportGenericMode()
    {
        // Act
        _viewModel.SelectedMode = EstimateMode.Generic;

        // Assert
        _viewModel.SelectedMode.Should().Be(EstimateMode.Generic);
    }

    // Note: Humorous mode is only triggered by easter egg, not selectable in UI

    #endregion

    #region Max History Size Tests

    [Fact]
    public void MaxHistorySize_WhenChanged_ShouldUpdateProperty()
    {
        // Act
        _viewModel.MaxHistorySize = 50;

        // Assert
        _viewModel.MaxHistorySize.Should().Be(50);
    }

    [Fact]
    public void MaxHistorySize_ShouldAcceptValidValues()
    {
        // Act & Assert
        var testAction = () => _viewModel.MaxHistorySize = 100;
        testAction.Should().NotThrow();
        _viewModel.MaxHistorySize.Should().Be(100);
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void SelectedMode_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.SelectedMode))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.SelectedMode = EstimateMode.Generic;

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void MaxHistorySize_WhenChanged_ShouldRaisePropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.MaxHistorySize))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.MaxHistorySize = 50;

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ChangeSettings_ThenSave_ShouldPersistChanges()
    {
        // Arrange
        _viewModel.SelectedMode = EstimateMode.Generic;
        _viewModel.MaxHistorySize = 30;

        // Act
        await _viewModel.SaveSettingsCommand.ExecuteAsync(null);

        // Assert
        await _storageService.Received(1).SaveSettingsAsync(Arg.Is<AppSettings>(s =>
            s.SelectedMode == EstimateMode.Generic &&
            s.MaxHistorySize == 30));
    }

    [Fact]
    public async Task LoadSettings_ShouldPopulateProperties()
    {
        // Arrange - Create new instance with specific settings
        var storageService = Substitute.For<IStorageService>();
        var settings = new AppSettings { SelectedMode = EstimateMode.Generic, MaxHistorySize = 50 };
        storageService.LoadSettingsAsync().Returns(Task.FromResult(settings));

        // Act
        var vm = new SettingsViewModel(storageService);
        await Task.Delay(50); // Give async load time to complete

        // Assert
        vm.SelectedMode.Should().Be(EstimateMode.Generic);
        vm.MaxHistorySize.Should().Be(50);
    }

    #endregion
}

using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using HihaArvio.ViewModels;
using NSubstitute;

namespace HihaArvio.Tests.ViewModels;

/// <summary>
/// Tests for the SettingsViewModel covering initialization, save operations, mode selection, history size configuration, property change notifications, and integration scenarios.
/// </summary>
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

    /// <summary>
    /// Verifies that settings are loaded from storage during initialization.
    /// </summary>
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

    /// <summary>
    /// Verifies that the SaveSettingsCommand is initialized.
    /// </summary>
    [Fact]
    public void Constructor_ShouldHaveSaveSettingsCommand()
    {
        // Assert
        _viewModel.SaveSettingsCommand.Should().NotBeNull();
    }

    #endregion

    #region Save Settings Tests

    /// <summary>
    /// Verifies that executing SaveSettingsCommand calls the storage service.
    /// </summary>
    [Fact]
    public async Task SaveSettingsCommand_ShouldSaveToStorage()
    {
        // Act
        await _viewModel.SaveSettingsCommand.ExecuteAsync(null);

        // Assert
        await _storageService.Received(1).SaveSettingsAsync(Arg.Any<AppSettings>());
    }

    /// <summary>
    /// Verifies that the current SelectedMode and MaxHistorySize values are saved.
    /// </summary>
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

    /// <summary>
    /// Verifies that setting SelectedMode updates the property value.
    /// </summary>
    [Fact]
    public void SelectedMode_WhenChanged_ShouldUpdateProperty()
    {
        // Act
        _viewModel.SelectedMode = EstimateMode.Generic;

        // Assert
        _viewModel.SelectedMode.Should().Be(EstimateMode.Generic);
    }

    /// <summary>
    /// Verifies that Work mode can be selected.
    /// </summary>
    [Fact]
    public void SelectedMode_ShouldSupportWorkMode()
    {
        // Act
        _viewModel.SelectedMode = EstimateMode.Work;

        // Assert
        _viewModel.SelectedMode.Should().Be(EstimateMode.Work);
    }

    /// <summary>
    /// Verifies that Generic mode can be selected.
    /// </summary>
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

    /// <summary>
    /// Verifies that setting MaxHistorySize updates the property value.
    /// </summary>
    [Fact]
    public void MaxHistorySize_WhenChanged_ShouldUpdateProperty()
    {
        // Act
        _viewModel.MaxHistorySize = 50;

        // Assert
        _viewModel.MaxHistorySize.Should().Be(50);
    }

    /// <summary>
    /// Verifies that valid MaxHistorySize values are accepted without exceptions.
    /// </summary>
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

    /// <summary>
    /// Verifies that changing SelectedMode raises PropertyChanged.
    /// </summary>
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

    /// <summary>
    /// Verifies that changing MaxHistorySize raises PropertyChanged.
    /// </summary>
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

    /// <summary>
    /// Verifies that modified settings are correctly persisted when saved.
    /// </summary>
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

    /// <summary>
    /// Verifies that loaded settings populate the ViewModel properties.
    /// </summary>
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

using HihaArvio.Models;

namespace HihaArvio.Services.Interfaces;

/// <summary>
/// Service for persisting application settings and estimate history.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Saves application settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Loads application settings.
    /// </summary>
    /// <returns>The loaded settings, or default settings if none exist.</returns>
    Task<AppSettings> LoadSettingsAsync();

    /// <summary>
    /// Saves an estimate result to history.
    /// Automatically prunes history if it exceeds the maximum size.
    /// </summary>
    /// <param name="estimate">The estimate to save.</param>
    Task SaveEstimateAsync(EstimateResult estimate);

    /// <summary>
    /// Gets estimate history, ordered by timestamp (newest first).
    /// </summary>
    /// <param name="count">Maximum number of estimates to retrieve (default 10).</param>
    /// <returns>List of estimate results, newest first.</returns>
    Task<List<EstimateResult>> GetHistoryAsync(int count = 10);

    /// <summary>
    /// Clears all estimate history.
    /// </summary>
    Task ClearHistoryAsync();

    /// <summary>
    /// Gets the current count of estimates in history.
    /// </summary>
    /// <returns>The number of estimates in storage.</returns>
    Task<int> GetHistoryCountAsync();
}

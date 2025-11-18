using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using SQLite;

namespace HihaArvio.Services;

/// <summary>
/// SQLite-based storage service for persisting application settings and estimate history.
/// </summary>
public class StorageService : IStorageService
{
    private readonly SQLiteAsyncConnection _database;

    public StorageService(string databasePath)
    {
        _database = new SQLiteAsyncConnection(databasePath);
        InitializeDatabaseAsync().Wait();
    }

    private async Task InitializeDatabaseAsync()
    {
        await _database.CreateTableAsync<SettingsEntity>();
        await _database.CreateTableAsync<EstimateHistoryEntity>();
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var entity = new SettingsEntity
        {
            Id = 1, // Single settings row
            SelectedMode = settings.SelectedMode,
            MaxHistorySize = settings.MaxHistorySize
        };

        var existing = await _database.Table<SettingsEntity>().FirstOrDefaultAsync(s => s.Id == 1);
        if (existing != null)
        {
            await _database.UpdateAsync(entity);
        }
        else
        {
            await _database.InsertAsync(entity);
        }
    }

    /// <inheritdoc/>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        var entity = await _database.Table<SettingsEntity>().FirstOrDefaultAsync(s => s.Id == 1);

        if (entity == null)
        {
            // Return default settings if none exist
            return new AppSettings
            {
                SelectedMode = EstimateMode.Work,
                MaxHistorySize = 10
            };
        }

        return new AppSettings
        {
            SelectedMode = entity.SelectedMode,
            MaxHistorySize = entity.MaxHistorySize
        };
    }

    /// <inheritdoc/>
    public async Task SaveEstimateAsync(EstimateResult estimate)
    {
        var entity = new EstimateHistoryEntity
        {
            Id = estimate.Id.ToString(),
            Timestamp = estimate.Timestamp,
            EstimateText = estimate.EstimateText,
            Mode = estimate.Mode,
            ShakeIntensity = estimate.ShakeIntensity,
            ShakeDuration = estimate.ShakeDuration
        };

        await _database.InsertAsync(entity);

        // Auto-prune based on MaxHistorySize setting
        var settings = await LoadSettingsAsync();
        var count = await GetHistoryCountAsync();

        if (count > settings.MaxHistorySize)
        {
            var excessCount = count - settings.MaxHistorySize;
            var oldestEstimates = await _database.Table<EstimateHistoryEntity>()
                .OrderBy(e => e.Timestamp)
                .Take(excessCount)
                .ToListAsync();

            foreach (var old in oldestEstimates)
            {
                await _database.DeleteAsync(old);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<List<EstimateResult>> GetHistoryAsync(int count = 10)
    {
        if (count <= 0)
        {
            return new List<EstimateResult>();
        }

        var entities = await _database.Table<EstimateHistoryEntity>()
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();

        return entities.Select(e =>
        {
            var result = EstimateResult.Create(
                e.EstimateText,
                e.Mode,
                e.ShakeIntensity,
                e.ShakeDuration
            );
            result.Id = Guid.Parse(e.Id);
            result.Timestamp = e.Timestamp;
            return result;
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task ClearHistoryAsync()
    {
        await _database.DeleteAllAsync<EstimateHistoryEntity>();
    }

    /// <inheritdoc/>
    public async Task<int> GetHistoryCountAsync()
    {
        return await _database.Table<EstimateHistoryEntity>().CountAsync();
    }

    /// <summary>
    /// Internal entity for storing settings in SQLite.
    /// </summary>
    [Table("Settings")]
    private class SettingsEntity
    {
        [PrimaryKey]
        public int Id { get; set; }

        public EstimateMode SelectedMode { get; set; }

        public int MaxHistorySize { get; set; }
    }

    /// <summary>
    /// Internal entity for storing estimate history in SQLite.
    /// </summary>
    [Table("EstimateHistory")]
    private class EstimateHistoryEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public DateTimeOffset Timestamp { get; set; }

        public string EstimateText { get; set; } = string.Empty;

        public EstimateMode Mode { get; set; }

        public double ShakeIntensity { get; set; }

        public TimeSpan ShakeDuration { get; set; }
    }
}

using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using SQLite;

namespace HihaArvio.Services;

/// <summary>
/// SQLite-based storage service for persisting application settings and estimate history.
/// </summary>
public class StorageService : IStorageService, IDisposable
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;
    private int _disposed;

    public StorageService(string databasePath)
    {
        _database = new SQLiteAsyncConnection(databasePath);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await _database.CreateTableAsync<SettingsEntity>();
                await _database.CreateTableAsync<EstimateHistoryEntity>();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await EnsureInitializedAsync();

        var entity = new SettingsEntity
        {
            Id = 1, // Single settings row
            SelectedMode = settings.SelectedMode,
            MaxHistorySize = settings.MaxHistorySize
        };

        await _database.InsertOrReplaceAsync(entity);
    }

    /// <inheritdoc/>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        await EnsureInitializedAsync();

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
        await EnsureInitializedAsync();

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

            var ids = string.Join("','", oldestEstimates.Select(e => e.Id));
            await _database.ExecuteAsync($"DELETE FROM EstimateHistory WHERE Id IN ('{ids}')");
        }
    }

    /// <inheritdoc/>
    public async Task<List<EstimateResult>> GetHistoryAsync(int count = 10)
    {
        await EnsureInitializedAsync();

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
        await EnsureInitializedAsync();
        await _database.DeleteAllAsync<EstimateHistoryEntity>();
    }

    /// <inheritdoc/>
    public async Task<int> GetHistoryCountAsync()
    {
        await EnsureInitializedAsync();
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

    /// <summary>
    /// Disposes the SemaphoreSlim used for initialization locking.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _initLock.Dispose();
    }
}

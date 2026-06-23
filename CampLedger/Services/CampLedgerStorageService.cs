using System.Diagnostics;
using System.Text.Json;
using CampLedger.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace CampLedger.Services;

public sealed class CampLedgerStorageService : ICampLedgerStorageService
{
    private const string DefaultDatabaseFileName = "campledger.db3";
    private const string PreferencesStateKey = "camp-ledger-state";
    private const string MigrationFlagKey = "camp-ledger-sqlite-migrated";
    private readonly string _databasePath;
    private readonly IPreferences _preferences;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly object _saveLock = new();
    private Task _pendingSaveTask = Task.CompletedTask;
    private SQLiteAsyncConnection? _database;

    public CampLedgerStorageService()
        : this(Path.Combine(FileSystem.AppDataDirectory, DefaultDatabaseFileName), null)
    {
    }

    public CampLedgerStorageService(string databasePath)
        : this(databasePath, null)
    {
    }

    public CampLedgerStorageService(string databasePath, IPreferences? preferences)
    {
        _databasePath = databasePath;
        _preferences = preferences ?? CreatePreferences();
    }

    private static IPreferences CreatePreferences()
    {
        try
        {
            return Preferences.Default;
        }
        catch (Exception)
        {
            return new NoOpPreferences();
        }
    }

    public CampLedgerState Load()
    {
        try
        {
            Task pendingSave;
            lock (_saveLock)
            {
                pendingSave = _pendingSaveTask;
            }

            pendingSave.GetAwaiter().GetResult();
            return LoadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Storage load failed: {ex.Message}");
            return new CampLedgerState();
        }
    }

    public void Save(CampLedgerState state)
    {
        try
        {
            lock (_saveLock)
            {
                _pendingSaveTask = Task.Run(() => SaveAsync(state));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Storage save scheduling failed: {ex.Message}");
        }
    }

    public async Task CloseConnectionAsync()
    {
        SQLiteAsyncConnection? database;
        lock (_saveLock)
        {
            database = _database;
            _database = null;
        }

        if (database is not null)
        {
            await database.CloseAsync();
        }
    }

    private async Task<CampLedgerState> LoadAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            await EnsurePreferencesMigrationAsync(database);
            var inventoryRows = await database.Table<InventoryItemEntity>().ToListAsync();
            var tripRows = await database.Table<TripRecordEntity>().ToListAsync();
            var checklistRows = await database.Table<TripChecklistItemEntity>().ToListAsync();

            var state = new CampLedgerState();
            state.Needs = inventoryRows
                .Where(row => row.Bucket == (int)InventoryBucket.Needs)
                .Select(ToInventoryItem)
                .ToList();
            state.Wants = inventoryRows
                .Where(row => row.Bucket == (int)InventoryBucket.Wants)
                .Select(ToInventoryItem)
                .ToList();
            state.Has = inventoryRows
                .Where(row => row.Bucket == (int)InventoryBucket.Has)
                .Select(ToInventoryItem)
                .ToList();

            var currentTrip = tripRows.FirstOrDefault(row => row.IsCurrent);
            if (currentTrip is not null)
            {
                state.CurrentTrip = ToTripRecord(currentTrip, checklistRows);
            }

            state.TripHistory = tripRows
                .Where(row => !row.IsCurrent)
                .Select(row => ToTripRecord(row, checklistRows))
                .ToList();

            return state;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Storage load failed: {ex.Message}");
            return new CampLedgerState();
        }
    }

    private async Task SaveAsync(CampLedgerState state)
    {
        try
        {
            var database = await GetDatabaseAsync();
            await PersistStateAsync(database, state);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Storage save failed: {ex.Message}");
        }
    }

    private async Task EnsurePreferencesMigrationAsync(SQLiteAsyncConnection database)
    {
        try
        {
            if (_preferences.Get(MigrationFlagKey, false, null))
            {
                return;
            }

            if (!_preferences.ContainsKey(PreferencesStateKey, null))
            {
                return;
            }

            var preferencesJson = _preferences.Get(PreferencesStateKey, string.Empty, null);
            if (string.IsNullOrWhiteSpace(preferencesJson))
            {
                return;
            }

            if (await HasPersistedStateAsync(database))
            {
                _preferences.Set(MigrationFlagKey, true, null);
                return;
            }

            var migratedState = JsonSerializer.Deserialize<CampLedgerState>(preferencesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (migratedState is null)
            {
                return;
            }

            await PersistStateAsync(database, migratedState);
            _preferences.Set(MigrationFlagKey, true, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Preferences migration failed: {ex.Message}");
        }
    }

    private async Task<bool> HasPersistedStateAsync(SQLiteAsyncConnection database)
    {
        var inventoryCount = await database.Table<InventoryItemEntity>().CountAsync();
        if (inventoryCount > 0)
        {
            return true;
        }

        var tripCount = await database.Table<TripRecordEntity>().CountAsync();
        return tripCount > 0;
    }

    private async Task PersistStateAsync(SQLiteAsyncConnection database, CampLedgerState state)
    {
        await database.DeleteAllAsync<TripChecklistItemEntity>();
        await database.DeleteAllAsync<TripRecordEntity>();
        await database.DeleteAllAsync<InventoryItemEntity>();

        foreach (var inventoryItem in state.Needs.Concat(state.Wants).Concat(state.Has))
        {
            await database.InsertAsync(ToInventoryEntity(inventoryItem));
        }

        var currentTrip = state.CurrentTrip ?? new TripRecord();
        var currentTripEntity = ToTripRecordEntity(currentTrip, isCurrent: true);
        await database.InsertAsync(currentTripEntity);
        foreach (var checklistItem in currentTrip.Items)
        {
            await database.InsertAsync(ToTripChecklistItemEntity(currentTripEntity.Id, checklistItem));
        }

        foreach (var completedTrip in state.TripHistory)
        {
            var tripEntity = ToTripRecordEntity(completedTrip, isCurrent: false);
            await database.InsertAsync(tripEntity);
            foreach (var checklistItem in completedTrip.Items)
            {
                await database.InsertAsync(ToTripChecklistItemEntity(tripEntity.Id, checklistItem));
            }
        }
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database is not null)
        {
            return _database;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_database is not null)
            {
                return _database;
            }

            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _database = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
            try
            {
                await _database.ExecuteAsync("PRAGMA journal_mode=WAL;");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WAL initialization skipped: {ex.Message}");
            }

            await _database.CreateTableAsync<InventoryItemEntity>();
            await _database.CreateTableAsync<TripRecordEntity>();
            await _database.CreateTableAsync<TripChecklistItemEntity>();
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_TripChecklistItems_TripRecordId ON TripChecklistItems (TripRecordId);");
            return _database;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static InventoryItem ToInventoryItem(InventoryItemEntity entity)
    {
        return new InventoryItem
        {
            Id = Guid.TryParse(entity.Id, out var id) ? id : Guid.NewGuid(),
            Name = entity.Name,
            Bucket = (InventoryBucket)entity.Bucket,
            PhotoData = entity.PhotoData
        };
    }

    private static InventoryItemEntity ToInventoryEntity(InventoryItem inventoryItem)
    {
        return new InventoryItemEntity
        {
            Id = inventoryItem.Id.ToString(),
            Name = inventoryItem.Name,
            Bucket = (int)inventoryItem.Bucket,
            PhotoData = inventoryItem.PhotoData
        };
    }

    private static TripRecord ToTripRecord(TripRecordEntity entity, IEnumerable<TripChecklistItemEntity> checklistRows)
    {
        var trip = new TripRecord
        {
            Id = Guid.TryParse(entity.Id, out var id) ? id : Guid.NewGuid(),
            Date = entity.Date,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Notes = entity.Notes,
            Location = string.IsNullOrWhiteSpace(entity.LocationName) && string.IsNullOrWhiteSpace(entity.LocationUrl)
                ? null
                : new TripLocation
                {
                    LocationName = entity.LocationName,
                    GoogleMapsUrl = entity.LocationUrl
                },
            Items = checklistRows
                .Where(row => string.Equals(row.TripRecordId, entity.Id, StringComparison.Ordinal))
                .Select(row => new TripChecklistItem
                {
                    ItemId = Guid.TryParse(row.ItemId, out var itemId) ? itemId : Guid.NewGuid(),
                    Name = row.Name,
                    IsPacked = row.IsPacked,
                    PhotoData = row.PhotoData
                })
                .ToList()
        };

        return trip;
    }

    private static TripRecordEntity ToTripRecordEntity(TripRecord tripRecord, bool isCurrent)
    {
        return new TripRecordEntity
        {
            Id = tripRecord.Id.ToString(),
            Date = tripRecord.Date,
            StartDate = tripRecord.StartDate,
            EndDate = tripRecord.EndDate,
            Notes = tripRecord.Notes,
            LocationName = tripRecord.Location?.LocationName ?? string.Empty,
            LocationUrl = tripRecord.Location?.GoogleMapsUrl ?? string.Empty,
            IsCurrent = isCurrent
        };
    }

    private static TripChecklistItemEntity ToTripChecklistItemEntity(string tripRecordId, TripChecklistItem checklistItem)
    {
        return new TripChecklistItemEntity
        {
            Id = Guid.NewGuid().ToString(),
            TripRecordId = tripRecordId,
            ItemId = checklistItem.ItemId.ToString(),
            Name = checklistItem.Name,
            IsPacked = checklistItem.IsPacked,
            PhotoData = checklistItem.PhotoData
        };
    }

    private sealed class NoOpPreferences : IPreferences
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public bool ContainsKey(string key, string? sharedName)
        {
            return _values.ContainsKey(GetCompositeKey(key, sharedName));
        }

        public void Remove(string key, string? sharedName)
        {
            _values.Remove(GetCompositeKey(key, sharedName));
        }

        public void Clear(string? sharedName)
        {
            if (sharedName is null)
            {
                _values.Clear();
                return;
            }

            var prefix = sharedName + ":";
            foreach (var key in _values.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                _values.Remove(key);
            }
        }

        public string Get(string key, string defaultValue, string? sharedName)
        {
            return _values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is string stringValue
                ? stringValue
                : defaultValue;
        }

        public bool Get(string key, bool defaultValue, string? sharedName)
        {
            return _values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is bool boolValue
                ? boolValue
                : defaultValue;
        }

        public int Get(string key, int defaultValue, string? sharedName)
        {
            return _values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is int intValue
                ? intValue
                : defaultValue;
        }

        public long Get(string key, long defaultValue, string? sharedName)
        {
            return _values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is long longValue
                ? longValue
                : defaultValue;
        }

        public float Get(string key, float defaultValue, string? sharedName)
        {
            return _values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is float floatValue
                ? floatValue
                : defaultValue;
        }

        public double Get(string key, double defaultValue, string? sharedName)
        {
            return _values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is double doubleValue
                ? doubleValue
                : defaultValue;
        }

        public void Set(string key, string value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value;
        }

        public void Set(string key, bool value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value;
        }

        public void Set(string key, int value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value;
        }

        public void Set(string key, long value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value;
        }

        public void Set(string key, float value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value;
        }

        public void Set(string key, double value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value;
        }

        public T Get<T>(string key, T defaultValue, string? sharedName)
        {
            return _values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is T typedValue
                ? typedValue
                : defaultValue;
        }

        public void Set<T>(string key, T value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value ?? throw new ArgumentNullException(nameof(value));
        }

        private static string GetCompositeKey(string key, string? sharedName)
        {
            return string.IsNullOrWhiteSpace(sharedName) ? key : string.Concat(sharedName, ":", key);
        }
    }

    [Table("InventoryItems")]
    public sealed class InventoryItemEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Bucket { get; set; }

        public byte[]? PhotoData { get; set; }
    }

    [Table("TripRecords")]
    public sealed class TripRecordEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Notes { get; set; } = string.Empty;

        public string LocationName { get; set; } = string.Empty;

        public string LocationUrl { get; set; } = string.Empty;

        public bool IsCurrent { get; set; }
    }

    [Table("TripChecklistItems")]
    public sealed class TripChecklistItemEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public string TripRecordId { get; set; } = string.Empty;

        public string ItemId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsPacked { get; set; }

        public byte[]? PhotoData { get; set; }
    }

}

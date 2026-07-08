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
    private const string MigrationInProgressKey = "camp-ledger-sqlite-migrating";
    private readonly string _databasePath;
    private readonly IPreferences _preferences;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        _databasePath = ResolveDatabasePath(databasePath);
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

    private static string ResolveDatabasePath(string requestedPath)
    {
        var candidates = GetDatabasePathCandidates(requestedPath).ToList();
        string? preferredPath = null;
        var preferredScore = -1;

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            var stateScore = GetDatabaseStateScore(candidate);
            if (stateScore > preferredScore)
            {
                preferredPath = candidate;
                preferredScore = stateScore;
            }
        }

        if (!string.IsNullOrWhiteSpace(preferredPath))
        {
            return preferredPath;
        }

        return requestedPath;
    }

    private static int GetDatabaseStateScore(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return 0;
        }

        try
        {
            using var connection = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.FullMutex);
            var inventoryCount = TryGetTableRowCount(connection, "InventoryItems");
            var tripCount = TryGetTableRowCount(connection, "TripRecords");
            var checklistCount = TryGetTableRowCount(connection, "TripChecklistItems");
            return inventoryCount + tripCount + checklistCount;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static int TryGetTableRowCount(SQLiteConnection connection, string tableName)
    {
        try
        {
            return connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tableName}");
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static IEnumerable<string> GetDatabasePathCandidates(string requestedPath)
    {
        var normalizedPath = Path.GetFullPath(requestedPath);
        var directory = Path.GetDirectoryName(normalizedPath);
        var fileName = Path.GetFileName(normalizedPath);

        yield return normalizedPath;

        if (!string.IsNullOrWhiteSpace(directory))
        {
            yield return Path.Combine(directory, fileName);
        }

        foreach (var fallback in GetFallbackDatabasePaths(fileName))
        {
            yield return fallback;
        }
    }

    private static IEnumerable<string> GetFallbackDatabasePaths(string fileName)
    {
        var paths = new List<string>();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            paths.Add(Path.Combine(localAppData, "User Name", "EJRitterDevelopment.campledger", "Data", fileName));
            paths.Add(Path.Combine(localAppData, "EJRitterDevelopment.campledger", "Data", fileName));
            paths.Add(Path.Combine(localAppData, "CampLedger", fileName));
        }

        try
        {
            var appDataDirectory = FileSystem.AppDataDirectory;
            if (!string.IsNullOrWhiteSpace(appDataDirectory))
            {
                paths.Add(Path.Combine(appDataDirectory, fileName));
                paths.Add(Path.Combine(appDataDirectory, "Data", fileName));
            }
        }
        catch (Exception)
        {
        }

        return paths;
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

            // Run the async load on the thread pool so its continuations never
            // resume on a captured UI SynchronizationContext. Blocking the UI
            // thread on an async SQLite call directly deadlocks Windows startup
            // (CreateWindow never completes and no native window is shown).
            return Task.Run(async () =>
            {
                await pendingSave.ConfigureAwait(false);
                return await LoadAsync().ConfigureAwait(false);
            }).GetAwaiter().GetResult();
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
            var hasPersistedState = await HasPersistedStateAsync(database);
            var migrationInProgress = _preferences.Get(MigrationInProgressKey, false, null);

            if (!migrationInProgress && hasPersistedState)
            {
                MarkMigrationComplete();
                ClearMigrationInProgress();
                return;
            }

            if (!_preferences.ContainsKey(PreferencesStateKey, null))
            {
                MarkMigrationComplete();
                ClearMigrationInProgress();
                return;
            }

            var preferencesJson = _preferences.Get(PreferencesStateKey, string.Empty, null);
            if (string.IsNullOrWhiteSpace(preferencesJson))
            {
                MarkMigrationComplete();
                ClearMigrationInProgress();
                return;
            }

            var migratedState = TryDeserializeState(preferencesJson);
            if (migratedState is null)
            {
                MarkMigrationComplete();
                ClearMigrationInProgress();
                return;
            }

            MarkMigrationInProgress();
            await PersistStateAsync(database, migratedState);
            MarkMigrationComplete();
            ClearMigrationInProgress();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Preferences migration failed: {ex.Message}");
        }
    }

    private void MarkMigrationComplete()
    {
        _preferences.Set(MigrationFlagKey, true, null);
    }

    private void MarkMigrationInProgress()
    {
        _preferences.Set(MigrationInProgressKey, true, null);
    }

    private void ClearMigrationInProgress()
    {
        _preferences.Remove(MigrationInProgressKey, null);
    }

    private static CampLedgerState? TryDeserializeState(string preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(preferencesJson);
            if (LooksLikeLegacyEnvelope(document.RootElement))
            {
                var legacyState = TryDeserializeLegacyEnvelope(document.RootElement);
                if (legacyState is not null)
                {
                    return legacyState;
                }
            }
        }
        catch (JsonException)
        {
        }

        try
        {
            var state = JsonSerializer.Deserialize<CampLedgerState>(preferencesJson, SerializerOptions);
            if (state is not null)
            {
                var normalizedState = NormalizeState(state);
                if (HasStateData(normalizedState))
                {
                    return normalizedState;
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static CampLedgerState NormalizeState(CampLedgerState state)
    {
        state.Needs ??= [];
        state.Wants ??= [];
        state.Has ??= [];
        state.CurrentTrip ??= new TripRecord();
        state.TripHistory ??= [];
        return state;
    }

    private static bool HasStateData(CampLedgerState state)
    {
        if (state.Needs.Count > 0 || state.Wants.Count > 0 || state.Has.Count > 0)
        {
            return true;
        }

        if (state.TripHistory.Count > 0)
        {
            return true;
        }

        if (state.CurrentTrip is not null && (state.CurrentTrip.Items.Count > 0 || !string.IsNullOrWhiteSpace(state.CurrentTrip.Notes) || state.CurrentTrip.Location is not null))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeLegacyEnvelope(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return TryGetProperty(root, "inventory", out _) ||
               TryGetProperty(root, "needs", out _) ||
               TryGetProperty(root, "wants", out _) ||
               TryGetProperty(root, "has", out _) ||
               TryGetProperty(root, "currentTrip", out _) ||
               TryGetProperty(root, "tripHistory", out _);
    }

    private static CampLedgerState? TryDeserializeLegacyEnvelope(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var state = new CampLedgerState();
        var foundAnyInventory = false;
        var foundAnyState = false;

        if (TryGetProperty(root, "inventory", out var inventoryElement))
        {
            foundAnyInventory = true;
            foundAnyState = true;
            TryPopulateInventoryBuckets(inventoryElement, state);
        }

        if (!foundAnyInventory)
        {
            TryPopulateInventoryBuckets(root, state);
            foundAnyState = state.Needs.Count > 0 || state.Wants.Count > 0 || state.Has.Count > 0;
        }

        if (TryGetProperty(root, "currentTrip", out var currentTripElement))
        {
            state.CurrentTrip = ParseTripRecord(currentTripElement) ?? new TripRecord();
            foundAnyState = true;
        }

        if (TryGetProperty(root, "tripHistory", out var tripHistoryElement) && tripHistoryElement.ValueKind == JsonValueKind.Array)
        {
            state.TripHistory = ParseTripHistory(tripHistoryElement);
            foundAnyState = true;
        }

        if (!foundAnyState)
        {
            return null;
        }

        return NormalizeState(state);
    }

    private static void TryPopulateInventoryBuckets(JsonElement root, CampLedgerState state)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            var inventoryItems = ParseInventoryItems(root);
            state.Needs = inventoryItems.Where(item => item.Bucket == InventoryBucket.Needs).ToList();
            state.Wants = inventoryItems.Where(item => item.Bucket == InventoryBucket.Wants).ToList();
            state.Has = inventoryItems.Where(item => item.Bucket == InventoryBucket.Has).ToList();
            return;
        }

        if (TryGetProperty(root, "inventory", out var inventoryElement))
        {
            if (inventoryElement.ValueKind == JsonValueKind.Array)
            {
                var inventoryItems = ParseInventoryItems(inventoryElement);
                state.Needs = inventoryItems.Where(item => item.Bucket == InventoryBucket.Needs).ToList();
                state.Wants = inventoryItems.Where(item => item.Bucket == InventoryBucket.Wants).ToList();
                state.Has = inventoryItems.Where(item => item.Bucket == InventoryBucket.Has).ToList();
                return;
            }

            if (inventoryElement.ValueKind == JsonValueKind.Object)
            {
                TryPopulateInventoryBuckets(inventoryElement, state);
                return;
            }
        }

        if (TryGetProperty(root, "needs", out var needsElement))
        {
            state.Needs = ParseInventoryItems(needsElement);
        }

        if (TryGetProperty(root, "wants", out var wantsElement))
        {
            state.Wants = ParseInventoryItems(wantsElement);
        }

        if (TryGetProperty(root, "has", out var hasElement))
        {
            state.Has = ParseInventoryItems(hasElement);
        }
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static List<InventoryItem> ParseInventoryItems(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<InventoryItem>();
        foreach (var itemElement in element.EnumerateArray())
        {
            if (itemElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var item = new InventoryItem();
            if (TryGetProperty(itemElement, "name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                item.Name = nameElement.GetString() ?? string.Empty;
            }

            if (TryGetProperty(itemElement, "bucket", out var bucketElement))
            {
                if (bucketElement.ValueKind == JsonValueKind.Number && bucketElement.TryGetInt32(out var bucketValue))
                {
                    item.Bucket = (InventoryBucket)bucketValue;
                }
                else if (bucketElement.ValueKind == JsonValueKind.String && Enum.TryParse<InventoryBucket>(bucketElement.GetString(), true, out var bucket))
                {
                    item.Bucket = bucket;
                }
            }

            if (TryGetProperty(itemElement, "photoData", out var photoDataElement) && photoDataElement.ValueKind == JsonValueKind.String)
            {
                item.PhotoData = Convert.FromBase64String(photoDataElement.GetString() ?? string.Empty);
            }

            items.Add(item);
        }

        return items;
    }

    private static List<TripRecord> ParseTripHistory(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var trips = new List<TripRecord>();
        foreach (var tripElement in element.EnumerateArray())
        {
            var trip = ParseTripRecord(tripElement);
            if (trip is not null)
            {
                trips.Add(trip);
            }
        }

        return trips;
    }

    private static TripRecord? ParseTripRecord(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var trip = new TripRecord();

        if (TryGetProperty(element, "notes", out var notesElement) && notesElement.ValueKind == JsonValueKind.String)
        {
            trip.Notes = notesElement.GetString() ?? string.Empty;
        }

        if (TryGetProperty(element, "date", out var dateElement) && dateElement.ValueKind == JsonValueKind.String)
        {
            trip.Date = DateTime.TryParse(dateElement.GetString(), out var date) ? date : trip.Date;
        }

        if (TryGetProperty(element, "startDate", out var startDateElement) && startDateElement.ValueKind == JsonValueKind.String)
        {
            trip.StartDate = DateTime.TryParse(startDateElement.GetString(), out var date) ? date : trip.StartDate;
        }

        if (TryGetProperty(element, "endDate", out var endDateElement) && endDateElement.ValueKind == JsonValueKind.String)
        {
            trip.EndDate = DateTime.TryParse(endDateElement.GetString(), out var date) ? date : trip.EndDate;
        }

        if (TryGetProperty(element, "location", out var locationElement) && locationElement.ValueKind == JsonValueKind.Object)
        {
            trip.Location = new TripLocation();
            if (TryGetProperty(locationElement, "locationName", out var locationNameElement) && locationNameElement.ValueKind == JsonValueKind.String)
            {
                trip.Location.LocationName = locationNameElement.GetString() ?? string.Empty;
            }

            if (TryGetProperty(locationElement, "googleMapsUrl", out var googleMapsUrlElement) && googleMapsUrlElement.ValueKind == JsonValueKind.String)
            {
                trip.Location.GoogleMapsUrl = googleMapsUrlElement.GetString() ?? string.Empty;
            }
        }

        if (TryGetProperty(element, "items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var checklistItemElement in itemsElement.EnumerateArray())
            {
                if (checklistItemElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var checklistItem = new TripChecklistItem();
                if (TryGetProperty(checklistItemElement, "name", out var itemNameElement) && itemNameElement.ValueKind == JsonValueKind.String)
                {
                    checklistItem.Name = itemNameElement.GetString() ?? string.Empty;
                }

                if (TryGetProperty(checklistItemElement, "isPacked", out var packedElement) && packedElement.ValueKind == JsonValueKind.True)
                {
                    checklistItem.IsPacked = true;
                }
                else if (TryGetProperty(checklistItemElement, "isPacked", out packedElement) && packedElement.ValueKind == JsonValueKind.False)
                {
                    checklistItem.IsPacked = false;
                }

                if (TryGetProperty(checklistItemElement, "photoData", out var photoElement) && photoElement.ValueKind == JsonValueKind.String)
                {
                    checklistItem.PhotoData = Convert.FromBase64String(photoElement.GetString() ?? string.Empty);
                }

                trip.Items.Add(checklistItem);
            }
        }

        return trip;
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

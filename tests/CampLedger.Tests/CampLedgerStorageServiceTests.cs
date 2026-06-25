using System.Text.Json;
using CampLedger.Models;
using CampLedger.Services;
using Microsoft.Maui.Storage;

namespace CampLedger.Tests;

public sealed class CampLedgerStorageServiceTests
{
    private const string PreferencesStateKey = "camp-ledger-state";

    [Fact]
    public async Task SaveAndLoad_RoundTripsInventoryAndTripState()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campledger-tests-{Guid.NewGuid():N}.db3");
        var sut = new CampLedgerStorageService(databasePath);
        var state = new CampLedgerState
        {
            Needs =
            [
                new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs },
                new InventoryItem { Name = "Water", Bucket = InventoryBucket.Needs }
            ],
            Wants =
            [
                new InventoryItem { Name = "Lantern", Bucket = InventoryBucket.Wants }
            ],
            Has =
            [
                new InventoryItem { Name = "Backpack", Bucket = InventoryBucket.Has }
            ],
            CurrentTrip = new TripRecord
            {
                Notes = "Pack early",
                Location = new TripLocation { LocationName = "Yellowstone", GoogleMapsUrl = "https://maps.example/1" },
                Items =
                [
                    new TripChecklistItem { Name = "Tent", IsPacked = false }
                ]
            },
            TripHistory =
            [
                new TripRecord
                {
                    Notes = "Previous trip",
                    Items =
                    [
                        new TripChecklistItem { Name = "Backpack", IsPacked = true }
                    ]
                }
            ]
        };

        sut.Save(state);

        var reloaded = sut.Load();

        await sut.CloseConnectionAsync();

        Assert.Equal(2, reloaded.Needs.Count);
        Assert.Equal("Tent", reloaded.Needs[0].Name);
        Assert.Single(reloaded.Wants);
        Assert.Single(reloaded.Has);
        Assert.Equal("Pack early", reloaded.CurrentTrip.Notes);
        Assert.Equal("Yellowstone", reloaded.CurrentTrip.Location!.LocationName);
        Assert.Single(reloaded.CurrentTrip.Items);
        Assert.Single(reloaded.TripHistory);
        Assert.Equal("Previous trip", reloaded.TripHistory[0].Notes);

        File.Delete(databasePath);
    }

    [Fact]
    public async Task Load_WhenPreferencesStateExistsAndSqliteIsEmpty_MigratesPreferencesIntoSqlite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campledger-tests-{Guid.NewGuid():N}.db3");
        var preferences = new InMemoryPreferences();
        var state = new CampLedgerState
        {
            Needs =
            [
                new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs }
            ],
            Wants =
            [
                new InventoryItem { Name = "Lantern", Bucket = InventoryBucket.Wants }
            ],
            Has =
            [
                new InventoryItem { Name = "Backpack", Bucket = InventoryBucket.Has }
            ]
        };

        preferences.Set("camp-ledger-state", JsonSerializer.Serialize(state), null);

        var sut = new CampLedgerStorageService(databasePath, preferences);
        var reloaded = sut.Load();

        await sut.CloseConnectionAsync();

        Assert.Single(reloaded.Needs);
        Assert.Single(reloaded.Wants);
        Assert.Single(reloaded.Has);
        Assert.True(preferences.Get("camp-ledger-sqlite-migrated", false, null));

        File.Delete(databasePath);
    }

    [Fact]
    public async Task Load_WhenPreferencesPayloadUsesLegacyEnvelope_MigratesIntoSqlite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campledger-tests-{Guid.NewGuid():N}.db3");
        var preferences = new InMemoryPreferences();
        var legacyPayload = new
        {
            inventory = new
            {
                needs = new[]
                {
                    new { name = "Tent", bucket = 0 }
                },
                wants = Array.Empty<object>(),
                has = Array.Empty<object>()
            },
            currentTrip = new
            {
                notes = "Pack early",
                items = new[]
                {
                    new { name = "Tent", isPacked = false }
                }
            },
            tripHistory = Array.Empty<object>()
        };

        preferences.Set("camp-ledger-state", JsonSerializer.Serialize(legacyPayload), null);

        var sut = new CampLedgerStorageService(databasePath, preferences);
        var reloaded = sut.Load();

        await sut.CloseConnectionAsync();

        Assert.Single(reloaded.Needs);
        Assert.Equal("Tent", reloaded.Needs[0].Name);
        Assert.Equal("Pack early", reloaded.CurrentTrip.Notes);
        Assert.Single(reloaded.CurrentTrip.Items);
        Assert.True(preferences.Get("camp-ledger-sqlite-migrated", false, null));

        File.Delete(databasePath);
    }

    [Fact]
    public async Task Load_WhenMigrationFlagIsSetButSqliteIsEmpty_MigratesPreferencesIntoSqlite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"campledger-tests-{Guid.NewGuid():N}.db3");
        var preferences = new InMemoryPreferences();
        var state = new CampLedgerState
        {
            Needs =
            [
                new InventoryItem { Name = "Tent", Bucket = InventoryBucket.Needs }
            ]
        };

        preferences.Set("camp-ledger-state", JsonSerializer.Serialize(state), null);
        preferences.Set("camp-ledger-sqlite-migrated", true, null);

        var sut = new CampLedgerStorageService(databasePath, preferences);
        var reloaded = sut.Load();

        await sut.CloseConnectionAsync();

        Assert.Single(reloaded.Needs);
        Assert.True(preferences.Get("camp-ledger-sqlite-migrated", false, null));

        File.Delete(databasePath);
    }

    private sealed class InMemoryPreferences : IPreferences
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
            if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is string stringValue)
            {
                return stringValue;
            }

            return defaultValue;
        }

        public bool Get(string key, bool defaultValue, string? sharedName)
        {
            if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is bool boolValue)
            {
                return boolValue;
            }

            return defaultValue;
        }

        public int Get(string key, int defaultValue, string? sharedName)
        {
            if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is int intValue)
            {
                return intValue;
            }

            return defaultValue;
        }

        public long Get(string key, long defaultValue, string? sharedName)
        {
            if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is long longValue)
            {
                return longValue;
            }

            return defaultValue;
        }

        public float Get(string key, float defaultValue, string? sharedName)
        {
            if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is float floatValue)
            {
                return floatValue;
            }

            return defaultValue;
        }

        public double Get(string key, double defaultValue, string? sharedName)
        {
            if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is double doubleValue)
            {
                return doubleValue;
            }

            return defaultValue;
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
            if (_values.TryGetValue(GetCompositeKey(key, sharedName), out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        public void Set<T>(string key, T value, string? sharedName)
        {
            _values[GetCompositeKey(key, sharedName)] = value ?? throw new ArgumentNullException(nameof(value));
        }

        private static string GetCompositeKey(string key, string? sharedName)
        {
            if (string.IsNullOrWhiteSpace(sharedName))
            {
                return key;
            }

            return string.Concat(sharedName, ":", key);
        }
    }
}

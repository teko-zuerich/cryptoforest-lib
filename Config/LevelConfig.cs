using CryptoForestLibrary.Cryptograph;
using CryptoForestLibrary.Cryptograph.Algorithm;
using CryptoForestLibrary.Cryptograph.Storage;
using CryptoForestLibrary.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoForestLibrary.Config;

/// <summary>
/// Used for storing the values of a level
/// </summary>
public class LevelConfig : ItemConfig
{
    private KeyIV? _previousKey;

    private KeyIV XorKeyIV
    {
        get
        {
            if (_previousKey != null)
            {
                var xorKey = PerformXOR(KeyIV.Key, _previousKey.Key);
                var xorIV = PerformXOR(KeyIV.IV, _previousKey.IV);

                return new KeyIV(xorKey, xorIV);
            }

            // As the current level is the base level the keyIV can/should only be returned unaltered
            return KeyIV;

            // Performs the XOR operation for each byte
            static byte[] PerformXOR(byte[] b1, byte[] b2)
            {
                if (b1.Length != b2.Length)
                {
                    throw new ArgumentException("b1 and b2 don't have the same length");
                }

                var result = new byte[b1.Length];
                for (var i = 0; i < b1.Length; i++)
                {
                    result[i] = (byte)(b1[i] ^ b2[i]);
                }

                return result;
            }
        }
    }

    internal Dictionary<string, ItemConfig> Items { get; }

    [JsonIgnore]
    internal Dictionary<string, LevelConfig> Sublevels { get; }
    private Dictionary<string, Guid>? _sublevelNameMappings;
    /// <summary>
    /// Stores the sublevel name mappings as they need to be exported.
    /// When the config is loaded and this property is set the Sublevels dictionary will be adjusted to reflect the actual names.
    /// </summary>
    internal Dictionary<string, Guid> SublevelNameMappings
    {
        get
        {
            var nameMappings = new Dictionary<string, Guid>();
            foreach (var sublevel in Sublevels)
            {
                nameMappings.Add(sublevel.Key, sublevel.Value.EntryGuid);
            }

            return nameMappings;
        }
        set
        {
            _sublevelNameMappings = value;
        }
    }

    internal LevelConfig(CryptoForestConfig config) : base(config.ConfigGuid, config.KeyIV, ItemType.Level)
    {
        Items = [];
        Sublevels = [];

        // Creates the sublevels
        var sublevelConfigs = config.Sublevels.Select(l => new LevelConfig(l, config.KeyIV)).ToArray();
        for (var i = 0; i < sublevelConfigs.Length; i++)
        {
            // temp is used here as the names of the sublevel aren't known until they are decrypted
            Sublevels.Add($"temp{i}", sublevelConfigs[i]);
        }
    }

    internal LevelConfig(CryptoForestConfig config, KeyIV parentKeyIV) : this(config)
    {
        _previousKey = parentKeyIV;
    }

    internal LevelConfig(Guid configGuid, KeyIV keyIV, KeyIV parentKeyIV) : base(configGuid, keyIV, ItemType.Level)
    {
        _previousKey = parentKeyIV;
        Items = [];
        Sublevels = [];
    }

    internal async Task LoadConfigAsync<T>(CryptoForestCryptograph<T> cryptograph, CancellationToken cancellationToken)
        where T : ICryptoForestAlgorithm, new()
    {
        // Decrypt config
        var levelConfigJson = await cryptograph.DecryptTextAsync(EntryGuid, XorKeyIV, cancellationToken);
        var levelConfig = JsonSerializer.Deserialize<LevelConfig>(levelConfigJson)!;

        // Update sublevel names
        var sublevels = new Dictionary<string, LevelConfig>();
        foreach (var nameMapping in _sublevelNameMappings!)
        {
            sublevels.Add(nameMapping.Key, Sublevels.Values.Single(l => l.EntryGuid == nameMapping.Value));
        }

        Sublevels.Clear();
        foreach (var sublevel in sublevels)
        {
            Sublevels.Add(sublevel.Key, sublevel.Value);
        }

        // Add items
        foreach (var item in levelConfig.Items)
        {
            Items.Add(item.Key, item.Value);
        }
    }

    internal async Task SaveConfigAsync<T>(CryptoForestCryptograph<T> cryptograph, ICryptoForestStorage storage, CancellationToken cancellationToken)
        where T : ICryptoForestAlgorithm, new()
    {
        if (storage.EntryExists(EntryGuid))
        {
            await storage.RemoveEntryAsync(EntryGuid, cancellationToken);
        }

        var levelConfigJson = JsonSerializer.Serialize(this);
        await cryptograph.EncryptTextAsync(levelConfigJson, EntryGuid, cancellationToken, XorKeyIV);
    }

    /// <summary>
    /// Returns all direct sublevels of the current level with their name as key and ItemConfig as value
    /// </summary>
    /// <returns>Returns all direct sublevels ordered by name</returns>
    public Dictionary<string, ItemConfig> GetLevels()
    {
        var levels = new Dictionary<string, ItemConfig>();
        foreach (var sublevel in Sublevels)
        {
            levels.Add(sublevel.Key, sublevel.Value);
        }

        // Orders the dictionary by level name
        // https://stackoverflow.com/questions/289/how-do-you-sort-a-dictionary-by-value
        var orderedLevels = from level in levels
                            orderby level.Key ascending
                            select level;
        return orderedLevels.ToDictionary();
    }

    /// <summary>
    /// Checks if a level with the GUID exists recursively
    /// </summary>
    /// <param name="guid">The GUID of the level</param>
    /// <returns>Returns whether or not a level with this GUID was found</returns>
    public bool HasLevel(Guid guid)
        => EntryGuid == guid || Sublevels.Values.Any(sublevel => sublevel.EntryGuid == guid || sublevel.HasLevel(guid));

    /// <summary>
    /// Searches a level based on a GUID.
    /// The search is conducted recursively so all sublevels will be searched.
    /// </summary>
    /// <param name="guid">The level GUID to search for</param>
    /// <returns>Returns the level with the GUID</returns>
    /// <exception cref="LevelNotFoundException">Thrown if the level cannot be found</exception>
    public LevelConfig GetLevel(Guid guid)
    {
        if (EntryGuid == guid)
        {
            return this;
        }

        var level = Sublevels.Values.SingleOrDefault(level => level.EntryGuid == guid);
        if (level == null)
        {
            foreach (var sublevel in Sublevels.Values)
            {
                if (sublevel.HasLevel(guid))
                {
                    return sublevel.GetLevel(guid);
                }
            }

            throw new LevelNotFoundException(guid);
        }

        return level;
    }

    /// <summary>
    /// Searches a level based on a item GUID.
    /// The search is conducted recursively so all sublevels will be searched.
    /// </summary>
    /// <param name="guid">The GUID of the item the level should be returned for</param>
    /// <returns>Returns the level containing the item</returns>
    /// <exception cref="ItemNotFoundException">Thrown if the item could not be found in any level</exception>
    public LevelConfig GetLevelOfItem(Guid guid)
    {
        var item = Items.Values.SingleOrDefault(item => item.EntryGuid == guid);
        if (item == null)
        {
            foreach (var sublevel in Sublevels.Values)
            {
                if (sublevel.HasItem(guid))
                {
                    if (sublevel.Items.Values.Any(i => i.EntryGuid == guid))
                    {
                        return sublevel;
                    }
                    else
                    {
                        return sublevel.GetLevelOfItem(guid);
                    }
                }
            }

            throw new ItemNotFoundException(guid);
        }

        return this;
    }

    /// <summary>
    /// Searches a parent level based on a level GUID.
    /// The search is conducted recursively so all sublevels will be searched.
    /// </summary>
    /// <param name="guid">The GUID of the child level the parent level should be returned for</param>
    /// <returns>Returns the parent level of the level</returns>
    /// <exception cref="LevelNotFoundException">Thrown if thee level could not be found in any level</exception>
    public LevelConfig GetLevelOfLevel(Guid guid)
    {
        var level = Sublevels.Values.SingleOrDefault(level => level.EntryGuid == guid);
        if (level == null)
        {
            foreach (var sublevel in Sublevels.Values)
            {
                if (sublevel.HasLevel(guid))
                {
                    if (sublevel.Sublevels.Values.Any(l => l.EntryGuid == guid))
                    {
                        return sublevel;
                    }
                    else
                    {
                        return sublevel.GetLevelOfLevel(guid);
                    }
                }
            }

            throw new LevelNotFoundException(guid);
        }

        return level;
    }

    /// <summary>
    /// Returns all items from the current level with their name as key and ItemConfig as value
    /// </summary>
    /// <returns>Returns all items from the current level ordered by name</returns>
    public Dictionary<string, ItemConfig> GetItems()
    {
        var items = new Dictionary<string, ItemConfig>();
        foreach (var item in Items)
        {
            items.Add(item.Key, item.Value);
        }

        // Orders the dictionary by item name
        // https://stackoverflow.com/questions/289/how-do-you-sort-a-dictionary-by-value
        var orderedItems = from item in items
                           orderby item.Key ascending
                           select item;
        return orderedItems.ToDictionary();
    }

    /// <summary>
    /// Checks if an item with the GUID exists in any of the levels
    /// </summary>
    /// <param name="guid">The GUID of the item</param>
    /// <returns>Returns whether or not an item with this GUID was found</returns>
    public bool HasItem(Guid guid)
        => Items.Values.Any(item => item.EntryGuid == guid) || Sublevels.Values.Any(sublevel => sublevel.HasItem(guid));

    /// <summary>
    /// Searches a level based on a GUID.
    /// The search is conducted recursively so all sublevels will be searched.
    /// </summary>
    /// <param name="guid">The item GUID to search for</param>
    /// <returns>Returns the item with the GUID</returns>
    /// <exception cref="ItemNotFoundException">Thrown if the item cannot be found</exception>
    public ItemConfig GetItem(Guid guid)
    {
        var item = Items.Values.SingleOrDefault(item => item.EntryGuid == guid);
        if (item == null)
        {
            foreach (var sublevel in Sublevels.Values)
            {
                if (sublevel.HasItem(guid))
                {
                    return sublevel.GetItem(guid);
                }
            }

            throw new ItemNotFoundException(guid);
        }

        return item;
    }

    /// <summary>
    /// Searches items and levels based on their name in all sublevels.
    /// For the search it uses the Contains method.
    /// </summary>
    /// <param name="searchString">The string to search for</param>
    /// <returns>Returns the found levels and items ordered by name</returns>
    public Dictionary<string, ItemConfig> SearchItems(string searchString)
    {
        var foundItems = new Dictionary<string, ItemConfig>();
        var foundLevels = new Dictionary<string, LevelConfig>();
        SearchItemsInLevel(this, searchString);

        // Orders the dictionaries by name
        // https://stackoverflow.com/questions/289/how-do-you-sort-a-dictionary-by-value
        var orderedLevels = from level in foundLevels
                            orderby level.Value ascending
                            select level;
        var orderedItems = from item in foundItems
                           orderby item.Value ascending
                           select item;

        // Combines the two dictionaries to one single dictionary
        var searchResult = new Dictionary<string, ItemConfig>();
        foreach (var level in orderedLevels)
        {
            searchResult.Add(level.Key, level.Value);
        }

        foreach (var item in orderedItems)
        {
            searchResult.Add(item.Key, item.Value);
        }

        return searchResult;

        // Recursively searches items and levels while going through the whole tree structure
        void SearchItemsInLevel(LevelConfig level, string searchString)
        {
            foreach (var foundItem in Items.Where(item => item.Key.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
            {
                foundItems.Add(foundItem.Key, foundItem.Value);
            }

            foreach (var foundLevel in Sublevels.Where(level => level.Key.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
            {
                foundLevels.Add(foundLevel.Key, foundLevel.Value);
            }

            foreach (var sublevel in Sublevels.Values)
            {
                SearchItemsInLevel(sublevel, searchString);
            }
        }
    }
}

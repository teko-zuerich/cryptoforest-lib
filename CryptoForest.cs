using CryptoForestLibrary.Config;
using CryptoForestLibrary.Cryptograph;
using CryptoForestLibrary.Cryptograph.Algorithm;
using CryptoForestLibrary.Cryptograph.Storage;
using CryptoForestLibrary.DirectoryStructure;
using CryptoForestLibrary.Exceptions;
using System.Security.Cryptography;

namespace CryptoForestLibrary;

/// <summary>
/// Used for creating and managing a CryptoForest
/// </summary>
/// <typeparam name="T">Type of the algorithm used in the CryptoForest</typeparam>
public class CryptoForest<T>
    where T : ICryptoForestAlgorithm, new()
{
    private readonly ICryptoForestStorage _storage;
    private readonly CryptoForestCryptograph<T> _cryptograph;
    private readonly LevelConfig _baseLevel;

    /// <summary>
    /// Opens a CryptoForest using the provided storage and loads the structure from the encrypted file with the key.
    /// </summary>
    /// <param name="storage">The storage to used in the CryptoForest</param>
    /// <param name="key">The key used to decrypt the file containing the encrypted config</param>
    /// <param name="filePath">The path to the encrypted file containing the config</param>
    /// <exception cref="CryptoForestConfigException">Thrown when the config could not be decrypted</exception>
    public CryptoForest(ICryptoForestStorage storage, byte[] key, string filePath)
    {
        _storage = storage;
        _cryptograph = new CryptoForestCryptograph<T>(storage);

        // Decrypt the config and load the structure
        try
        {
            var config = _cryptograph.DecryptConfigAsync(key, filePath, cancellationToken: default).Result;
            _baseLevel = new LevelConfig(config);
        }
        catch
        {
            throw new CryptoForestConfigException(encrypting: false);
        }
    }

    /// <summary>
    /// Used internally when a CryptoForest is created with the CreateCryptoForest method.
    /// </summary>
    /// <param name="storage">The storage used in the new CryptoForest</param>
    /// <param name="baseLevel">The base level of the new CryptoForest</param>
    private CryptoForest(ICryptoForestStorage storage, LevelConfig baseLevel)
    {
        _storage = storage;
        _cryptograph = new CryptoForestCryptograph<T>(storage);
        _baseLevel = baseLevel;
    }

    /// <summary>
    /// Creates and opens a new CryptoForest using the storage provided.
    /// </summary>
    /// <param name="storage">The storage used in the new CryptoForest</param>
    /// <returns>An instance of the CryptoForest with the provided storage</returns>
    public static CryptoForest<T> CreateCryptoForest(ICryptoForestStorage storage)
        => new CryptoForest<T>(storage, new LevelConfig(new CryptoForestConfig
        {
            ConfigGuid = GenerateSecureGuid(storage),
            KeyIV = new T().GenerateKeyIV(),
            Sublevels = []
        }));

    /// <summary>
    /// Get the LevelConfig of the base level which can be used for finding files and levels.
    /// </summary>
    /// <returns>Returns the base level of the open CryptoForest</returns>
    public LevelConfig GetBaseLevel()
        => _baseLevel;

    /// <summary>
    /// Adds a text item to the CryptoForest.
    /// </summary>
    /// <param name="text">The text to encrypt and add</param>
    /// <param name="itemKey">The key/name of the item</param>
    /// <param name="levelGuid">The GUID of the level the item should be added to</param>
    /// <returns>Returns the GUID of the added item or if ther was an issue when adding the item Guid.Empty</returns>
    /// <exception cref="LevelNotFoundException">Thrown if the level of the levelGuid could not be found in the structure</exception>
    /// <exception cref="ItemAlreadyExistsException">Thrown if an item with the same itemKey already exists on the level</exception>
    public async Task<Guid> AddItemAsync(string text, string itemKey, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        return await AddItemBaseAsync(itemKey, levelGuid, EncryptItemAsync, ItemType.Text, cancellationToken);

        async Task<KeyIV> EncryptItemAsync(Guid itemGuid, CancellationToken cancellationToken)
        {
            return await _cryptograph.EncryptTextAsync(text, itemGuid, cancellationToken);
        }
    }

    /// <summary>
    /// Adds a data item to the CryptoForest.
    /// </summary>
    /// <param name="fileSearch">FileSearch used to search for the files to encrypt and combine</param>
    /// <param name="itemKey">The key/name of the item</param>
    /// <param name="levelGuid">The GUID of the level the item should be added to</param>
    /// <returns>Returns the GUID of the added item or if ther was an issue when adding the item Guid.Empty</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the directory that should be searched does not exist</exception>
    /// <exception cref="LevelNotFoundException">Thrown if the level of the levelGuid could not be found in the structure</exception>
    /// <exception cref="ItemAlreadyExistsException">Thrown if an item with the same itemKey already exists on the level</exception>
    public async Task<Guid> AddItemAsync(FileSearch fileSearch, string itemKey, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(fileSearch.SearchedDirectory))
        {
            throw new DirectoryNotFoundException($"The directory {fileSearch.SearchedDirectory} doesn't exist");
        }

        return await AddItemBaseAsync(itemKey, levelGuid, EncryptItemAsync, ItemType.Files, cancellationToken);

        async Task<KeyIV> EncryptItemAsync(Guid itemGuid, CancellationToken cancellationToken)
        {
            return await _cryptograph.EncryptFilesAsync(fileSearch, itemGuid, cancellationToken);
        }
    }

    /// <summary>
    /// The base implementation to add an item to the CryptoForest used internally.
    /// </summary>
    /// <param name="itemKey">The key/name of the item</param>
    /// <param name="levelGuid">The GUID of the level the item should be added to</param>
    /// <param name="performEncryption">The function called to perform the encryption</param>
    /// <param name="itemType">The item type of the new item</param>
    /// <returns>Returns the GUID of the added item or if ther was an issue when adding the item Guid.Empty</returns>
    /// <exception cref="LevelNotFoundException">Thrown if the level of the levelGuid could not be found in the structure</exception>
    /// <exception cref="ItemAlreadyExistsException">Thrown if an item with the same itemKey already exists on the level</exception>
    private async Task<Guid> AddItemBaseAsync(string itemKey, Guid levelGuid, Func<Guid, CancellationToken, Task<KeyIV>> performEncryption, ItemType itemType, CancellationToken cancellationToken)
    {
        // Gets the level or throws a LevelNotFoundException
        var level = _baseLevel.GetLevel(levelGuid);
        if (level.Items.ContainsKey(itemKey))
        {
            throw new ItemAlreadyExistsException(itemKey);
        }

        try
        {
            var itemGuid = GenerateSecureGuid();
            var keyIV = await performEncryption(itemGuid, cancellationToken);
            level.Items.Add(itemKey, new ItemConfig(itemGuid, keyIV, itemType));
            await level.SaveConfigAsync(_cryptograph, cancellationToken);

            return itemGuid;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Removes an item from the CryptoForest.
    /// </summary>
    /// <param name="itemGuid">The GUID of the item to remove</param>
    /// <returns>Returns true when the removal was successfull</returns>
    /// <exception cref="ItemNotFoundException">Thrown if the item with the GUID could not be found in the structure</exception>
    public async Task<bool> RemoveItemAsync(Guid itemGuid, CancellationToken cancellationToken = default)
    {
        if (!_baseLevel.HasItem(itemGuid))
        {
            throw new ItemNotFoundException(itemGuid);
        }

        try
        {
            // We should check first if the entry exists before removing it as otherwise it would be impossible to remove an entry if the entry in the storage was removed already
            if (_storage.EntryExists(itemGuid))
            {
                await _storage.RemoveEntryAsync(itemGuid, cancellationToken);
            }

            var level = _baseLevel.GetLevelOfItem(itemGuid);
            var item = level.Items.Single(i => i.Value.EntryGuid == itemGuid);
            level.Items.Remove(item.Key);
            await level.SaveConfigAsync(_cryptograph, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Decrypts a text item in the CryptoForest.
    /// </summary>
    /// <param name="itemGuid">The GUID of the item to decrypt</param>
    /// <returns>Returns the decrypted text or string.Empty if the decryption wasn't successfull</returns>
    /// <exception cref="ItemNotFoundException">Thrown when the item with the itemGuid could not be found in the structure</exception>
    /// <exception cref="InvalidOperationException">Thrown if the item was found but was not of the expected type Text</exception>
    public async Task<string> GetTextItemAsync(Guid itemGuid, CancellationToken cancellationToken = default)
    {
        return await GetItemBaseAsync(itemGuid, DecryptItemAsync, ItemType.Text, cancellationToken);

        async Task<string> DecryptItemAsync(ItemConfig item, CancellationToken cancellationToken)
        {
            try
            {
                return await _cryptograph.DecryptTextAsync(itemGuid, item.KeyIV, cancellationToken);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Decrypts a data item in the CryptoForest and puts the decrypted data into the SearchedDirectory structure.
    /// All the data will be loaded into the memory meaning there must be enought memory available or the decryption will fail.
    /// </summary>
    /// <param name="itemGuid">The GUID of the item to decrypt</param>
    /// <returns>Returns the decrypted SeachedDirectory containing the decrypted data</returns>
    /// <exception cref="ItemNotFoundException">Thrown when the item with the itemGuid could not be found in the structure</exception>
    /// <exception cref="InvalidOperationException">Thrown if the item was found but was not of the expected type Files</exception>
    public async Task<SearchedDirectory> GetDataItemAsync(Guid itemGuid, CancellationToken cancellationToken = default)
    {
        return await GetItemBaseAsync(itemGuid, DecryptItemAsync, ItemType.Files, cancellationToken);

        async Task<SearchedDirectory> DecryptItemAsync(ItemConfig item, CancellationToken cancellationToken)
        {
            try
            {
                return await _cryptograph.DecryptFilesAsync(itemGuid, item.KeyIV, cancellationToken);
            }
            catch
            {
                return new SearchedDirectory(string.Empty);
            }
        }
    }

    /// <summary>
    /// Decrypts a data item in the CryptoForest and writes the decrypted files to the file system.
    /// </summary>
    /// <param name="itemGuid">The GUID of the item to decrypt</param>
    /// <param name="saveDirectory">The directory to decrypt the files to</param>
    /// <param name="onFileExists">Specifies what should happen should a file already exist at the target location</param>
    /// <returns>Returns the decrypted SearchDirectory without data</returns>
    /// <exception cref="ItemNotFoundException">Thrown when the item with the itemGuid could not be found in the structure</exception>
    /// <exception cref="InvalidOperationException">Thrown if the item was found but was not of the expected type Files</exception>
    public async Task<SearchedDirectory> GetDataItemAsync(Guid itemGuid, string saveDirectory, OnFileExists onFileExists = OnFileExists.Throw, CancellationToken cancellationToken = default)
    {
        return await GetItemBaseAsync(itemGuid, DecryptItemAsync, ItemType.Files, cancellationToken);

        async Task<SearchedDirectory> DecryptItemAsync(ItemConfig item, CancellationToken cancellationToken)
        {
            try
            {
                return await _cryptograph.DecryptFilesAsync(itemGuid, item.KeyIV, saveDirectory, onFileExists, cancellationToken);
            }
            catch
            {
                return new SearchedDirectory(string.Empty);
            }
        }
    }

    /// <summary>
    /// Decrypts an item in the CryptoForest.
    /// Used internally as base for the decryption methods.
    /// </summary>
    /// <typeparam name="R">The type of the retuen value</typeparam>
    /// <param name="itemGuid">The GUID of the item to decrypt</param>
    /// <param name="performDecryption">The function called to decrypt the item</param>
    /// <param name="expectedItemType">The expected item type of the item</param>
    /// <returns>Returns the returned value of performDecryption</returns>
    /// <exception cref="ItemNotFoundException">Thrown when the item with the itemGuid could not be found in the structure</exception>
    /// <exception cref="InvalidOperationException">Thrown if the item was found but was not of the expected type</exception>
    private async Task<R> GetItemBaseAsync<R>(Guid itemGuid, Func<ItemConfig, CancellationToken, Task<R>> performDecryption, ItemType expectedItemType, CancellationToken cancellationToken)
    {
        if (!_baseLevel.HasItem(itemGuid))
        {
            throw new ItemNotFoundException(itemGuid);
        }

        var item = _baseLevel.GetItem(itemGuid);
        if (item.ItemType != expectedItemType)
        {
            throw new InvalidOperationException($"The item with guid {itemGuid} is not an item of type {Enum.GetName(expectedItemType)}");
        }

        return await performDecryption(item, cancellationToken);
    }

    /// <summary>
    /// Adds a level to the CryptoForest.
    /// </summary>
    /// <param name="levelKey">The key/name of the new level</param>
    /// <param name="parentLevelGuid">The GUID of the level the new level should be added to</param>
    /// <returns>Returns the Guid of the added level or Guid.Empty</returns>
    /// <exception cref="LevelNotFoundException">Thrown when the parent level could not be found in the structure</exception>
    /// <exception cref="LevelAlreadyExistsException">Thrown if a level with the same key already exists on the parent level</exception>
    public async Task<Guid> AddLevelAsync(string levelKey, Guid parentLevelGuid, CancellationToken cancellationToken = default)
    {
        // Gets the level or throws a LevelNotFoundException
        var parentLevel = _baseLevel.GetLevel(parentLevelGuid);
        if (parentLevel.Sublevels.Any(l => l.Key == levelKey))
        {
            throw new LevelAlreadyExistsException(levelKey);
        }

        try
        {
            var levelGuid = GenerateSecureGuid();
            var level = new LevelConfig(levelGuid, new T().GenerateKeyIV(), parentLevel.KeyIV);
            await parentLevel.SaveConfigAsync(_cryptograph, cancellationToken);
            await level.SaveConfigAsync(_cryptograph, cancellationToken);

            return levelGuid;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Removes a level from the CryptoForest.
    /// </summary>
    /// <param name="levelGuid">The GUID of the level to remove</param>
    /// <returns>Returns true if the removal was successfull</returns>
    /// <exception cref="LevelNotFoundException">Thrown when the level to remove could not be found</exception>
    public async Task<bool> RemoveLevelAsync(Guid levelGuid, CancellationToken cancellationToken = default)
    {
        if (!_baseLevel.HasLevel(levelGuid))
        {
            throw new LevelNotFoundException(levelGuid);
        }

        try
        {
            // We should check first if the entry exists before removing it as otherwise it would be impossible to remove an entry if the entry in the storage was removed already
            if (_storage.EntryExists(levelGuid))
            {
                await _storage.RemoveEntryAsync(levelGuid, cancellationToken);
            }

            var parentLevel = _baseLevel.GetLevelOfLevel(levelGuid);
            var level = parentLevel.Sublevels.Single(l => l.Value.EntryGuid == levelGuid);
            parentLevel.Sublevels.Remove(level.Key);
            await parentLevel.SaveConfigAsync(_cryptograph, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Exports a config used for opening the CryptoForest
    /// </summary>
    /// <param name="levelGuids">The GUIDs of the levels to export in any order</param>
    /// <param name="key">The key used when the config gets encrypted</param>
    /// <param name="filePath">The file path to encrypt the exported config to</param>
    /// <exception cref="CryptoForestConfigException">Thrown when the config could not be encrypted</exception>
    public async Task ExportConfigAsync(IEnumerable<Guid> levelGuids, byte[] key, string filePath, CancellationToken cancellationToken = default)
    {
        // Create the export config for the base level
        var exportConfig = new CryptoForestConfig()
        {
            ConfigGuid = _baseLevel.EntryGuid,
            KeyIV = _baseLevel.KeyIV,
            Sublevels = []
        };
        CreateStructure(_baseLevel, exportConfig);

        // Encrypt the config to the file system
        try
        {
            await _cryptograph.EncryptConfigAsync(exportConfig, key, filePath, cancellationToken);
        }
        catch
        {
            throw new CryptoForestConfigException(encrypting: true);
        }

        // Recursively goes through the currently loaded level structure and creates the structure for the levels included in levelGuids
        void CreateStructure(LevelConfig currentLevel, CryptoForestConfig currentExportLevel)
        {
            foreach (var sublevel in currentLevel.Sublevels.Values)
            {
                // If the level GUID is included in the levelGuids list it will be added to the structure and AddToStructure will be called to add any sublevels also contained in levelGuids
                if (levelGuids.Contains(sublevel.EntryGuid))
                {
                    var newExportLevel = new CryptoForestConfig
                    {
                        ConfigGuid = sublevel.EntryGuid,
                        KeyIV = sublevel.KeyIV,
                        Sublevels = []
                    };
                    currentExportLevel.Sublevels.Add(newExportLevel);
                    CreateStructure(sublevel, newExportLevel);
                }
            }
        }
    }

    /// <summary>
    /// Generates a secure GUID and makes sure that the GUID is not existing in the storage already.
    /// </summary>
    /// <returns>Returns the cryptographicaly secure GUID</returns>
    private Guid GenerateSecureGuid()
        => GenerateSecureGuid(_storage);

    /// <summary>
    /// Generates a secure GUID and makes sure that the GUID is not existing in the storage already and expects a storage.
    /// Used internally when creating a new CryptoForest.
    /// </summary>
    /// <param name="storage">The storage used to check the availability of the GUID</param>
    /// <returns>Returns the cryptographicaly secure GUID</returns>
    private static Guid GenerateSecureGuid(ICryptoForestStorage storage)
    {
        var guid = new Guid(RandomNumberGenerator.GetBytes(16));
        while (storage.EntryExists(guid))
        {
            guid = new Guid(RandomNumberGenerator.GetBytes(16));
        }

        return guid;
    }
}

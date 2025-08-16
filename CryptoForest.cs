using CryptoForestLibrary.Config;
using CryptoForestLibrary.Cryptograph;
using CryptoForestLibrary.Cryptograph.Algorithm;
using CryptoForestLibrary.Cryptograph.Storage;
using CryptoForestLibrary.DirectoryStructure;
using CryptoForestLibrary.Exceptions;
using System.Security.Cryptography;

namespace CryptoForestLibrary;
internal class CryptoForest<T>
    where T : ICryptoForestAlgorithm, new()
{
    private readonly ICryptoForestStorage _storage;
    private readonly CryptoForestCryptograph<T> _cryptograph;
    private readonly LevelConfig _baseLevel;

    public CryptoForest(ICryptoForestStorage storage, byte[] key, string filePath)
    {
        _storage = storage;
        _cryptograph = new CryptoForestCryptograph<T>(storage);
        // TODO decrypt file with key and default IV and set _baseLevel
    }

    private CryptoForest(ICryptoForestStorage storage, LevelConfig baseLevel)
    {
        _storage = storage;
        _cryptograph = new CryptoForestCryptograph<T>(storage);
        _baseLevel = baseLevel;
    }

    public static CryptoForest<T> CreateCryptoForest(ICryptoForestStorage storage)
        => new CryptoForest<T>(storage, new LevelConfig(new CryptoForestConfig
        {
            ConfigGuid = GenerateSecureGuid(storage),
            KeyIV = new T().GenerateKeyIV(),
            SubLevels = []
        }));

    public LevelConfig GetBaseLevel()
        => _baseLevel;

    public async Task<Guid> AddItemAsync(string text, string itemKey, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        return await AddItemBaseAsync(itemKey, levelGuid, EncryptItemAsync, ItemType.Text, cancellationToken);

        async Task<KeyIV> EncryptItemAsync(Guid itemGuid)
        {
            return await _cryptograph.EncryptTextAsync(text, itemGuid, cancellationToken);
        }
    }

    public async Task<Guid> AddItemAsync(FileSearch fileSearch, string itemKey, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(fileSearch.SearchedDirectory))
        {
            throw new DirectoryNotFoundException($"The directory {fileSearch.SearchedDirectory} doesn't exist");
        }

        return await AddItemBaseAsync(itemKey, levelGuid, EncryptItemAsync, ItemType.Files, cancellationToken);

        async Task<KeyIV> EncryptItemAsync(Guid itemGuid)
        {
            return await _cryptograph.EncryptFilesAsync(fileSearch, itemGuid, cancellationToken);
        }
    }

    private async Task<Guid> AddItemBaseAsync(string itemKey, Guid levelGuid, Func<Guid, Task<KeyIV>> performEncryption, ItemType itemType, CancellationToken cancellationToken)
    {
        if (!_baseLevel.HasLevel(levelGuid))
        {
            throw new LevelNotFoundException(levelGuid);
        }

        var level = _baseLevel.GetLevel(levelGuid);
        if (level.Items.ContainsKey(itemKey))
        {
            throw new ItemAlreadyExistsException(itemKey);
        }

        try
        {
            var itemGuid = GenerateSecureGuid();
            var keyIV = await performEncryption(itemGuid);
            level.Items.Add(itemKey, new ItemConfig(itemGuid, keyIV, itemType));
            await level.SaveConfigAsync(_cryptograph, cancellationToken);

            return itemGuid;
        }
        catch
        {
            return Guid.Empty;
        }
    }

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

    public async Task<string> GetTextItemAsync(Guid itemGuid, CancellationToken cancellationToken = default)
    {
        return await GetItemBaseAsync(itemGuid, DecryptItemAsync, ItemType.Text, cancellationToken);

        async Task<string> DecryptItemAsync(ItemConfig item)
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

    public async Task<SearchedDirectory> GetDataItemAsync(Guid itemGuid, CancellationToken cancellationToken = default)
    {
        return await GetItemBaseAsync(itemGuid, DecryptItemAsync, ItemType.Files, cancellationToken);

        async Task<SearchedDirectory> DecryptItemAsync(ItemConfig item)
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

    public async Task<SearchedDirectory> GetDataItemAsync(Guid itemGuid, string saveDirectory, OnFileExists onFileExists = OnFileExists.Throw, CancellationToken cancellationToken = default)
    {
        return await GetItemBaseAsync(itemGuid, DecryptItemAsync, ItemType.Files, cancellationToken);

        async Task<SearchedDirectory> DecryptItemAsync(ItemConfig item)
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

    private async Task<R> GetItemBaseAsync<R>(Guid itemGuid, Func<ItemConfig, Task<R>> performDecryption, ItemType expectedItemType, CancellationToken cancellationToken)
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

        return await performDecryption(item);
    }

    public async Task<Guid> AddLevelAsync(string levelKey, Guid parentLevelGuid, CancellationToken cancellationToken = default)
    {
        if (!_baseLevel.HasLevel(parentLevelGuid))
        {
            throw new LevelNotFoundException(parentLevelGuid);
        }

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

    public void ExportConfig(IEnumerable<Guid> levelGuids, byte[] key, string filePath)
    {
        throw new NotImplementedException();
    }

    private Guid GenerateSecureGuid()
        => GenerateSecureGuid(_storage);

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

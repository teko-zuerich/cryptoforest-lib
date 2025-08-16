using CryptoForestLibrary.Config;
using CryptoForestLibrary.Cryptograph;
using CryptoForestLibrary.Cryptograph.Algorithm;
using CryptoForestLibrary.Cryptograph.Storage;
using CryptoForestLibrary.DirectoryStructure;
using System.Security.Cryptography;

namespace CryptoForestLibrary;
internal class CryptoForest<T>
    where T : ICryptoForestAlgorithm, new()
{
    private readonly ICryptoForestStorage _storage;
    private readonly CryptoForestCryptograph<T> _cryptograph;
    private readonly LevelConfig _mainLevel;

    public CryptoForest(ICryptoForestStorage storage, byte[] key, string filePath)
    {
        _storage = storage;
        _cryptograph = new CryptoForestCryptograph<T>(storage);
        // TODO decrypt file with key and default IV and set mainLevel
    }

    private CryptoForest(ICryptoForestStorage storage, LevelConfig mainLevel)
    {
        _storage = storage;
        _cryptograph = new CryptoForestCryptograph<T>(storage);
        _mainLevel = mainLevel;
    }

    public static CryptoForest<T> CreateCryptoForest(ICryptoForestStorage storage)
        => new CryptoForest<T>(storage, new LevelConfig(new CryptoForestConfig
        {
            ConfigGuid = GenerateSecureGuid(storage),
            KeyIV = new T().GenerateKeyIV(),
            SubLevels = []
        }));

    public LevelConfig GetMainLevel()
        => _mainLevel;

    public async Task<Guid> AddItemAsync(string text, string itemKey, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        if (!_mainLevel.HasLevel(levelGuid))
        {
            throw new ArgumentException($"No level with GUID {levelGuid} exists", nameof(levelGuid));
        }

        try
        {
            var level = _mainLevel.GetLevel(levelGuid);
            var itemGuid = GenerateSecureGuid();
            var keyIV = await _cryptograph.EncryptTextAsync(text, itemGuid, cancellationToken);
            level.Items.Add(itemKey, new ItemConfig(itemGuid, keyIV, ItemType.Text));
            await level.SaveConfigAsync(_cryptograph, cancellationToken);

            return itemGuid;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    public async Task<Guid> AddItemAsync(FileSearch fileSearch, string itemKey, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> RemoveItemAsync(Guid itemGuid, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetTextItemAsync(Guid itemGuid, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<SearchedDirectory> GetDataItemAsync(Guid itemGuid, Guid levelGuid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<SearchedDirectory> GetDataItemAsync(Guid itemGuid, Guid levelGuid, string saveDirectory, OnFileExists onFileExists = OnFileExists.Throw, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Guid> AddLevelAsync(string levelKey, Guid parentLevelGuid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> RemoveLevelAsync(Guid levelGuid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

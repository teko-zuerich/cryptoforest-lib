using CryptoForestLibrary.Cryptograph;
using CryptoForestLibrary.Cryptograph.Storage;
using CryptoForestLibrary.DirectoryStructure;
using System.Security.Cryptography;
using System.Text;

namespace CryptoForestLibrary.Tests;

public class CryptoForestTests : IDisposable
{
    private CryptoForestMemoryStorage _storage;
    private AesCryptoForest _cryptoForest;
    private Guid _baseLevelGuid;

    public CryptoForestTests()
    {
        _storage = new CryptoForestMemoryStorage();
        _cryptoForest = AesCryptoForest.CreateCryptoForest(_storage);
        _baseLevelGuid = _cryptoForest.GetBaseLevel().EntryGuid;
    }

    [Fact]
    public void CreateCryptoForest()
    {
        Assert.True(_storage.EntryExists(_baseLevelGuid));
    }

    [Fact]
    public async Task CreateLevels()
    {
        var levelGuid = await _cryptoForest.AddLevelAsync("TestLevel", _baseLevelGuid);
        Assert.True(_storage.EntryExists(levelGuid));
        Assert.True(_storage.GetEntryCount() == 2);

        var sublevelGuid = await _cryptoForest.AddLevelAsync("TestSublevel", levelGuid);
        Assert.True(_storage.EntryExists(sublevelGuid));
        Assert.True(_storage.GetEntryCount() == 3);
    }

    [Fact]
    public async Task RemoveEmptyLevel()
    {
        var levelGuid = await _cryptoForest.AddLevelAsync("TestLevel", _baseLevelGuid);
        Assert.True(_storage.EntryExists(levelGuid));

        await _cryptoForest.RemoveLevelAsync(levelGuid);
        Assert.False(_storage.EntryExists(levelGuid));
    }

    [Theory]
    [InlineData("My Text")]
    [InlineData("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.")]
    public async Task EncryptText(string text)
    {
        var itemGuid = await _cryptoForest.AddItemAsync(text, "TestItem", _baseLevelGuid);
        Assert.True(_storage.EntryExists(itemGuid));
    }

    [Fact]
    public async Task EncryptFile()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/testFile.txt";
        var fileSearch = new FileSearch([path]);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);
        Assert.True(_storage.EntryExists(itemGuid));

        var originalSize = new FileInfo(path).Length;
        var encryptedSize = _storage.GetEntrySize(itemGuid);
        Assert.True(originalSize <= encryptedSize && originalSize * 1.05M >= encryptedSize);
    }

    [Fact]
    public async Task EncryptFiles()
    {
        var path1 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/testFile.txt";
        var path2 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/cryptoForestLogo.png";
        var fileSearch = new FileSearch([path1, path2]);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);
        Assert.True(_storage.EntryExists(itemGuid));

        var originalSize = new FileInfo(path1).Length + new FileInfo(path2).Length;
        var encryptedSize = _storage.GetEntrySize(itemGuid);
        Assert.True(originalSize <= encryptedSize && originalSize * 1.05M >= encryptedSize);
    }

    [Fact]
    public async Task EncryptDirectory()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);
        Assert.True(_storage.EntryExists(itemGuid));

        var originalSize = 0l;
        AddFileSizes(fileSearch.GetDirectoryAndFileStructure());
        var encryptedSize = _storage.GetEntrySize(itemGuid);
        Assert.True(originalSize <= encryptedSize && originalSize * 1.05M >= encryptedSize);

        void AddFileSizes(SearchedDirectory searchedDirectory)
        {
            foreach (var file in searchedDirectory.ChildFiles)
            {
                originalSize += new FileInfo(file.Path).Length;
            }

            foreach (var diretory in searchedDirectory.ChildDirectories)
            {
                AddFileSizes(diretory);
            }
        }
    }

    [Theory]
    [InlineData("My Text")]
    [InlineData("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.")]
    public async Task DecryptText(string text)
    {
        var itemGuid = await _cryptoForest.AddItemAsync(text, "TestItem", _baseLevelGuid);
        var decryptedText = await _cryptoForest.GetTextItemAsync(itemGuid);
        Assert.NotNull(decryptedText);
        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public async Task DecryptFile()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/testFile.txt";
        var fileSearch = new FileSearch([path]);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);
        var data = await _cryptoForest.GetDataItemAsync(itemGuid);

        var originalSize = new FileInfo(path).Length;
        var decryptedSize = data.ChildFiles.Single(f => f.FileName == "testFile.txt").FileDataLength;
        Assert.Equal(originalSize, decryptedSize);
    }

    [Fact]
    public async Task DecryptFiles()
    {
        var path1 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/testFile.txt";
        var path2 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/cryptoForestLogo.png";
        var fileSearch = new FileSearch([path1, path2]);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);
        var data = await _cryptoForest.GetDataItemAsync(itemGuid);

        var originalSize1 = new FileInfo(path1).Length;
        var decryptedSize1 = data.ChildFiles.Single(f => f.FileName == "testFile.txt").FileDataLength;
        Assert.Equal(originalSize1, decryptedSize1);

        var originalSize2 = new FileInfo(path2).Length;
        var decryptedSize2 = data.ChildFiles.Single(f => f.FileName == "cryptoForestLogo.png").FileDataLength;
        Assert.Equal(originalSize2, decryptedSize2);
    }

    [Fact]
    public async Task DecryptDirectory()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);
        var data = await _cryptoForest.GetDataItemAsync(itemGuid);

        var path1 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/testFile.txt";
        var originalSize1 = new FileInfo(path1).Length;
        var decryptedSize1 = data.ChildFiles.Single(f => f.FileName == "testFile.txt").FileDataLength;
        Assert.Equal(originalSize1, decryptedSize1);

        var path2 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/cryptoForestLogo.png";
        var originalSize2 = new FileInfo(path2).Length;
        var decryptedSize2 = data.ChildFiles.Single(f => f.FileName == "cryptoForestLogo.png").FileDataLength;
        Assert.Equal(originalSize2, decryptedSize2);

        var icons = data.ChildDirectories.Single(d => d.DirectoryName == "Icons");
        var path3 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/Icons/data_icon.png";
        var originalSize3 = new FileInfo(path3).Length;
        var decryptedSize3 = icons.ChildFiles.Single(f => f.FileName == "data_icon.png").FileDataLength;
        Assert.Equal(originalSize3, decryptedSize3);

        var path4 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/Icons/level_icon.png";
        var originalSize4 = new FileInfo(path4).Length;
        var decryptedSize4 = icons.ChildFiles.Single(f => f.FileName == "level_icon.png").FileDataLength;
        Assert.Equal(originalSize4, decryptedSize4);

        var path5 = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources/Icons/text_icon.png";
        var originalSize5 = new FileInfo(path5).Length;
        var decryptedSize5 = icons.ChildFiles.Single(f => f.FileName == "text_icon.png").FileDataLength;
        Assert.Equal(originalSize5, decryptedSize5);
    }

    [Fact]
    public async Task DecryptDirectoryThrows()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);
        
        var tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}/Temp";
        Directory.CreateDirectory(tempPath);
        File.Create($"{tempPath}/testFile.txt");
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => _cryptoForest.GetDataItemAsync(itemGuid, tempPath));
    }

    [Fact]
    public async Task DecryptDirectoryReplace()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);

        var tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}/Temp";
        Directory.CreateDirectory(tempPath);
        await File.Create($"{tempPath}/testFile.txt").DisposeAsync();
        await _cryptoForest.GetDataItemAsync(itemGuid, tempPath, OnFileExists.Replace);

        var originalSize = new FileInfo($"{path}/testFile.txt").Length;
        var decryptedSize = new FileInfo($"{tempPath}/testFile.txt").Length;
        Assert.Equal(originalSize, decryptedSize);
    }

    [Fact]
    public async Task DecryptDirectorySkip()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path);
        var itemGuid = await _cryptoForest.AddItemAsync(fileSearch, "TestItem", _baseLevelGuid);

        var tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}/Temp";
        Directory.CreateDirectory(tempPath);
        File.Create($"{tempPath}/testFile.txt");
        await _cryptoForest.GetDataItemAsync(itemGuid, tempPath, OnFileExists.Skip);

        var decryptedSize = new FileInfo($"{tempPath}/testFile.txt").Length;
        Assert.Equal(0, decryptedSize);
    }

    [Fact]
    public async Task ExportConfig()
    {
        var levelGuid = await _cryptoForest.AddLevelAsync("TestLevel", _baseLevelGuid);
        var itemGuid = await _cryptoForest.AddItemAsync("My Text", "TestItem", levelGuid);
        var passwordBytes = Encoding.UTF8.GetBytes("1234");
        var hashBytes = SHA256.HashData(passwordBytes);
        var tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}/Temp";
        Directory.CreateDirectory(tempPath);
        await _cryptoForest.ExportConfigAsync([_baseLevelGuid, levelGuid], hashBytes, $"{tempPath}/test");

        Assert.True(File.Exists($"{tempPath}/test"));
    }

    [Fact]
    public async Task OpenCryptoForest()
    {
        var levelGuid = await _cryptoForest.AddLevelAsync("TestLevel", _baseLevelGuid);
        var itemGuid = await _cryptoForest.AddItemAsync("My Text", "TestItem", levelGuid);
        var passwordBytes = Encoding.UTF8.GetBytes("1234");
        var hashBytes = SHA256.HashData(passwordBytes);
        var tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}/Temp";
        Directory.CreateDirectory(tempPath);
        await _cryptoForest.ExportConfigAsync([_baseLevelGuid, levelGuid], hashBytes, $"{tempPath}/test");

        var cryptoForest = new AesCryptoForest(_storage, hashBytes, $"{tempPath}/test");
    }

    public void Dispose()
    {
        _storage.Dispose();
        if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}/Temp"))
        {
            Directory.Delete($"{AppDomain.CurrentDomain.BaseDirectory}/Temp", recursive: true);
        }
    }
}
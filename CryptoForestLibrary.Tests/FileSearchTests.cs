using CryptoForestLibrary.DirectoryStructure;

namespace CryptoForestLibrary.Tests;
public class FileSearchTests
{
    [Fact]
    public void GetDirectory()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path);
        var searchedDirectory = fileSearch.GetDirectoryAndFileStructure();
        Assert.True(searchedDirectory.ChildFiles.Count == 2);
        Assert.True(searchedDirectory.ChildDirectories.Count == 1);
        Assert.NotNull(searchedDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "testFile.txt"));
        Assert.NotNull(searchedDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "cryptoForestLogo.png"));

        var iconsDirectory = searchedDirectory.ChildDirectories.SingleOrDefault(d => d.DirectoryName == "Icons");
        Assert.NotNull(iconsDirectory);
        Assert.True(iconsDirectory.ChildFiles.Count == 3);
        Assert.NotNull(iconsDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "data_icon.png"));
        Assert.NotNull(iconsDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "level_icon.png"));
        Assert.NotNull(iconsDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "text_icon.png"));
        Assert.Empty(iconsDirectory.ChildDirectories);
    }

    [Fact]
    public void GetDirectoryByEnding()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path, [".txt"], includePaths: true);
        var searchedDirectory = fileSearch.GetDirectoryAndFileStructure();
        Assert.True(searchedDirectory.ChildFiles.Count == 1);
        Assert.NotNull(searchedDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "testFile.txt"));
        Assert.Empty(searchedDirectory.ChildDirectories);
    }

    [Fact]
    public void GetDirectoryWithExclude()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch(path, ["/Icons"], includePaths: false);
        var searchedDirectory = fileSearch.GetDirectoryAndFileStructure();
        Assert.True(searchedDirectory.ChildFiles.Count == 2);
        Assert.NotNull(searchedDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "testFile.txt"));
        Assert.NotNull(searchedDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "cryptoForestLogo.png"));
        Assert.Empty(searchedDirectory.ChildDirectories);
    }

    [Fact]
    public void GetFiles()
    {
        var path = $"{AppDomain.CurrentDomain.BaseDirectory}/Resources";
        var fileSearch = new FileSearch([$"{path}/testFile.txt", $"{path}/Icons/data_icon.png"]);
        var searchedDirectory = fileSearch.GetDirectoryAndFileStructure();
        Assert.True(searchedDirectory.ChildFiles.Count == 2);
        Assert.NotNull(searchedDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "testFile.txt"));
        Assert.NotNull(searchedDirectory.ChildFiles.SingleOrDefault(f => f.FileName == "data_icon.png"));
        Assert.Empty(searchedDirectory.ChildDirectories);
    }
}

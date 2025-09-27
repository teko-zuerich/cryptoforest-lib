using CryptoForestLibrary.Tests.Fixture;

namespace CryptoForestLibrary.Tests;
public class LevelConfigTests : IClassFixture<CryptoForestFixture>
{
    private CryptoForestFixture _fixture;

    public LevelConfigTests(CryptoForestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GetLevels()
    {
        var baseLevel = _fixture.CryptoForest.GetBaseLevel();
        var levels = baseLevel.GetLevels();
        Assert.True(levels.Count == 1);
        Assert.True(levels.ContainsKey("TestLevel"));
    }

    [Fact]
    public void GetLevel()
    {
        var baseLevel = _fixture.CryptoForest.GetBaseLevel();
        var levelGuid = baseLevel.GetLevels().First().Value.EntryGuid;
        var level = baseLevel.GetLevel(levelGuid);
        Assert.Equal(levelGuid, level.EntryGuid);
    }

    [Fact]
    public void GetItems()
    {
        var baseLevel = _fixture.CryptoForest.GetBaseLevel();
        var items = baseLevel.GetItems();
        Assert.True(items.Count == 3);
        Assert.Contains(items, i => i.Key == "TestItem1");
        Assert.Contains(items, i => i.Key == "TestItem2");
        Assert.Contains(items, i => i.Key == "TestItem3");
    }

    [Fact]
    public void HasItemTrue()
    {
        var baseLevel = _fixture.CryptoForest.GetBaseLevel();
        var itemGuid = baseLevel.GetItems().First().Value.EntryGuid;
        Assert.True(baseLevel.HasItem(itemGuid));
    }

    [Fact]
    public void HasItemFalse()
    {
        var baseLevel = _fixture.CryptoForest.GetBaseLevel();
        var itemGuid = Guid.Empty;
        Assert.False(baseLevel.HasItem(itemGuid));
    }

    [Fact]
    public void GetItem()
    {
        var baseLevel = _fixture.CryptoForest.GetBaseLevel();
        var itemGuid = baseLevel.GetItems().First().Value.EntryGuid;
        var item = baseLevel.GetItem(itemGuid);
        Assert.Equal(itemGuid, item.EntryGuid);
    }

    [Fact]
    public void SearchItems()
    {
        var baseLevel = _fixture.CryptoForest.GetBaseLevel();
        var search = baseLevel.SearchItems("TestI").ToArray();
        Assert.True(search.Length == 5);
        Assert.Contains(search, s => s.Key == "TestItem1");
        Assert.Contains(search, s => s.Key == "TestItem2");
        Assert.Contains(search, s => s.Key == "TestItem3");
        Assert.Contains(search, s => s.Key == "TestItem4");
        Assert.Contains(search, s => s.Key == "TestItem5");
    }
}

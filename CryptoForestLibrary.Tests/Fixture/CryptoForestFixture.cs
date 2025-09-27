using CryptoForestLibrary.Cryptograph.Storage;

namespace CryptoForestLibrary.Tests.Fixture;
public class CryptoForestFixture : IDisposable
{
    public CryptoForestMemoryStorage Storage { get; private set; }
    public AesCryptoForest CryptoForest { get; private set; }

    public CryptoForestFixture()
    {
        Storage = new CryptoForestMemoryStorage();
        CryptoForest = AesCryptoForest.CreateCryptoForest(Storage);
        PrepareStructure().GetAwaiter().GetResult();
    }

    private async Task PrepareStructure()
    {
        var baseLevelGuid = CryptoForest.GetBaseLevel().EntryGuid;
        var levelGuid = await CryptoForest.AddLevelAsync("TestLevel", baseLevelGuid);
        _ = await CryptoForest.AddItemAsync("TestItem1", "TestItem1", baseLevelGuid);
        _ = await CryptoForest.AddItemAsync("TestItem2", "TestItem2", baseLevelGuid);
        _ = await CryptoForest.AddItemAsync("TestItem3", "TestItem3", baseLevelGuid);
        _ = await CryptoForest.AddItemAsync("TestItem4", "TestItem4", levelGuid);
        _ = await CryptoForest.AddItemAsync("TestItem5", "TestItem5", levelGuid);
    }

    public void Dispose()
        => Storage.Dispose();
}

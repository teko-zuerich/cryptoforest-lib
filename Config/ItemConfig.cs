namespace CryptoForestLibrary.Config;

/// <summary>
/// Used to store item values in an immutable config
/// </summary>
public class ItemConfig
{
    public Guid EntryGuid { get; }

    public KeyIV KeyIV { get; }

    public ItemType ItemType { get; }

    public ItemConfig(Guid entryGuid, KeyIV keyIV, ItemType itemType)
    {
        EntryGuid = entryGuid;
        KeyIV = keyIV;
        ItemType = itemType;
    }
}

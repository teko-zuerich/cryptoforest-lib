namespace CryptoForestLibrary.Config;

/// <summary>
/// Used to store item values in an immutable config
/// </summary>
public class ItemConfig
{
    public Guid EntryGuid { get; set; }

    public KeyIV KeyIV { get; set; }

    public ItemType ItemType { get; set; }

    public ItemConfig(Guid entryGuid, KeyIV keyIV, ItemType itemType)
    {
        EntryGuid = entryGuid;
        KeyIV = keyIV;
        ItemType = itemType;
    }

    public ItemConfig() { }
}

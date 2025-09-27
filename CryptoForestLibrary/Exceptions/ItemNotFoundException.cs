namespace CryptoForestLibrary.Exceptions;

/// <summary>
/// Used when an item cannot be found in any level
/// </summary>
public class ItemNotFoundException : Exception
{
    internal ItemNotFoundException(Guid itemGuid) : base($"Item with GUID {itemGuid} does not exist")
    { }
}

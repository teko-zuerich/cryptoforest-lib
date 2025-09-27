namespace CryptoForestLibrary.Exceptions;

/// <summary>
/// Used when an item already exist with the same key in the same level
/// </summary>
public class ItemAlreadyExistsException : Exception
{
    internal ItemAlreadyExistsException(string itemKey) : base($"Item with key {itemKey} already exists")
    { }
}

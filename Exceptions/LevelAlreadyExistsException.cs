namespace CryptoForestLibrary.Exceptions;

/// <summary>
/// Used when a level already exists with the same key in the same parent level
/// </summary>
public class LevelAlreadyExistsException : Exception
{
    internal LevelAlreadyExistsException(string levelKey) : base($"Level with key {levelKey} already exists")
    { }
}

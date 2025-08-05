namespace CryptoForestLibrary;

/// <summary>
/// Used when a level cannot be found
/// </summary>
public class LevelNotFoundException : Exception
{
    internal LevelNotFoundException(Guid levelGuid) : base($"Level with GUID {levelGuid} does not exist")
    { }
}

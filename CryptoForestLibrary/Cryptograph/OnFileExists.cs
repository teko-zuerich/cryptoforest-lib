namespace CryptoForestLibrary.Cryptograph;

/// <summary>
/// Used to define what should happen if the file already exists during decryption
/// </summary>
public enum OnFileExists
{
    Throw = 0,
    Replace = 1,
    Skip = 2
}

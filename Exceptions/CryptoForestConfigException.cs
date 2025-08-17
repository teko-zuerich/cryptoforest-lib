namespace CryptoForestLibrary.Exceptions;

/// <summary>
/// Used when decrypting / encrypting a CryptoForestConfig and it fails
/// </summary>
public class CryptoForestConfigException : Exception
{
    internal CryptoForestConfigException(bool encrypting) : base($"The config could not be {(encrypting ? "encrypted" : "decrypted")}.{(encrypting ? string.Empty : " Either the key was wrong or the file cannot be accessed.")}")
    { }
}

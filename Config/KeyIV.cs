namespace CryptoForestLibrary.Config;

/// <summary>
/// Used to hold an immutable Key and IV
/// </summary>
public class KeyIV
{
    public byte[] Key { get; }

    public byte[] IV { get; }

    public KeyIV(byte[] key, byte[] iv)
    {
        Key = key;
        IV = iv;
    }

    /// <summary>
    /// Checks whether or not the Key and IV is valid or not.
    /// The length must be specified in bytes so 256 bits would be 32 bytes
    /// </summary>
    /// <param name="expectedLength">The expected byte length</param>
    /// <returns>Returns the result of the check</returns>
    public bool Validate(int expectedLength)
        => Key.Length == expectedLength && IV.Length == expectedLength;
}

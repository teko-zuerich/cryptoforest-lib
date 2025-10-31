using CryptoForestLibrary.Config;
using System.Security.Cryptography;

namespace CryptoForestLibrary.Cryptograph.Algorithm;

/// <summary>
/// The AES256 implementation for the CryptoForest
/// </summary>
public class CryptoForestAesAlgorithm : ICryptoForestAlgorithm
{
    /// <summary>
    /// Creates a CryptoStream with AES256 algorithm from the storageStream to encrypt data in the passed writeToStreamAsync function
    /// </summary>
    /// <param name="keyIV">The key and IV used by AES256</param>
    /// <param name="storageStream">The storage stream from the ICryptoForestStorage</param>
    /// <param name="writeToStreamAsync">The function passed where the CryptoStream can be used to encrypt</param>
    /// <exception cref="ArgumentException">Thrown when the KeyIV is not a valid 256 bit key and IV</exception>
    public async Task EncryptToStreamAsync(KeyIV keyIV, Stream storageStream, Func<CryptoStream, CancellationToken, Task> writeToStreamAsync, CancellationToken cancellationToken)
    {
        if (!keyIV.Validate(32))
        {
            throw new ArgumentException("The key or IV is not valid or not secure enough", nameof(keyIV));
        }

        using var aes = Aes.Create();
        using var encryptor = aes.CreateEncryptor(keyIV.Key, keyIV.IV);
        using var cryptoStream = new CryptoStream(storageStream, encryptor, CryptoStreamMode.Write);
        await writeToStreamAsync(cryptoStream, cancellationToken);
        await cryptoStream.FlushFinalBlockAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a CryptoStream with AES256 algorithm from the storageStream to decrypt data in the passed readFromStreamAsync function
    /// </summary>
    /// <param name="keyIV">The key and IV used by AES256</param>
    /// <param name="storageStream">The storage stream from the ICryptoForestStorage</param>
    /// <param name="readFromStreamAsync">The function passed where the CryptoStream can be used to decrypt</param>
    /// <exception cref="ArgumentException">Thrown when the KeyIV is not a valid 256 bit key and IV</exception>
    public async Task<T> DecryptFromStreamAsync<T>(KeyIV keyIV, Stream storageStream, Func<CryptoStream, CancellationToken, Task<T>> readFromStreamAsync, CancellationToken cancellationToken)
    {
        if (!keyIV.Validate(32))
        {
            throw new ArgumentException("The key or IV is not valid or not secure enough", nameof(keyIV));
        }

        using var aes = Aes.Create();
        using var decryptor = aes.CreateDecryptor(keyIV.Key, keyIV.IV);
        using var cryptoStream = new CryptoStream(storageStream, decryptor, CryptoStreamMode.Read);
        return await readFromStreamAsync(cryptoStream, cancellationToken);
    }

    /// <summary>
    /// Generates a random key and IV from the AES algorithm
    /// </summary>
    /// <returns>Returns the generated KeyIV object</returns>
    public KeyIV GenerateKeyIV()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();

        return new KeyIV(aes.Key, aes.IV);
    }
}

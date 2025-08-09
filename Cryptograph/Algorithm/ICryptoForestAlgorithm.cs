using CryptoForestLibrary.Config;
using System.Security.Cryptography;

namespace CryptoForestLibrary.Cryptograph.Algorithm;

/// <summary>
/// The interface used to define an algorithm implementation for the CryptoForest
/// </summary>
public interface ICryptoForestAlgorithm
{
    /// <summary>
    /// Creates a CryptoStream from the storageStream to encrypt data in the passed writeToStreamAsync function
    /// </summary>
    /// <param name="keyIV">The key and IV used during the encryption</param>
    /// <param name="storageStream">The writablee storage stream from the ICryptoForestStorage used as the underlying stream</param>
    /// <param name="writeToStreamAsync">The function passed where the CryptoStream can be used to encrypt</param>
    public Task EncryptToStreamAsync(KeyIV keyIV, Stream storageStream, Func<CryptoStream, CancellationToken, Task> writeToStreamAsync, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a CryptoStream from the storageStream to decrypt data in the passed readFromStreamAsync function
    /// </summary>
    /// <param name="keyIV">The key and IV used during the decryption</param>
    /// <param name="storageStream">The readonly storage stream from the ICryptoForestStorage used as the underlying stream</param>
    /// <param name="readFromStreamAsync">The function passed where the CryptoStream can be used to decrypt</param>
    public Task DecryptFromStreamAsync(KeyIV keyIV, Stream storageStream, Func<CryptoStream, CancellationToken, Task> readFromStreamAsync, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a random secure Key and IV from the algorithm
    /// </summary>
    /// <returns>Returns the generated KeyIV object</returns>
    public KeyIV GenerateKeyIV();
}

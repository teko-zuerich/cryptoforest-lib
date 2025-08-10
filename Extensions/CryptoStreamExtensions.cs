using System.Security.Cryptography;

namespace CryptoForestLibrary.Extensions;
internal static class CryptoStreamExtensions
{
    /// <summary>
    /// Encrypts a whole file into the CryptoStream.
    /// To avoid OOM a buffer is used reading and writing 10Mb of data at a time.
    /// </summary>
    /// <param name="readStream">The read stream of the file to encrypt</param>
    internal static async Task EncryptFileAsync(this CryptoStream cryptoStream, Stream readStream, CancellationToken cancellationToken = default)
    {
        var currentFileData = new byte[10485760];
        var currentlyRead = await readStream.ReadAsync(currentFileData, cancellationToken);
        while (currentlyRead > 0)
        {
            await cryptoStream.WriteAsync(currentFileData, cancellationToken);
            currentlyRead = await readStream.ReadAsync(currentFileData, cancellationToken);
        }
    }
}

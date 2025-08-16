using System.Security.Cryptography;

namespace CryptoForestLibrary.Extensions;
internal static class CryptoStreamExtensions
{
    /// <summary>
    /// Encrypts a whole file into the CryptoStream.
    /// To avoid OOM a buffer is used reading and writing 10Mb of data at a time.
    /// </summary>
    /// <param name="readStream">The read stream of the file to encrypt</param>
    internal static async Task EncryptFileAsync(this CryptoStream cryptoStream, Stream readStream, CancellationToken cancellationToken)
    {
        var currentFileData = new byte[10485760];
        var currentlyRead = await readStream.ReadAsync(currentFileData, cancellationToken);
        while (currentlyRead > 0)
        {
            await cryptoStream.WriteAsync(currentFileData, cancellationToken);
            currentlyRead = await readStream.ReadAsync(currentFileData, cancellationToken);
        }
    }

    /// <summary>
    /// Decrypts a whole file from the CryptoStream.
    /// To avoid OOM a buffer is used reading and writing 10Mb of data at a time.
    /// </summary>
    /// <param name="writeStream">The write stream where the decrypted data is written to</param>
    /// <param name="fileLength">The length of the file to decrypt</param>
    internal static async Task DecryptFileAsync(this CryptoStream cryptoStream, Stream writeStream, long fileLength, CancellationToken cancellationToken)
    {
        var remainingFileLength = fileLength;
        var currentFileData = new byte[remainingFileLength < 10485760 ? remainingFileLength : 10485760];
        var currentlyRead = await cryptoStream.ReadAsync(currentFileData, cancellationToken);
        while (currentlyRead > 0)
        {
            await writeStream.WriteAsync(currentFileData, cancellationToken);
            remainingFileLength -= currentlyRead;
            if (remainingFileLength < 10485760)
            {
                currentFileData = new byte[remainingFileLength];
            }

            currentlyRead = remainingFileLength == 0 ? 0 : await cryptoStream.ReadAsync(currentFileData, cancellationToken);
        }

        await writeStream.FlushAsync();
    }

    /// <summary>
    /// Skips a whole file in the CryptoStream by reading the data beloning to it without writing it.
    /// </summary>
    /// <param name="fileLength">>The length of the file to skip</param>
    internal static async Task SkipFileAsnyc(this CryptoStream cryptoStream, long fileLength, CancellationToken cancellationToken)
    {
        var remainingFileLength = fileLength;
        var currentFileData = new byte[remainingFileLength < 10485760 ? remainingFileLength : 10485760];
        var currentlyRead = await cryptoStream.ReadAsync(currentFileData, cancellationToken);
        while (currentlyRead > 0)
        {
            remainingFileLength -= currentlyRead;
            if (remainingFileLength < 10485760)
            {
                currentFileData = new byte[remainingFileLength];
            }

            currentlyRead = remainingFileLength == 0 ? 0 : await cryptoStream.ReadAsync(currentFileData, cancellationToken);
        }
    }
}

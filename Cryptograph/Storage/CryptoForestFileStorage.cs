namespace CryptoForestLibrary.Cryptograph.Storage;

/// <summary>
/// The file system implementation for the CryptoForest
/// </summary>
public class CryptoForestFileStorage : ICryptoForestStorage
{
    private readonly string _cryptoForestDirectory;

    /// <summary>
    /// Creates a storage handler for the file system
    /// </summary>
    /// <param name="cryptoForestDirectory">The directory where the CryptoForest lies</param>
    /// <exception cref="ArgumentException">Thrown when the directory passed is not valid, not accessible or doesn't exist</exception>
    public CryptoForestFileStorage(string cryptoForestDirectory)
    {
        if (!Directory.Exists(cryptoForestDirectory))
        {
            throw new ArgumentException("The directory path is invalid, not accessible or doesn't exist", nameof(cryptoForestDirectory));
        }

        // Sets the _cryptoForestDirectory and removes / or \ at the end of the path
        _cryptoForestDirectory = cryptoForestDirectory.EndsWith('/') || cryptoForestDirectory.EndsWith('\\') ? cryptoForestDirectory[..^1] : cryptoForestDirectory;
    }

    /// <summary>
    /// Creates a FileStream for reading or writing an entry in the CryptoForest
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry to create or read</param>
    /// <param name="asReadonly">Specifies whether or not the stream is reading or creating a file</param>
    /// <returns>Returns the FileStream for the specified GUID</returns>
    /// <exception cref="ArgumentException">Throws when the entry GUID that should be read doesn't exist</exception>
    public Stream GetStream(Guid entryGuid, bool asReadonly)
    {
        var entryPath = $"{_cryptoForestDirectory}/{entryGuid}";
        if (asReadonly)
        {
            if (!File.Exists(entryPath))
            {
                throw new ArgumentException($"Cannot read the entry {entryGuid} as it doesn't exist", nameof(entryGuid));
            }

            return new FileStream(entryPath, FileMode.Open, FileAccess.Read);
        }

        return new FileStream(entryPath, FileMode.Create, FileAccess.Write);
    }

    public Task FinalizeAsync(Stream storageStream, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Checks if a file with this GUID already exists.
    /// </summary>
    /// <param name="entryGuid">The entry GUID to check</param>
    /// <returns>Returns if the GUID already exists</returns>
    public bool EntryExists(Guid entryGuid)
        => File.Exists($"{_cryptoForestDirectory}/{entryGuid}");
}

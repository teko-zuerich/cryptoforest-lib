using CryptoForestLibrary.Exceptions;

namespace CryptoForestLibrary.Cryptograph.Storage;

/// <summary>
/// The memory implementation for the CryptoForest used for testing
/// </summary>
public class CryptoForestMemoryStorage : ICryptoForestStorage
{
    public Dictionary<Guid, byte[]> _storage = new();

    /// <summary>
    /// Creates a MemoryStream for reading or writing an entry in the CryptoForest
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry to create or read</param>
    /// <param name="asReadonly">Specifies whether or not the stream is reading or creating a file<</param>
    /// <returns>Returns the MemoryStream fro the specified GUID</returns>
    /// <exception cref="ItemNotFoundException">Thrown when the entry GUID does not exist in the storage dictionary</exception>
    public Stream GetStream(Guid entryGuid, bool asReadonly)
    {
        if (asReadonly)
        {
            if (!_storage.TryGetValue(entryGuid, out var data))
            {
                throw new ItemNotFoundException(entryGuid);
            }

            return new MemoryStream(data, writable: false);
        }

        return new MemoryStream();
    }

    /// <summary>
    /// Adds the data and the GUID into the dictionary
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry that was encrypted</param>
    /// <param name="storageStream">The MemoryStream where the data was encrypted to</param>
    public Task FinalizeAsync(Guid entryGuid, Stream storageStream, CancellationToken cancellationToken)
    {
        var memoryStream = (MemoryStream)storageStream;
        _storage.Add(entryGuid, memoryStream.ToArray());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the GUID exists in the dictionary
    /// </summary>
    /// <param name="entryGuid">The GUID to check for</param>
    /// <returns>Returns wheter or not the GUID exists in the storage dictionary</returns>
    public bool EntryExists(Guid entryGuid)
        => _storage.ContainsKey(entryGuid);

    /// <summary>
    /// Removes an entry from the dictionary
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry to remove</param>
    /// <exception cref="ItemNotFoundException">Thrown when no entry with this GUID exists</exception>
    public Task RemoveEntryAsync(Guid entryGuid, CancellationToken cancellationToken)
    {
        if (!_storage.ContainsKey(entryGuid))
        {
            throw new ItemNotFoundException(entryGuid);
        }

        _storage.Remove(entryGuid);

        return Task.CompletedTask;
    }
}

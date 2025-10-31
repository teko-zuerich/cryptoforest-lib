namespace CryptoForestLibrary.Cryptograph.Storage;

/// <summary>
/// The interface used to define a storage system implementation for the CryptoForest
/// </summary>
public interface ICryptoForestStorage
{
    /// <summary>
    /// Creates the storage stream used in the CryptoForestCryptograph
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry this stream belongs to</param>
    /// <param name="asReadonly">If the Stream should be created as readonly or if it should be writable</param>
    /// <returns>Returns the stream for the entry</returns>
    public Stream GetStream(Guid entryGuid, bool asReadonly);

    /// <summary>
    /// Called after the encrpytions are finished in the CryptoForestCryptograph.
    /// Can be used to define actions for the storage after the encryptions have finished.
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry the stream belongs to</param>
    /// <param name="storageStream">The current storage stream</param>
    public Task FinalizeAsync(Guid entryGuid, Stream storageStream, CancellationToken cancellationToken);

    /// <summary>
    /// Called to determine if an entry already exists in the storage before creating it.
    /// When it already exists a new GUID is generated until a GUID that doesn't exist yet is found.
    /// </summary>
    /// <param name="entryGuid">The entry GUID to check</param>
    /// <returns>Returns if the entry GUID already exists or not</returns>
    public bool EntryExists(Guid entryGuid);

    /// <summary>
    /// Removes an entry from the storage.
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry to remove</param>
    public Task RemoveEntryAsync(Guid entryGuid, CancellationToken cancellationToken);
}

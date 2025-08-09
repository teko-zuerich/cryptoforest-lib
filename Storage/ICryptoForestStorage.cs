namespace CryptoForestLibrary.Storage;

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
    /// Called after the operations are finished in the CryptoForestCryptograph.
    /// Can be used to define actions for the storage after the operations have finished.
    /// </summary>
    /// <param name="storageStream">The current storage stream</param>
    public Task FinalizeAsync(Stream storageStream, CancellationToken cancellationToken = default);
}

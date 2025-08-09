namespace CryptoForestLibrary.DirectoryStructure;

/// <summary>
/// Used for storing the directory of a directory structure
/// </summary>
public class SearchedFile
{
    public long FileId { get; init; }

    public string FileName { get; init; }

    public long FileDataLength { get; init; }

    public byte[]? FileData { get; init; }

    internal SearchedFile(long fileId, string fileName, long fileDataLength)
    {
        FileId = fileId;
        FileName = fileName;
        FileDataLength = fileDataLength;
    }

    internal SearchedFile(long fileId, string fileName, long fileDataLength, byte[] fileData) : this(fileId, fileName, fileDataLength)
    {
        FileData = fileData;
    }
}

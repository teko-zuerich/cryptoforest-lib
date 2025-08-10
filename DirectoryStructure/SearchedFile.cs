namespace CryptoForestLibrary.DirectoryStructure;

/// <summary>
/// Used for storing the directory of a directory structure
/// </summary>
public class SearchedFile
{
    public string FileName { get; init; }

    public long FileDataLength { get; init; }

    public byte[]? FileData { get; init; }

    internal SearchedFile(string fileName, long fileDataLength)
    {
        FileName = fileName;
        FileDataLength = fileDataLength;
    }

    internal SearchedFile(string fileName, long fileDataLength, byte[] fileData) : this(fileName, fileDataLength)
    {
        FileData = fileData;
    }
}

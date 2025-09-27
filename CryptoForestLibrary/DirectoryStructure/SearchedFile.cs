using System.Text.Json.Serialization;

namespace CryptoForestLibrary.DirectoryStructure;

/// <summary>
/// Used for storing the directory of a directory structure
/// </summary>
public class SearchedFile
{
    public string FileName { get; init; }

    [JsonIgnore]
    public string Path { get; init; }

    public long FileDataLength { get; init; }

    public byte[]? FileData { get; set; }

    internal SearchedFile(string fileName, long fileDataLength, string path)
    {
        FileName = fileName;
        FileDataLength = fileDataLength;
        Path = path;
    }

    public SearchedFile() : this(string.Empty, 0, string.Empty) { }
}

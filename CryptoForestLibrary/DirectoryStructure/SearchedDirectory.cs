namespace CryptoForestLibrary.DirectoryStructure;

/// <summary>
/// Used for storing the directory of a directory structure
/// </summary>
public class SearchedDirectory
{
    public string DirectoryName { get; init; }

    public List<SearchedDirectory> ChildDirectories { get; init; }

    public List<SearchedFile> ChildFiles { get; init; }

    internal SearchedDirectory(string directoryName)
    {
        DirectoryName = directoryName;
        ChildDirectories = [];
        ChildFiles = [];
    }

    public SearchedDirectory() : this(string.Empty) { }
}

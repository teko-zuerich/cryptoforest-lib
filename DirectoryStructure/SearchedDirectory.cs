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

    /// <summary>
    /// Returns an absolute path for the file with the specified id if found
    /// </summary>
    /// <param name="directoryPath">The directory path this file should be in</param>
    /// <param name="fileId">The file id to search for</param>
    /// <param name="currentPath">Current searched directory path within the structure. Should not be set as it is set in the recursion.</param>
    /// <returns>Returns the path of the file or null if not found</returns>
    public string? GetFilePathById(string directoryPath, long fileId, string currentPath = "")
    {
        if (ChildFiles.Any(f => f.FileId == fileId))
        {
            return directoryPath + (currentPath != string.Empty && !directoryPath.EndsWith('/') && !directoryPath.EndsWith('\\') ? "/" : "") + currentPath + $"/{ChildFiles.First(f => f.FileId == fileId).FileName}";
        }

        foreach (var directory in ChildDirectories)
        {
            var filePath = directory.GetFilePathById(directoryPath, fileId, currentPath + (currentPath != string.Empty ? "/" : "") + directory.DirectoryName);
            if (filePath != null)
            {
                return filePath;
            }
        }

        return null;
    }
}

namespace CryptoForestLibrary.DirectoryStructure;

/// <summary>
/// FileSearch can be used to get the directory structure with defined search parameters to specify further what should be included or excluded
/// </summary>
public class FileSearch
{
    public string SearchedDirectory { get; init; }

    public IEnumerable<string> SearchedPaths { get; init; }

    public bool IncludePaths { get; init; }

    /// <summary>
    /// Creates a FileSearch which can search the whole directory structure of the specified directory
    /// </summary>
    /// <param name="searchedDirectory">The directory to search</param>
    public FileSearch(string searchedDirectory)
    {
        SearchedDirectory = searchedDirectory;
        SearchedPaths = [];
    }

    /// <summary>
    /// Creates a FileSearch with a dynamic configuration on what will be searched
    /// </summary>
    /// <param name="searchedDirectory">The directory to search</param>
    /// <param name="searchedPaths">The paths to include or exclude</param>
    /// <param name="includePaths">Specifies if the paths should be used to specifically include files or to exclude certain files and directories</param>
    public FileSearch(string searchedDirectory, IEnumerable<string> searchedPaths, bool includePaths)
    {
        SearchedDirectory = searchedDirectory;
        SearchedPaths = searchedPaths;
        IncludePaths = includePaths;
    }

    /// <summary>
    /// Gets the directory and file structure based on the configuration provided in the properties.
    /// </summary>
    /// <returns>Returns the found directory structure</returns>
    public SearchedDirectory GetDirectoryAndFileStructure()
    {
        var searchedDirectory = new SearchedDirectory(SearchedDirectory);
        GetStructure(searchedDirectory);

        return searchedDirectory;
    }

    /// <summary>
    /// This method gets the files and directories recousively based on the configuration provided in the properties of FileSearch.
    /// As it can be confusing on how the parameters of this method are used it was set as private and GetDirectoryAndFileStructure using it should be used instead.
    /// </summary>
    /// <param name="searchedDirectory">The main SearchedDirectory which will contain the whole structure in the end</param>
    /// <param name="currentPath">The current path that is searched. Should not be set as it will be used in the recursion.</param>
    /// <param name="fileId">The current fileId counter value. Should not be set as it will be used in the recursion.</param>
    /// <returns>Returns the current fileId counter value</returns>
    private long GetStructure(SearchedDirectory searchedDirectory, string currentPath = "", long fileId = 0)
    {
        var fullPath = SearchedDirectory + (currentPath != string.Empty && !SearchedDirectory.EndsWith('/') && !SearchedDirectory.EndsWith('\\') ? "/" : "") + currentPath;
        if (!IncludePaths)
        {
            var files = Directory.GetFiles(fullPath).Where(f => !SearchedPaths.Contains(f));
            var directories = Directory.GetDirectories(fullPath).Where(d => !SearchedPaths.Contains(d));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                searchedDirectory.ChildFiles.Add(new SearchedFile(fileId++, fileInfo.Name, fileInfo.Length));
            }

            foreach (var directory in directories)
            {
                var searchedSubDirectory = new SearchedDirectory(new DirectoryInfo(directory).Name);
                searchedDirectory.ChildDirectories.Add(searchedSubDirectory);
                // Recursively calls GetStructure for each subdirectory and updates fileId to avoid having multiple files with the same id as the id should be unique for the whole structure
                fileId = GetStructure(searchedDirectory, currentPath + (currentPath != string.Empty ? "/" : "") + searchedSubDirectory.DirectoryName, fileId);
            }
        }
        else
        {
            foreach (var searchPath in SearchedPaths)
            {
                var existingPath = File.Exists(searchPath) ? searchPath : File.Exists(fullPath + searchPath) ? fullPath + searchPath : null;
                if (existingPath != null)
                {
                    var fileInfo = new FileInfo(existingPath);
                    searchedDirectory.ChildFiles.Add(new SearchedFile(fileId, fileInfo.Name, fileInfo.Length));
                }
            }
        }

        return fileId;
    }
}

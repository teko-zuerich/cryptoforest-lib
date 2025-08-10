namespace CryptoForestLibrary.DirectoryStructure;

/// <summary>
/// FileSearch can be used to get the directory structure with defined search parameters to specify further what should be included or excluded
/// </summary>
public class FileSearch
{
    public string SearchedDirectory { get; init; }

    public IEnumerable<string> SearchedPaths { get; init; }

    public bool IncludePaths { get; init; }

    public bool SpecificFiles { get; init; }

    /// <summary>
    /// Creates a FileSearch which can search the whole directory structure of the specified directory
    /// </summary>
    /// <param name="searchedDirectory">The directory to search in</param>
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
    /// <param name="includePaths">Specifies if the paths should be used to include or to exclude certain files and directories if the searched path is part of the path</param>
    /// <param name="specificFiles">Specifies if the files in the searchedPath should be treated as specific files to include in a flattened structure</param>
    public FileSearch(string searchedDirectory, IEnumerable<string> searchedPaths, bool includePaths, bool specificFiles)
    {
        SearchedDirectory = searchedDirectory;
        SearchedPaths = searchedPaths;
        IncludePaths = includePaths;
        SpecificFiles = specificFiles;
    }

    /// <summary>
    /// Gets the directory and file structure based on the configuration provided in the properties.
    /// </summary>
    /// <returns>Returns the found directory structure</returns>
    public SearchedDirectory GetDirectoryAndFileStructure()
    {
        var searchedDirectory = new SearchedDirectory(string.Empty);
        GetStructure(searchedDirectory);

        return searchedDirectory;
    }

    /// <summary>
    /// This method gets the files and directories recousively based on the configuration provided in the properties of FileSearch.
    /// As it can be confusing on how the parameters of this method are used it was set as private and GetDirectoryAndFileStructure using it should be used instead.
    /// </summary>
    /// <param name="searchedDirectory">The main SearchedDirectory which will contain the whole structure in the end</param>
    /// <param name="currentPath">The current path that is searched. Should not be set as it will be used in the recursion.</param>
    private void GetStructure(SearchedDirectory searchedDirectory, string currentPath = "")
    {
        var fullPath = SearchedDirectory + (currentPath != string.Empty && !SearchedDirectory.EndsWith('/') && !SearchedDirectory.EndsWith('\\') ? "/" : "") + currentPath;
        if (!SpecificFiles)
        {
            var files = Directory.GetFiles(fullPath).Where(f => IncludePaths ? SearchedPaths.Contains(f) : !SearchedPaths.Contains(f));
            var directories = Directory.GetDirectories(fullPath).Where(d => IncludePaths ? SearchedPaths.Contains(d) : !SearchedPaths.Contains(d));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                searchedDirectory.ChildFiles.Add(new SearchedFile(fileInfo.Name, fileInfo.Length));
            }

            foreach (var directory in directories)
            {
                var searchedSubDirectory = new SearchedDirectory(new DirectoryInfo(directory).Name);
                searchedDirectory.ChildDirectories.Add(searchedSubDirectory);
                GetStructure(searchedDirectory, currentPath + (currentPath != string.Empty ? "/" : "") + searchedSubDirectory.DirectoryName);
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
                    searchedDirectory.ChildFiles.Add(new SearchedFile(fileInfo.Name, fileInfo.Length));
                }
            }
        }
    }
}

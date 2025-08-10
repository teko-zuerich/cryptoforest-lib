using CryptoForestLibrary.Config;
using CryptoForestLibrary.Cryptograph.Algorithm;
using CryptoForestLibrary.Cryptograph.Storage;
using CryptoForestLibrary.DirectoryStructure;
using CryptoForestLibrary.Extensions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CryptoForestLibrary.Cryptograph;

/// <summary>
/// The generic cryptograph used in the CryptoForest
/// </summary>
/// <typeparam name="T">The implementation type of the algorithm to use in the cryptograph</typeparam>
internal class CryptoForestCryptograph<T>
    where T : ICryptoForestAlgorithm, new()
{
    private readonly ICryptoForestAlgorithm _algorithm;
    private readonly ICryptoForestStorage _storage;

    /// <summary>
    /// Creates a CryptoForestCryptograph with a defined storage and a new instance of the algorithm T
    /// </summary>
    /// <param name="storage">The storage to be used in the cryptograph</param>
    internal CryptoForestCryptograph(ICryptoForestStorage storage)
    {
        _storage = storage;
        _algorithm = new T();
    }

    /// <summary>
    /// Encrypts the text into the CryptoForest storage with the defined algorithm
    /// </summary>
    /// <param name="text">The text to encrypt</param>
    /// <param name="entryGuid">The GUID of the new entry in the CryptoForest</param>
    /// <returns>Returns the generated Key and IV</returns>
    /// <exception cref="ArgumentException">Thrown when the text is empty or null</exception>
    internal async Task<KeyIV> EncryptTextAsync(string text, Guid entryGuid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("The text cannot be empty or null", nameof(text));
        }

        using var storageStream = _storage.GetStream(entryGuid, asReadonly: false);
        var keyIV = _algorithm.GenerateKeyIV();
        await _algorithm.EncryptToStreamAsync(keyIV, storageStream, EncryptText, cancellationToken);
        await _storage.FinalizeAsync(storageStream, cancellationToken);

        return keyIV;

        async Task EncryptText(CryptoStream cryptoStream, CancellationToken cancellationToken)
        {
            var textData = Encoding.UTF8.GetBytes(text);
            await cryptoStream.WriteAsync(textData, cancellationToken);
        }
    }

    /// <summary>
    /// Encrypts the files into the CryptoForest storage with the defined algorithm as one combined file
    /// </summary>
    /// <param name="fileSearch">The definition to search for the files to encrypt</param>
    /// <param name="entryGuid">The GUID of the new entry in the CryptoForest</param>
    /// <returns>Returns the generated Key and IV</returns>
    internal async Task<KeyIV> EncryptFilesAsync(FileSearch fileSearch, Guid entryGuid, CancellationToken cancellationToken = default)
    {
        using var storageStream = _storage.GetStream(entryGuid, asReadonly: false);
        var keyIV = _algorithm.GenerateKeyIV();
        await _algorithm.EncryptToStreamAsync(keyIV, storageStream, EncryptFiles, cancellationToken);
        await _storage.FinalizeAsync(storageStream, cancellationToken);

        return keyIV;

        async Task EncryptFiles(CryptoStream cryptoStream, CancellationToken cancellationToken)
        {
            // Get directory structure
            var directoryStructure = fileSearch.GetDirectoryAndFileStructure();

            // Encrypt directory structure json
            var directoryStructureJson = JsonSerializer.Serialize(directoryStructure);
            var directoryStructureData = Encoding.UTF8.GetBytes(directoryStructureJson);
            var header = Encoding.ASCII.GetBytes(directoryStructureData.Length.ToString());
            var headerBytes = new byte[8]; // 8 Bytes allow for a data length of about 95Mb which should be more than enough
            Array.Copy(header, headerBytes, header.Length);
            await cryptoStream.WriteAsync(directoryStructureData, cancellationToken);

            // Encrypt directory structure recusively
            await EncryptDirectoryStructureAsync(directoryStructure, cryptoStream, cancellationToken: cancellationToken);
            async Task EncryptDirectoryStructureAsync(SearchedDirectory directoryStructure, CryptoStream cryptoStream, string currentPath = "", CancellationToken cancellationToken = default)
            {
                var fullPath = fileSearch.SearchedDirectory + (currentPath != string.Empty && !fileSearch.SearchedDirectory.EndsWith('/') && !fileSearch.SearchedDirectory.EndsWith('\\') ? "/" : "") + currentPath;
                foreach (var file in directoryStructure.ChildFiles)
                {
                    using var readStream = new FileStream($"{fullPath}/{file.FileName}", FileMode.Open, FileAccess.Read);
                    await cryptoStream.EncryptFileAsync(readStream, cancellationToken);
                }
                foreach (var directory in directoryStructure.ChildDirectories)
                {
                    await EncryptDirectoryStructureAsync(directory, cryptoStream, currentPath + (currentPath != string.Empty ? "/" : "") + directory.DirectoryName, cancellationToken);
                }
            }
        }
    }
}

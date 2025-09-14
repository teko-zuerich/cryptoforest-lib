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
    /// Encrypts and exports a config.
    /// </summary>
    /// <param name="config">The config to export</param>
    /// <param name="key">The key used to export the config</param>
    /// <param name="filePath">The file path to export the config to</param>
    internal async Task EncryptConfigAsync(CryptoForestConfig config, byte[] key, string filePath, CancellationToken cancellationToken)
    {
        using var storageStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        var keyIV = new KeyIV(key, iv: new byte[16]); // TODO check if byte array is filled with 0s
        await _algorithm.EncryptToStreamAsync(keyIV, storageStream, EncryptConfig, cancellationToken);
        await _storage.FinalizeAsync(storageStream, cancellationToken);

        async Task EncryptConfig(CryptoStream cryptoStream, CancellationToken cancellationToken)
        {
            var configJson = JsonSerializer.Serialize(config);
            var configData = Encoding.ASCII.GetBytes(configJson);
            await cryptoStream.WriteAsync(configData, cancellationToken);
        }
    }

    /// <summary>
    /// Encrypts the text into the CryptoForest storage with the defined algorithm
    /// </summary>
    /// <param name="text">The text to encrypt</param>
    /// <param name="entryGuid">The GUID of the new entry in the CryptoForest</param>
    /// <param name="usedKeyIV">The key iv used for the encryption of a level config</param>
    /// <returns>Returns the generated Key and IV</returns>
    /// <exception cref="ArgumentException">Thrown when the text is empty or null</exception>
    internal async Task<KeyIV> EncryptTextAsync(string text, Guid entryGuid, CancellationToken cancellationToken, KeyIV? usedKeyIV = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("The text cannot be empty or null", nameof(text));
        }

        using var storageStream = _storage.GetStream(entryGuid, asReadonly: false);
        var keyIV = usedKeyIV ?? _algorithm.GenerateKeyIV();
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
    internal async Task<KeyIV> EncryptFilesAsync(FileSearch fileSearch, Guid entryGuid, CancellationToken cancellationToken)
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
            await cryptoStream.WriteAsync(headerBytes, cancellationToken);
            await cryptoStream.WriteAsync(directoryStructureData, cancellationToken);

            // Encrypt directory structure recusively
            await EncryptDirectoryStructureAsync(directoryStructure, cryptoStream, cancellationToken: cancellationToken);
            async Task EncryptDirectoryStructureAsync(SearchedDirectory directoryStructure, CryptoStream cryptoStream, CancellationToken cancellationToken)
            {
                foreach (var file in directoryStructure.ChildFiles)
                {
                    using var readStream = new FileStream(file.Path, FileMode.Open, FileAccess.Read);
                    await cryptoStream.EncryptFileAsync(readStream, cancellationToken);
                }

                foreach (var directory in directoryStructure.ChildDirectories)
                {
                    await EncryptDirectoryStructureAsync(directory, cryptoStream, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Decrypts a config.
    /// </summary>
    /// <param name="key">The key used in the decryption</param>
    /// <param name="filePath">The path to the file to decrypt</param>
    /// <returns>Returns the decrypted CryptoForestConfig</returns>
    internal async Task<CryptoForestConfig> DecryptConfigAsync(byte[] key, string filePath, CancellationToken cancellationToken)
    {
        using var storageStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var keyIV = new KeyIV(key, iv: new byte[16]); // TODO check if byte array is filled with 0s
        return await _algorithm.DecryptFromStreamAsync(keyIV, storageStream, DecryptConfig, cancellationToken);

        async Task<CryptoForestConfig> DecryptConfig(CryptoStream cryptoStream, CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream();
            await cryptoStream.CopyToAsync(memoryStream, cancellationToken);
            //await memoryStream.FlushAsync(cancellationToken); // TODO check if needed
            var configJson = Encoding.ASCII.GetString(memoryStream.ToArray());
            return JsonSerializer.Deserialize<CryptoForestConfig>(configJson)!;
        }
    }

    /// <summary>
    /// Decrypts the text from the CryptoForest storage with the defined algorithm
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry to decrypt</param>
    /// <param name="keyIV">The KeyIV used for the decryption</param>
    /// <returns>Returns the decrypted text</returns>
    internal async Task<string> DecryptTextAsync(Guid entryGuid, KeyIV keyIV, CancellationToken cancellationToken)
    {
        using var storageStream = _storage.GetStream(entryGuid, asReadonly: true);
        return await _algorithm.DecryptFromStreamAsync(keyIV, storageStream, DecryptText, cancellationToken);

        async Task<string> DecryptText(CryptoStream cryptoStream, CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream();
            await cryptoStream.CopyToAsync(memoryStream, cancellationToken);
            //await memoryStream.FlushAsync(cancellationToken); // TODO check if needed
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }

    /// <summary>
    /// Decrypts the directory structure and data into a SearchedDirectory
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry to decrypt</param>
    /// <param name="keyIV">The KeyIV used for the decryption</param>
    /// <returns>Returns the decrypted SearchedDirectory containing the decrypted data</returns>
    internal async Task<SearchedDirectory> DecryptFilesAsync(Guid entryGuid, KeyIV keyIV, CancellationToken cancellationToken)
    {
        using var storageStream = _storage.GetStream(entryGuid, asReadonly: true);
        return await _algorithm.DecryptFromStreamAsync(keyIV, storageStream, DecryptFiles, cancellationToken);

        async Task<SearchedDirectory> DecryptFiles(CryptoStream cryptoStream, CancellationToken cancellationToken)
        {
            // Decrypt directory structure json
            var directoryStructure = await DecryptDirectoryStructureJsonAsync(cryptoStream, cancellationToken);

            // Decrypt directory structure
            await DecryptDirectoryStructureAsync(directoryStructure, cryptoStream, cancellationToken: cancellationToken);

            return directoryStructure;

            async Task DecryptDirectoryStructureAsync(SearchedDirectory directoryStructure, CryptoStream cryptoStream, CancellationToken cancellationToken, string currentPath = "")
            {
                foreach (var file in directoryStructure.ChildFiles)
                {
                    using var writeStream = new MemoryStream();
                    await cryptoStream.DecryptFileAsync(writeStream, file.FileDataLength, cancellationToken);
                    file.FileData = writeStream.ToArray();
                }

                foreach (var directory in directoryStructure.ChildDirectories)
                {
                    await DecryptDirectoryStructureAsync(directory, cryptoStream, cancellationToken, currentPath + (currentPath != string.Empty ? "/" : "") + directory.DirectoryName);
                }
            }
        }
    }

    /// <summary>
    /// Decrypts the directory structure and files onto the file system
    /// </summary>
    /// <param name="entryGuid">The GUID of the entry to decrypt</param>
    /// <param name="keyIV">The KeyIV used for the decryption</param>
    /// <param name="storageDirectory">The directory to encrypt to</param>
    /// <param name="onFileExists">Defines what happens if a file already exists</param>
    /// <returns>Returns the decrypted SearchedDirectory without file data</returns>
    /// <exception cref="InvalidOperationException">Thrown when Throw is used in onFileExists and a file already exists</exception>
    internal async Task<SearchedDirectory> DecryptFilesAsync(Guid entryGuid, KeyIV keyIV, string storageDirectory, OnFileExists onFileExists, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        using var storageStream = _storage.GetStream(entryGuid, asReadonly: true);
        return await _algorithm.DecryptFromStreamAsync(keyIV, storageStream, DecryptFiles, cancellationToken);

        async Task<SearchedDirectory> DecryptFiles(CryptoStream cryptoStream, CancellationToken cancellationToken)
        {
            // Decrypt directory structure json
            var directoryStructure = await DecryptDirectoryStructureJsonAsync(cryptoStream, cancellationToken);

            // Create directories
            CreateDirectories(storageDirectory, directoryStructure);

            // Decrypt directory structure
            await DecryptDirectoryStructureAsync(directoryStructure, cryptoStream, cancellationToken: cancellationToken);

            return directoryStructure;

            async Task DecryptDirectoryStructureAsync(SearchedDirectory directoryStructure, CryptoStream cryptoStream, CancellationToken cancellationToken, string currentPath = "")
            {
                var fullPath = storageDirectory + (currentPath != string.Empty && !storageDirectory.EndsWith('/') && !storageDirectory.EndsWith('\\') ? "/" : "") + currentPath;
                foreach (var file in directoryStructure.ChildFiles)
                {
                    if (await HandleOnFileExistsAsync($"{fullPath}/{file.FileName}", file.FileDataLength, cryptoStream, onFileExists, cancellationToken))
                    {
                        using var writeStream = new FileStream($"{fullPath}/{file.FileName}", FileMode.Create, FileAccess.Write);
                        await cryptoStream.DecryptFileAsync(writeStream, file.FileDataLength, cancellationToken);
                    }
                }

                foreach (var directory in directoryStructure.ChildDirectories)
                {
                    await DecryptDirectoryStructureAsync(directory, cryptoStream, cancellationToken, currentPath + (currentPath != string.Empty ? "/" : "") + directory.DirectoryName);
                }
            }
        }
    }

    private static async Task<SearchedDirectory> DecryptDirectoryStructureJsonAsync(CryptoStream cryptoStream, CancellationToken cancellationToken)
    {
        var headerBytes = new byte[8];
        await ReadFullyAsync(cryptoStream, headerBytes, cancellationToken);
        var directoryStructureLength = int.Parse(Encoding.ASCII.GetString(headerBytes).Replace("\0", ""));
        var directoryStructureData = new byte[directoryStructureLength];
        await ReadFullyAsync(cryptoStream, directoryStructureData, cancellationToken);
        return JsonSerializer.Deserialize<SearchedDirectory>(directoryStructureData)!;

        // Method to fully read all data into the byte[] generated by Google Gemini 2.5 Flash
        async Task ReadFullyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            var totalBytesRead = 0;
            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = await stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Reached the end of the stream before reading everything.");
                }
                totalBytesRead += bytesRead;
            }
        }
    }

    private static void CreateDirectories(string storageDirectory, SearchedDirectory searchedDirectory, string currentPath = "")
    {
        var fullPath = storageDirectory + (currentPath != string.Empty && !storageDirectory.EndsWith('/') && !storageDirectory.EndsWith('\\') ? "/" : "") + currentPath;
        if (currentPath != string.Empty && !Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        searchedDirectory.ChildDirectories.ForEach(d =>
        {
            CreateDirectories(storageDirectory, d, currentPath + (currentPath != string.Empty ? "/" : "") + d.DirectoryName);
        });
    }

    private static async Task<bool> HandleOnFileExistsAsync(string filePath, long fileLength, CryptoStream cryptoStream, OnFileExists onFileExists, CancellationToken cancellationToken)
    {
        if (File.Exists(filePath))
        {
            switch (onFileExists)
            {
                case OnFileExists.Throw:
                    throw new InvalidOperationException($"File {filePath} already exists");
                case OnFileExists.Replace:
                    File.Delete(filePath);
                    break;
                case OnFileExists.Skip:
                    await cryptoStream.SkipFileAsnyc(fileLength, cancellationToken);
                    return false;
            }
        }

        return true;
    }
}

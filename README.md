# Cryptograph Forest
## What is it?
The Cryptograph Forest library (or short CryptoForest) is a small library that simplifies securely encrypting data. The data can be encrypted in different levels. The structure of the levels is a tree structure which is also where the name comes from as you can create multiple of such trees. The tree structure was choosen as it allows for a level structure of various complexities. The data that can be encrypted to these levels can be simple text, files or a whole directory structure. Each data has an own generated key and iv and is stored as one file even if multiple files or directories are included.

The library was designed to be flexible and adaptable for various use cases. Things like the used algorithm or the storage used can be extended as needed. The encryption algorithms used are symmetric.

## How to use it?
To use it first a new CryptoForest needs to be created. To create a CryptoForest a storage is needed. Currently the only storage that is implemented in the library is a storage on the file system and the only algorithm implemented is AES256. Here is an example on how to create a new CryptoForest:
```c#
var storage = new CryptoForestFileStorage(directoryPath);
var cryptoForest = AesCryptForest.CreateCryptoForest(storage);
```
The `cryptoForest` instance then can be used to encrypt various types of data into levels. A new CryptoForest only has the base level. Each level and data has it's own unique GUID. To decrypt data the GUID of the data is needed. To find out which GUID is the correct one for a level or data the level config that can be retrieved with the `GetBaseLevel` method. The level config then has various methods to search for levels and data. Most methods in the level config are recursive and search all sublevels as well. To identify data and levels each data and level has a name set when encrypting the data.

When adding a directory structure it first needs to be defined what should be included in the directory structure. To define this the `FileSearch` class can be used.

To be able to open the CryptoForest again a config needs to be exported. This can be done with the `ExportConfigAsync` method. This method needs the GUIDs of the levels to be exported and the key used to encrypt it. Once the config has been exported it's pretty easy to just use the encrypted file that was just created to open the CryptoForest again. Here is a sample on how to open it again:
```c#
var storage = new CryptoForestFileStorage(directoryPath);
var cryptoForest = new AesCryptForest(storage, key, configFilePath);
```

All of the methods and their expected parameters have been documented in the code. This documentation can be used to understand better what is expected.

## How to use the `LevelConfig`?
The level configs method are all documented and mostly self explenatory. Most of the methods in the LevelConfig are recursive. When a method is recursive and can be used to search or get data from the whole structure it is mentioned in the method documentation.

To find items and levels the `SearchItems` method can be used. This method searches the keys of all levels and items in the structure by using contains without case sensitivity. To determine if a received `ItemConfig` of this method is a level or an item the property `ItemType` can be used. The key of the returned dictionary is the name of the item or level.

## How to use the `FileSearch`?
There are two ways to create and use the file search. The first on is to just specify the directory. This way everything in this directory and the subdirectories is added to the directory structure that is encrypted.

The other way is to create it with more specific parameters. Additional parameters for the second way are:
- `searchedPaths` -> Used for defining path / part of paths that either are included or excluded.
- `includePaths` -> Used to define if the `searchedPaths` are used to include or exclude files or directories.
- `specificFiles` -> Used to define if the `searchedPaths` contains specific files that should be included. If true `includePaths` is ignored and only the files defined in `searchedPaths` will be added at the base of the directory structure

## How to customize the CryptoForest?
There are two possible places where the CryptoForest library can be customized. These two places are the storage where the encrypted data is stored and the algorithm used.
- To customize the storage the interface [`ICryptoForestStorage`](Cryptograph/Storage/ICryptoForestStorage.cs) can be used
- To customize the algorithm the interface [`ICryptoForestAlgorithm`](Cryptograph/Algorithm/ICryptoForestAlgorithm.cs) can be used

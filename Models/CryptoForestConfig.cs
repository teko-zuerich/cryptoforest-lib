namespace CryptoForestLibrary;

/// <summary>
/// Used for exporting levels of a CryptoForest
/// </summary>
internal class CryptoForestConfig
{
    internal required Guid ConfigGuid { get; init; }

    internal required KeyIV KeyIV { get; init; }

    internal required IEnumerable<CryptoForestConfig> SubLevels { get; init; }
}

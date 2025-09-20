namespace CryptoForestLibrary.Config;

/// <summary>
/// Used for exporting levels of a CryptoForest
/// </summary>
internal class CryptoForestConfig
{
    public required Guid ConfigGuid { get; init; }

    public required KeyIV KeyIV { get; init; }

    public required List<CryptoForestConfig> Sublevels { get; init; }
}

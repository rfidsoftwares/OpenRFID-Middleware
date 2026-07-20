namespace OpenRFID.Core.Abstractions;

/// <summary>
/// Plugin provider interface for instantiating vendor or protocol specific reader connections.
/// </summary>
public interface IReaderProvider
{
    /// <summary>
    /// Unique identifier for this provider (e.g. "llrp", "identium", "impinj", "zebra", "tcp-socket", "serial-com").
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Display name of the RFID hardware brand or driver type.
    /// </summary>
    string BrandName { get; }

    /// <summary>
    /// Protocols supported by this reader provider.
    /// </summary>
    IReadOnlyList<string> SupportedProtocols { get; }

    /// <summary>
    /// Asynchronously creates a reader connection instance using the provided configuration.
    /// </summary>
    Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default);
}

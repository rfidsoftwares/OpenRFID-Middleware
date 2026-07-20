namespace OpenRFID.Core.Abstractions;

/// <summary>
/// Active connection abstraction to an RFID reader hardware unit.
/// </summary>
public interface IReaderConnection : IAsyncDisposable
{
    /// <summary>
    /// Identifier assigned to this reader connection instance.
    /// </summary>
    string ReaderId { get; }

    /// <summary>
    /// Current connection state of the reader.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Event triggered when an RFID tag is read and normalized.
    /// </summary>
    event EventHandler<TagReadEventArgs>? TagRead;

    /// <summary>
    /// Event triggered when reader connection status or health changes.
    /// </summary>
    event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    /// <summary>
    /// Connects asynchronously to the RFID reader.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnects asynchronously from the RFID reader.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies updated configuration to the active reader connection.
    /// </summary>
    Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default);
}

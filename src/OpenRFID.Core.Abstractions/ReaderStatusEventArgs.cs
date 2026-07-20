namespace OpenRFID.Core.Abstractions;

/// <summary>
/// Event arguments emitted when an RFID reader connection status changes.
/// </summary>
public sealed class ReaderStatusEventArgs : EventArgs
{
    public string ReaderId { get; }
    public ConnectionState PreviousState { get; }
    public ConnectionState NewState { get; }
    public string? Message { get; }
    public Exception? Exception { get; }

    public ReaderStatusEventArgs(
        string readerId,
        ConnectionState previousState,
        ConnectionState newState,
        string? message = null,
        Exception? exception = null)
    {
        ReaderId = readerId ?? throw new ArgumentNullException(nameof(readerId));
        PreviousState = previousState;
        NewState = newState;
        Message = message;
        Exception = exception;
    }
}

namespace OpenRFID.Core.Abstractions;

/// <summary>
/// Event arguments emitted when an RFID tag read occurs.
/// </summary>
public sealed class TagReadEventArgs : EventArgs
{
    public TagReadEvent Tag { get; }

    public TagReadEventArgs(TagReadEvent tag)
    {
        Tag = tag ?? throw new ArgumentNullException(nameof(tag));
    }
}

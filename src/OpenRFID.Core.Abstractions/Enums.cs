namespace OpenRFID.Core.Abstractions;

/// <summary>
/// Connection state of an RFID reader driver.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Faulted
}

/// <summary>
/// Transport or protocol type used by the reader plugin.
/// </summary>
public enum ProtocolType
{
    LLRP,
    TcpRaw,
    UdpRaw,
    Serial,
    Mqtt,
    VendorNative
}

/// <summary>
/// Health status evaluated by the ReaderHealthWatchdog.
/// </summary>
public enum ReaderHealthStatus
{
    Healthy,
    Degraded,
    Disconnected,
    Unreachable
}

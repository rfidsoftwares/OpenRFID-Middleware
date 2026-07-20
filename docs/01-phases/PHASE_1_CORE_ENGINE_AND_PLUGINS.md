# 🔌 Phase 1: Core Engine & Reader Driver Plugins

## Objective
Establish the foundational core framework for OpenRFID Middleware, creating unified abstractions for reader devices, dynamic plugin loading, and initial core drivers (LLRP, Identium, Impinj, Zebra, TCP/UDP Sockets, Serial RS232/RS485, and MQTT).

---

## 🎯 Detailed Key Deliverables

### 1. Unified Interface Abstraction (`OpenRFID.Core.Abstractions`)
```csharp
public interface IReaderProvider
{
    string ProviderId { get; }
    string BrandName { get; }
    IReadOnlyList<string> SupportedProtocols { get; }
    
    Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct);
}

public interface IReaderConnection : IAsyncDisposable
{
    string ReaderId { get; }
    ConnectionState State { get; }
    event EventHandler<TagReadEventArgs> TagRead;
    event EventHandler<ReaderStatusEventArgs> StatusChanged;

    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct);
}
```

### 2. Normalized Tag Schema (`TagReadEvent`)
Every incoming tag, regardless of brand or protocol, is normalized into `TagReadEvent`:
- `EPC` (string Hex / ASCII)
- `TID` (string Hex)
- `UserMemory` (string Hex)
- `AntennaPort` (int)
- `RSSI` (float, dBm)
- `ReadCount` (int)
- `FirstSeenTime` (DateTimeOffset UTC)
- `LastSeenTime` (DateTimeOffset UTC)
- `ReaderId` (string)
- `Location` (string)

### 3. Dynamic Plugin Loader (`PluginLoader`)
- Scans `plugins/` directory for compiled assembly packages (`.dll` on Windows, `.so` on Linux).
- Uses isolated `AssemblyLoadContext` to avoid library dependency conflicts.
- Instantiates `IReaderProvider` dynamically based on configuration.

### 4. Health Watchdog & Auto-Reconnect
- Periodically verifies connection state.
- Executes exponential backoff retries when connection breaks.

---

## 🧪 Acceptance Criteria
- [ ] Ability to register and dynamically load custom DLL driver plugins without modifying core code.
- [ ] Successful connection, inventory reading, and tag event normalization across Identium, LLRP, and Serial drivers.
- [ ] Automatic connection recovery verified by simulating network disconnect/re-plug.

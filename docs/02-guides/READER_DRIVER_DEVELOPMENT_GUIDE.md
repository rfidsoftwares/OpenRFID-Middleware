# 🛠️ Reader Driver Development Guide

## Overview

OpenRFID Middleware allows developers to write custom reader driver plugins for specialized, proprietary, or emerging RFID hardware brands. This guide explains how to implement the `IReaderProvider` interface and package custom driver plugins.

---

## 💻 Step-by-Step Implementation

### Step 1: Create a Plugin Library Project
Create a new Class Library project targeting .NET 10 or .NET Standard 2.1:
```bash
dotnet new classlib -n OpenRFID.Plugin.MyBrand
```
Reference `OpenRFID.Core.Abstractions`.

### Step 2: Implement `IReaderProvider`
```csharp
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugin.MyBrand;

public sealed class MyBrandReaderProvider : IReaderProvider
{
    public string ProviderId => "mybrand-rfid";
    public string BrandName => "MyBrand RFID";
    public IReadOnlyList<string> SupportedProtocols => new[] { "TCP-Raw", "Serial" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct)
    {
        var connection = new MyBrandReaderConnection(config);
        return Task.FromResult<IReaderConnection>(connection);
    }
}
```

### Step 3: Implement `IReaderConnection`
```csharp
public sealed class MyBrandReaderConnection : IReaderConnection
{
    public string ReaderId { get; }
    public ConnectionState State { get; private set; }
    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public async Task ConnectAsync(CancellationToken ct)
    {
        // Initialize socket or vendor DLL connection
        State = ConnectionState.Connected;
        // Start background reading loop
    }

    private void OnRawPacketReceived(byte[] rawData)
    {
        // Parse vendor packet into normalized TagReadEvent
        var tag = new TagReadEvent
        {
            EPC = ParseEpc(rawData),
            TID = ParseTid(rawData),
            RSSI = ParseRssi(rawData),
            AntennaPort = ParseAntenna(rawData),
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow,
            ReaderId = ReaderId
        };

        TagRead?.Invoke(this, new TagReadEventArgs(tag));
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        State = ConnectionState.Disconnected;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

### Step 4: Deployment
Drop the output compiled `.dll` into the `plugins/` directory of OpenRFID Middleware. Configure your `config.json` to specify:
```json
"reader": {
  "provider": "mybrand-rfid",
  "ipAddress": "192.168.1.200",
  "port": 5000
}
```
OpenRFID Middleware will dynamically load and execute your driver plugin.

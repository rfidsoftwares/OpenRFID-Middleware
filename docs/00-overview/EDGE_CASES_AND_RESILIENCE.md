# 🛡️ OpenRFID Middleware - Edge Cases & System Resilience Design

## Overview

In industrial and commercial RFID deployments, middleware encounters real-world failures: unreliable networks, physical reader disconnections, high-density tag storms, device power outages, day rollovers during continuous execution, and server response loss.

This document details all identified edge cases and the engineering strategies implemented in **OpenRFID Middleware** to guarantee zero data loss, zero duplication, and continuous resilience.

---

## ⚡ Edge Cases & Mitigation Matrix

### 1. Network Outage & Server Unreachability
- **Scenario**: Remote application server crashes, returns HTTP 500/503 errors, or local network connection drops.
- **Risk**: Loss of tag read events, memory buffer overflow, application crash.
- **Mitigation Strategy**:
  - OpenRFID Middleware uses a **Two-Tier Buffer Architecture**:
    - Tier 1: In-Memory Lock-Free Ring Channel.
    - Tier 2: Transactional SQLite Disk Queue with Write-Ahead Logging (WAL).
  - When HTTP dispatch encounters network failure or non-2xx response codes, active memory batches immediately spill over into the SQLite disk buffer.
  - A background Network Probe worker monitors server reachability using HTTP HEAD/OPTIONS health checks.
  - Once connectivity is restored, the `QueueReplayEngine` drains disk records in controlled batch sizes with configurable rate limits (e.g. 50 tags per request, 10 requests per second) to prevent overloading the recovered server.

### 2. Physical Reader Disconnection & Re-initialization
- **Scenario**: Ethernet cable disconnected, reader power loss, IP address changes, or serial COM port unplugs.
- **Risk**: Stale reader sessions, unhandled socket exceptions, background thread crashes.
- **Mitigation Strategy**:
  - Every `IReaderConnection` instance is wrapped inside a `ReaderHealthWatchdog`.
  - Watchdog sends periodic heartbeat commands (e.g., LLRP KEEPALIVE or reader ping).
  - On connection drop, connection state transitions to `Disconnected`, diagnostic warning logs are emitted, and an **Exponential Backoff Reconnect Algorithm** (1s, 2s, 4s, 8s, 16s, max 60s) attempts clean socket re-establishment.
  - Active tag subscriptions are re-registered automatically upon successful reconnection.

### 3. Tag Storms (High Tag Density / Rapid Scans)
- **Scenario**: Pallet containing 500+ RFID tags enters reader field simultaneously, producing 5,000+ tag reads per second.
- **Risk**: CPU starvation, unbounded RAM growth, GC pauses.
- **Mitigation Strategy**:
  - Deduplication Engine filters duplicates early in the pipeline before queuing or templating.
  - Channel buffer uses bounded capacities with backpressure policies (`BoundedChannelFullMode.Wait` or `DropOldest` depending on configuration).
  - Memory allocations use object pooling (`ArrayPool<T>` and ref structs) to eliminate GC allocation pressure.

### 4. Day Rollover & Midnight Transition (00:00)
- **Scenario**: System operates continuously across midnight (00:00) while daily unique deduplication filter is active.
- **Risk**: Old tag entries clogging memory registry or premature deletion of tags read near midnight.
- **Mitigation Strategy**:
  - Daily Unique Tag Store maintains dual dated registries (`TodayRegistry` and `YesterdayRegistry`).
  - At midnight UTC/Local time, `YesterdayRegistry` is archived/flushed safely, `TodayRegistry` becomes `YesterdayRegistry`, and a clean `TodayRegistry` initializes.
  - Thread synchronization ensures no tag read event is lost during the 1-millisecond swap window.

### 5. Sudden Host Power Loss / Abrupt Shutdown
- **Scenario**: Power outage turns off the host computer running OpenRFID Middleware.
- **Risk**: Corruption of queue state or configuration files.
- **Mitigation Strategy**:
  - Disk storage uses SQLite WAL mode with `PRAGMA synchronous = NORMAL` or `FULL`.
  - Configuration updates write to temporary atomic files (`config.tmp.json`) before atomic renaming (`config.json`).
  - On boot, SQLite auto-recovers uncommitted transactions from WAL file.

### 6. Server Response Loss & Duplicate Delivery Prevention (Idempotency)
- **Scenario**: OpenRFID Middleware sends HTTP POST with tag batch. Server processes batch, but response drops due to network drop. Middleware retries.
- **Risk**: Duplicate records saved in server database (e.g. double attendance entry).
- **Mitigation Strategy**:
  - Every tag batch payload generates a deterministic client-side `TransactionUUID` and attaches it in header (`X-OpenRFID-TxId`) and payload body.
  - Tag entries contain individual `TagScanUUID` (hash of EPC + Timestamp + Antenna + ReaderId).
  - Backend servers use this ID to enforce idempotent inserts (`INSERT ... ON CONFLICT DO NOTHING`).

### 7. Reader Clock Drift vs Server Clock
- **Scenario**: Internal clock on RFID reader drifts by minutes or hours compared to server time.
- **Risk**: Incorrect event sequencing on server.
- **Mitigation Strategy**:
  - OpenRFID Middleware attaches dual timestamps to every record: `ReaderTimestamp` (from hardware) and `HostIngestTimestamp` (UTC timestamp assigned by middleware upon receipt).

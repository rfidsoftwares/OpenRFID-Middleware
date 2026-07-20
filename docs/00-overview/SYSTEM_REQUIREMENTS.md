# ⚙️ OpenRFID Middleware - System Requirements & Specifications

## 🎯 1. Functional Requirements

### 1.1 Reader Management & Integration
- **FR-1.1**: The system MUST support plug-and-play integration with multiple RFID reader manufacturers (Identium, Impinj, Zebra, Alien, Honeywell, ThingMagic, Chafon).
- **FR-1.2**: The system MUST support standard protocols including LLRP (v1.0.1/v1.1), Raw TCP Sockets, UDP Sockets, Serial RS232/RS485, and MQTT.
- **FR-1.3**: The system MUST allow configuring reader hardware parameters: IP address, port, serial baud rate, antenna port bitmask, read power (dBm), and inventory search mode.
- **FR-1.4**: The system MUST maintain automatic connection recovery and watchdog monitoring for all connected readers.

### 1.2 Tag Filtering & Deduplication
- **FR-2.1**: The system MUST provide sliding window deduplication with configurable time window $T$ (seconds or minutes).
- **FR-2.2**: The system MUST provide daily/shift-based deduplication allowing a tag to be sent only once per calendar day or custom shift interval.
- **FR-2.3**: The system MUST support date and time range filtering (e.g. active operating hours schedule).
- **FR-2.4**: The system MUST support RSSI filtering with configurable minimum signal strength (dBm).
- **FR-2.5**: The system MUST support EPC regex pattern matching, prefix filtering, and length validation.
- **FR-2.6**: The system MUST support antenna port masking to enable/disable specific reader ports.

### 1.3 Server Dispatch & Payload Customization
- **FR-3.1**: The system MUST support HTTP dispatch via `POST`, `PUT`, `GET`, and `PATCH` methods.
- **FR-3.2**: The system MUST allow setting custom HTTP request headers (Authorization tokens, Organization IDs, API keys, custom content types).
- **FR-3.3**: The system MUST support dynamic request body formatting using template engines (Liquid / Handlebars) to emit JSON, Form Data, XML, or CSV.
- **FR-3.4**: The system MUST allow configurable transmission frequency triggers:
  - *Instant*: Send as read.
  - *Periodic*: Send every $N$ seconds/minutes.
  - *Batch Size*: Send when buffer reaches $N$ tags.
  - *Hybrid*: Send on whichever trigger occurs first.
- **FR-3.5**: The system MUST allow selecting specific tag metadata fields to transmit (`EPC`, `TID`, `UserMemory`, `RSSI`, `Antenna`, `ReadCount`, `FirstSeen`, `LastSeen`, `DeviceId`, `Location`).

### 1.4 Offline Resilience & Storage
- **FR-4.1**: The system MUST buffer unsent tags into a local disk-backed SQLite database during server network outages.
- **FR-4.2**: The system MUST automatically detect network restoration and drain offline buffered tags with configurable rate limiting.
- **FR-4.3**: The system MUST include unique transaction UUIDs in payloads to allow server-side idempotency.

---

## ⚡ 2. Non-Functional Requirements

- **NFR-1 Latency**: Ingestion-to-dispatch latency under 5 ms for real-time mode.
- **NFR-2 Throughput**: Capable of processing 5,000 tag reads per second on single core CPU.
- **NFR-3 RAM Baseline**: Baseline RAM memory footprint under 60 MB.
- **NFR-4 Cross-Platform**: Executable on Windows, Linux (Ubuntu, Debian, Alpine), and macOS (x64 and ARM64).
- **NFR-5 Uptime Reliability**: Designed for 24/7 continuous unattended operation without memory leak or performance degradation.

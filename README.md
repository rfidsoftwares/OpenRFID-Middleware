# OpenRFID Middleware 📡

> **Universal, Enterprise-Grade, Open-Source RFID Middleware**

OpenRFID Middleware is a modular, high-performance, vendor-agnostic RFID middleware designed to bridge physical RFID reader hardware with modern cloud and on-premise applications. It provides plug-and-play support for major RFID hardware brands, configurable tag filtering/deduplication engines, dynamic payload templating for HTTP (GET, POST, PUT, PATCH), WebSockets, MQTT, and offline resilience queues for uninterrupted operations.

---

## 🌟 Key Features

- **🔌 Vendor & Protocol Agnostic**: Unified plugin interface for **LLRP**, **Identium**, **Impinj Octane**, **Zebra FX**, **Alien**, **TCP/UDP Sockets**, **Serial (RS232/RS485)**, and **MQTT**.
- **🎯 Intelligent Tag Filtering & Deduplication**:
  - Sliding time window deduplication (e.g. ignore duplicate tag reads within 5-15 seconds).
  - Calendar day / Shift-based unique tag rules (send tag once per day or shift).
  - Custom date & time schedule windows (ingest/dispatch tags only during active business hours).
  - RSSI signal strength thresholding and antenna masking.
  - EPC pattern filtering (Prefix, Suffix, Length, Regex).
- **🚀 Flexible Server Integration**:
  - Support for **GET**, **POST**, **PUT**, **PATCH** HTTP methods.
  - Dynamic Custom Headers (Authorization tokens, static keys, custom headers).
  - Templated Body Formats (JSON, Form-Data, XML, CSV via Liquid/Handlebars dynamic templating).
  - Adjustable sync frequency (Instant, Periodic Timed Batches, Tag Count Batches, Hybrid).
- **🛡️ Enterprise Edge-Case Resilience**:
  - Persistent offline disk queue (SQLite WAL) for network outage protection.
  - Automatic reader reconnection with exponential backoff & health watchdog.
  - High-throughput tag storm backpressure handling (1000+ tags/sec).
  - Client-generated transaction UUID idempotency keys.
- **📊 Real-time Monitoring & UI**:
  - Cross-platform Management UI & Dashboard.
  - Live tag scan stream telemetry & diagnostics.
  - OpenAPI/Swagger REST Management API.

---

## ⚡ How to Run & Use the App Locally

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or .NET 8.0+)
- [Git](https://git-scm.com/)
- Docker & Docker Compose *(Optional for containerized deployment)*

### 1. Build the Solution

```bash
git clone https://github.com/rfidsoftwares/OpenRFID-Middleware.git
cd "OpenRFID Middleware"
dotnet build OpenRFID.slnx
```

### 2. Run Management API & Web Dashboard

Launch the ASP.NET Core server hosting the REST API, WebSockets, and Web Dashboard:

```bash
dotnet run --project src/OpenRFID.Management.Api --urls "http://localhost:5180"
```

Once started, open your browser:
- 🖥️ **Web Dashboard UI**: [http://localhost:5180](http://localhost:5180)
- 📜 **Swagger REST API**: [http://localhost:5180/swagger](http://localhost:5180/swagger)

### 3. Run Hardware Simulator (Optional)

To simulate incoming RFID tag streams without physical hardware:

```bash
dotnet run --project src/OpenRFID.Simulator
```
*The simulator listens on TCP port `5084` and broadcasts simulated RFID tags.* You can also test tag ingestion directly using the **⚡ Inject Tag** button on the Web Dashboard.

### 4. Run via Docker Compose

```bash
docker compose up -d
```

---

## 📖 Comprehensive User Guide & Core Use Cases

For step-by-step instructions on setting up readers, configuring filter rules, payload templates, and debugging logs, refer to the official guide:

👉 **[Complete OpenRFID Middleware User Guide & Use Cases](docs/02-guides/USER_GUIDE.md)**

### Key Use Cases Overview

| Use Case | Description | Screenshot Preview |
|---|---|---|
| **1. Live Tag Stream Telemetry** | Real-time monitoring of incoming tag reads, RSSI quality, and system health status. | [View Live Telemetry](docs/images/live_telemetry.png) |
| **2. Multi-Vendor Reader Setup** | Manage Zebra, Impinj, Identium, LLRP, and Socket RFID hardware connections. | [View Readers Setup](docs/images/readers_setup.png) |
| **3. Tag Filtering & Deduplication** | Configure sliding time windows, shift unique mode, RSSI thresholds, and EPC regex filters. | [View Filters & Dedup](docs/images/filters_dedup.png) |
| **4. HTTP Dispatcher & Templates** | Design dynamic JSON/XML HTTP payloads with Handlebars templates & HTTP POST/PUT dispatch. | [View HTTP Dispatcher](docs/images/http_dispatcher.png) |
| **5. Real-Time System Diagnostics** | Stream system log console over WebSockets with severity filtering and error tracking. | [View System Diagnostics](docs/images/system_diagnostics.png) |
| **6. REST API & Swagger Interface** | OpenAPI/Swagger interface for programmatic configuration and integration. | [View Swagger REST API](docs/images/swagger_docs.png) |

---

## 🏗️ Architecture Overview

```
[ RFID Readers ] ---> [ Driver Layer ] ---> [ Filtering & Dedup Engine ] 
                                                   │
                                                   ▼
[ Remote Server ] <--- [ HTTP / MQTT Dispatch ] <--- [ Offline Queue ]
```

---

## 📚 Comprehensive Documentation Hierarchy

Explore our complete phase-wise documentation and guides in the [`docs/`](docs/) directory:

- 📋 **[Project Completion Tracker](docs/PROJECT_TRACKER.md)** - Phase-by-phase implementation progress & task list.
- 📐 **[System Architecture](docs/00-overview/ARCHITECTURE.md)** - Detailed system design, data flow, and component specifications.
- ⚙️ **[System Requirements](docs/00-overview/SYSTEM_REQUIREMENTS.md)** - Functional & non-functional specifications.
- 🛡️ **[Edge Cases & Resilience Matrix](docs/00-overview/EDGE_CASES_AND_RESILIENCE.md)** - Failure handling, offline sync, tag storms, and recovery.

### Implementation Phases
1. 🔌 **[Phase 1: Core Engine & Reader Driver Plugins](docs/01-phases/PHASE_1_CORE_ENGINE_AND_PLUGINS.md)**
2. 🎯 **[Phase 2: Tag Filtering & Deduplication Engine](docs/01-phases/PHASE_2_TAG_FILTERING_AND_DEDUPLICATION.md)**
3. 🚀 **[Phase 3: Payload Dispatcher & HTTP Templating](docs/01-phases/PHASE_3_PAYLOAD_DISPATCHER_AND_TEMPLATES.md)**
4. 🖥️ **[Phase 4: Management API & Desktop/Web UI](docs/01-phases/PHASE_4_DESKTOP_UI_AND_MANAGEMENT_API.md)**
5. 🧪 **[Phase 5: Simulators, CI/CD & Production Release](docs/01-phases/PHASE_5_TESTING_CI_CD_AND_COMMUNITY.md)**

### Developer & User Guides
- 📖 **[User Guide & Use Cases](docs/02-guides/USER_GUIDE.md)** - Detailed local setup and visual feature walkthroughs.
- 🛠️ **[Reader Driver Development Guide](docs/02-guides/READER_DRIVER_DEVELOPMENT_GUIDE.md)** - Guide for writing custom hardware drivers.
- 📝 **[HTTP Payload Templating & Dispatch Guide](docs/02-guides/HTTP_PAYLOAD_TEMPLATING_GUIDE.md)** - Liquid/Handlebars payload formatting guide.

---

## 📜 License

OpenRFID Middleware is open-source software licensed under the [MIT License](LICENSE).

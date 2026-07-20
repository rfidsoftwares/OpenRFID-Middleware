# OpenRFID Middleware 📡

> **Universal, Enterprise-Grade, Open-Source RFID Middleware**

OpenRFID Middleware is a modular, high-performance, vendor-agnostic RFID middleware designed to bridge physical RFID reader hardware with modern cloud and on-premise applications. It provides plug-and-play support for major RFID hardware brands, configurable tag filtering/deduplication engines, dynamic payload templating for HTTP (GET, POST, PUT, PATCH), WebSockets, MQTT, and offline resilience queues for uninterrupted operations.

---

## 🌟 Key Features

- **🔌 Vendor & Protocol Agnostic**: Unified plugin interface for **LLRP**, **Identium**, **Impinj Octane**, **Zebra FX**, **Alien**, **TCP/UDP Sockets**, **Serial (RS232/RS485)**, and **MQTT**.
- **🎯 Intelligent Tag Filtering & Deduplication**:
  - Sliding time window deduplication (e.g. ignore duplicate tag reads within 15 seconds).
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
- 🛠️ **[Reader Driver Development Guide](docs/02-guides/READER_DRIVER_DEVELOPMENT_GUIDE.md)**
- 📝 **[HTTP Payload Templating & Dispatch Guide](docs/02-guides/HTTP_PAYLOAD_TEMPLATING_GUIDE.md)**

---

## 📜 License

OpenRFID Middleware is open-source software licensed under the [MIT License](LICENSE).

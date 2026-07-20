# 🖥️ Phase 4: Management API & Desktop/Web UI

## Objective
Provide an intuitive cross-platform UI dashboard and REST/WebSockets API for configuring reader devices, setting up filtering & payload templates, monitoring live tag streams, and managing system health.

---

## 🎨 Management UI Layout & Components

### 1. Dashboard & Live Tag Telemetry
- **Live Tag Stream**: Real-time table displaying incoming EPCs, TIDs, antenna ports, RSSI levels, and read counts.
- **Hardware Status Indicator**: Connection health pill (Connected, Reconnecting, Disconnected) for all configured readers.
- **Queue Status Gauge**: Visual indicators showing live throughput (tags/sec), memory buffer load, and offline SQLite disk queue count.

### 2. Configuration Studio
- **Reader Setup Tab**: Dropdown selector for reader brand & protocol, IP/Port inputs, COM port selection, power settings, antenna masks.
- **Deduplication & Filter Tab**: Toggle buttons for Sliding Window vs Daily Unique modes, RSSI slider, EPC regex tester tool.
- **Server Dispatcher Tab**: HTTP Method selector (`GET`, `POST`, `PUT`, `PATCH`), endpoint URL input, custom header editor, interactive payload template previewer with test JSON rendering.

---

## 🌐 Management REST & WebSockets API

- **GET `/api/v1/status`**: System health, uptime, connected readers, queue depth.
- **GET/POST `/api/v1/config`**: Fetch or update active configuration with schema validation.
- **POST `/api/v1/config/reload`**: Hot-reload active config without restarting service process.
- **WS `/ws/tags`**: Real-time WebSockets stream of raw and filtered `TagReadEvent` records.
- **WS `/ws/logs`**: Real-time log stream.

---

## 🧪 Acceptance Criteria
- [ ] UI loads smoothly on Windows, Linux, and web browsers.
- [ ] Config modifications in UI apply live via hot-reload without dropping active reader connections.
- [ ] WebSockets tag stream renders 1,000+ tag reads/sec smoothly without freezing UI thread.

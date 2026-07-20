# 🚀 Phase 3: Payload Dispatcher & HTTP Templating

## Objective
Build a dynamic payload generator supporting HTTP methods (`GET`, `POST`, `PUT`, `PATCH`), custom HTTP headers, templated body formatting (Liquid / Handlebars), sync frequency triggers, and an offline SQLite WAL resilience buffer.

---

## 📡 HTTP Method & Server Integration Capabilities

### 1. HTTP Methods
- **`POST`**: Sends payload as request body (JSON, Form Data, XML).
- **`PUT`**: Replaces/updates server resource payload.
- **`PATCH`**: Partial update request.
- **`GET`**: Appends tag data directly to URL query string parameters (e.g. `http://api.server.com/ingest?epc=E2801234&reader=gate-1`).

### 2. Custom Headers Injector
Users can define static and dynamic headers:
```json
"headers": {
  "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "X-Org-Api-Key": "ORG-SEC-99882211",
  "X-Client-Id": "MIDDLEWARE-GATE-01",
  "X-Request-Timestamp": "{{ timestamp_utc }}",
  "Content-Type": "application/json"
}
```

### 3. Payload Templating Engine (Liquid / Handlebars)
Users can design any payload structure expected by their server backend.

#### Example: Custom Array Payload Template
```json
{
  "device": "{{ config.deviceId }}",
  "location": "{{ config.location }}",
  "scanned_at": "{{ current_utc_iso }}",
  "tags": [
    {% for tag in tags %}
    {
      "epc": "{{ tag.epc }}",
      "tid": "{{ tag.tid }}",
      "rssi": {{ tag.rssi }},
      "antenna": {{ tag.antennaPort }},
      "readCount": {{ tag.readCount }}
    }{% if not forloop.last %},{% endif %}
    {% endfor %}
  ]
}
```

---

## ⏱️ Transmission Frequency & Batching Rules

1. **Real-time (Instant)**: Dispatches HTTP request immediately as each tag passes filters.
2. **Periodic Timer**: Flushes accumulated tags every $N$ seconds/minutes.
3. **Batch Size Threshold**: Flushes accumulated tags when buffer reaches $N$ tags (e.g., 100 tags).
4. **Hybrid Trigger**: Flushes whichever condition triggers first (e.g. Every 10 seconds OR 50 tags).

---

## 🛡️ Offline Queue & Network Recovery Engine

```
[ Ingest Stream ] ---> [ Memory Buffer Channel ]
                                │
                        (Network Online?)
                       /                 \
                     YES                  NO
                     /                     \
      [ HTTP Server Dispatch ]    [ SQLite Disk WAL Storage ]
                 │                          │
                 ▼                          ▼
          (Server 200 OK)           (Background Network Probe)
                                            │
                                    (Network Restored?)
                                            │
                                            ▼
                               [ Controlled Queue Replay ]
```

---

## 🧪 Acceptance Criteria
- [ ] Successfully executing HTTP `POST`, `PUT`, `PATCH`, and `GET` requests to mock HTTP endpoints.
- [ ] Template engine correctly rendering custom nested JSON and URL-encoded payloads.
- [ ] Disconnecting network during high-volume scan causes zero tag loss; all tags buffered in SQLite disk queue and successfully replayed upon network restoration.

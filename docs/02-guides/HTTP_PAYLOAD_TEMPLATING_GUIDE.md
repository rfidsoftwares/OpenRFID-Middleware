# 📝 HTTP Payload Templating & Server Integration Guide

## Overview

OpenRFID Middleware allows complete control over how tag data is transmitted to backend servers. You can select HTTP methods (`GET`, `POST`, `PUT`, `PATCH`), configure dynamic HTTP headers, and format body payloads using Liquid templates.

---

## 🛠️ Configuration Structure

In `config.json`, configure the `dispatcher` block:

```json
{
  "dispatcher": {
    "targetUrl": "https://api.yourcompany.com/v1/attendance/ingest",
    "httpMethod": "POST",
    "timeoutSeconds": 30,
    "headers": {
      "Authorization": "Bearer YOUR_JWT_TOKEN",
      "X-Org-Api-Key": "SEC-KEY-998877",
      "Content-Type": "application/json"
    },
    "frequency": {
      "mode": "Hybrid",
      "batchIntervalSeconds": 5,
      "maxBatchSize": 50
    },
    "bodyTemplate": "custom_json"
  }
}
```

---

## 🎨 Template Examples

### Example 1: Standard JSON Array Payload (`POST` / `PUT`)
```json
{
  "device_id": "{{ config.deviceId }}",
  "location": "{{ config.location }}",
  "sync_timestamp": "{{ current_utc_iso }}",
  "scan_count": {{ tags.size }},
  "scans": [
    {% for tag in tags %}
    {
      "epc": "{{ tag.epc }}",
      "tid": "{{ tag.tid }}",
      "antenna": {{ tag.antennaPort }},
      "rssi": {{ tag.rssi }},
      "read_time": "{{ tag.firstSeenTime | date: '%Y-%m-%dT%H:%M:%SZ' }}"
    }{% if not forloop.last %},{% endif %}
    {% endfor %}
  ]
}
```

### Example 2: Form-URL-Encoded (`POST`)
```
device_id={{ config.deviceId }}&epc={{ tags[0].epc }}&timestamp={{ tags[0].firstSeenTime }}
```

### Example 3: URL Parameters (`GET` Mode)
When `httpMethod` is `GET`, the template is rendered as the URL query string:
`https://api.yourcompany.com/v1/scan?epc={{ tag.epc }}&gate={{ config.deviceId }}`

---

## Variables Available in Templates

- `config.deviceId`: Current reader / gateway identifier.
- `config.location`: Reader location string.
- `current_utc_iso`: Current UTC timestamp in ISO-8601 format.
- `transaction_id`: Unique batch UUID string.
- `tags`: Array of tag read records in current batch.
  - `tag.epc`: Electronic Product Code.
  - `tag.tid`: Tag Identifier (if memory read).
  - `tag.userMemory`: User memory bank content.
  - `tag.antennaPort`: Reader antenna port number (1-16).
  - `tag.rssi`: Signal strength in dBm.
  - `tag.readCount`: Read count in batch.
  - `tag.firstSeenTime`: Timestamp when tag entered field.
  - `tag.lastSeenTime`: Timestamp of last read.

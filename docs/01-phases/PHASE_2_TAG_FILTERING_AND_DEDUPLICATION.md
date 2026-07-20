# 🎯 Phase 2: Tag Filtering & Deduplication Engine

## Objective
Implement high-throughput filtering, sliding time-window deduplication, daily/shift unique tag registers, date/time schedule controls, RSSI thresholds, and antenna masking.

---

## ⚙️ Deduplication & Filter Types

### 1. Sliding Time Window Filter (`SlidingWindowFilter`)
- **Concept**: Suppress consecutive reads of the same EPC within $N$ seconds/minutes.
- **Use Case**: Prevent hundreds of redundant events while an item sits in front of an antenna.
- **Config**:
  ```json
  "deduplication": {
    "mode": "SlidingWindow",
    "windowSeconds": 15,
    "scope": "PerAntenna" // Options: PerAntenna, PerReader, Global
  }
  ```

### 2. Daily & Shift Unique Tag Filter (`DailyUniqueFilter`)
- **Concept**: Allow a tag to be dispatched only **ONCE** per calendar day or custom shift window (e.g., Attendance tracking where an employee taps in once per shift).
- **Day Rollover Engine**: Automatically resets the unique EPC registry at midnight or at specified shift boundaries (e.g. 06:00 AM).
- **Config**:
  ```json
  "deduplication": {
    "mode": "DailyUnique",
    "shiftStartLocalTime": "06:00:00",
    "persistRegistryToDisk": true
  }
  ```

### 3. Date & Time Schedule Window (`ScheduleFilter`)
- **Concept**: Pass tag reads only during active operating hours or date ranges.
- **Config**:
  ```json
  "schedule": {
    "enabled": true,
    "operatingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
    "startTime": "08:00:00",
    "endTime": "18:00:00"
  }
  ```

### 4. Hardware & Pattern Filters (`MetadataFilter`)
- **Antenna Bitmask**: `antennaMask: 5` (Binary `0101` = Ports 1 & 3 enabled, Ports 2 & 4 disabled).
- **RSSI Threshold**: `minRssiDbm: -65.0` (Drop weak signals).
- **EPC Matchers**:
  - `prefix`: `"E280"`
  - `suffix`: `"99"`
  - `regexPattern`: `"^E280[0-9A-F]{12}$"`

---

## 🧪 Acceptance Criteria
- [ ] Benchmark test confirming >5,000 tags/sec throughput with active sliding window deduplication.
- [ ] Daily unique tag store tested across simulated day rollover (00:00) with zero lost or double-counted tags.
- [ ] Schedule filter correctly suppressing tags outside specified business hours.

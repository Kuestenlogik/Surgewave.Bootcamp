# Lesson 11.2: IoT Data Platform

> **Difficulty:** Advanced | **Duration:** 60 min | **Prerequisites:** Lesson 11.1

Build an IoT data platform for sensor ingestion, real-time aggregation, and alerting.

## Objectives

- Design a scalable IoT event architecture
- Ingest high-volume sensor data
- Perform real-time aggregations
- Implement threshold-based alerting

## Architecture

```
                           IoT DATA PLATFORM

    +----------+  +----------+  +----------+  +----------+
    | Sensor 1 |  | Sensor 2 |  | Sensor 3 |  | Sensor N |
    +----+-----+  +----+-----+  +----+-----+  +----+-----+
         |             |             |             |
         +-------------+------+------+-------------+
                              |
                              v
                    +---------+---------+
                    |  sensor-readings  |
                    |      topic        |
                    +---------+---------+
                              |
           +------------------+------------------+
           |                  |                  |
           v                  v                  v
    +------+------+    +------+------+    +------+------+
    |  Raw Data   |    | Aggregation |    |  Alerting   |
    |   Storage   |    |   Service   |    |   Service   |
    +------+------+    +------+------+    +------+------+
           |                  |                  |
           v                  v                  v
    +------+------+    +------+------+    +------+------+
    | sensor-raw  |    | sensor-agg  |    |   alerts    |
    |    topic    |    |    topic    |    |    topic    |
    +-------------+    +-------------+    +-------------+
```

## Concepts

### Sensor Data Model

```csharp
public record SensorReading(
    string SensorId,
    string DeviceType,
    string Location,
    double Value,
    string Unit,
    DateTime Timestamp);
```

### Aggregation Types

| Type | Description | Use Case |
|------|-------------|----------|
| Time Window | Aggregate over fixed time periods | Average temperature per minute |
| Tumbling | Non-overlapping windows | Count events per 5 seconds |
| Sliding | Overlapping windows | Moving average over last 10 readings |
| Session | Activity-based windows | Device session metrics |

### Alert Conditions

| Condition | Description | Example |
|-----------|-------------|---------|
| Threshold | Value exceeds limit | Temperature > 100 |
| Rate of Change | Value changes too fast | Delta > 10 per second |
| Missing Data | No readings received | No data for 60 seconds |
| Anomaly | Value outside normal range | Z-score > 3 |

### Topics Structure

- `sensor-readings` - Raw sensor data
- `sensor-raw` - Archived raw data
- `sensor-aggregates` - Computed aggregations
- `alerts` - Alert notifications

## Key Takeaways

- IoT data is high-volume, time-series data
- Partitioning by sensor ID enables parallel processing
- Aggregations reduce data volume while preserving insights
- Real-time alerting enables immediate response to anomalies

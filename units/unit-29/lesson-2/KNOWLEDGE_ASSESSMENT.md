# Quiz: Lesson 11.2 - IoT Data Platform

## Multiple Choice

### 1. Which partitioning strategy is recommended for IoT sensor data?
- [ ] A) Random partitioning
- [ ] B) Partitioning by timestamp
- [ ] C) Partitioning by sensor ID
- [ ] D) Single partition for all data

<details>
<summary>Answer</summary>
**C)** Partitioning by sensor ID enables parallel processing while keeping all data from a single sensor in order. This is important for time-series analysis and aggregations per device.
</details>

### 2. What type of aggregation uses non-overlapping fixed time periods?
- [ ] A) Sliding window
- [ ] B) Tumbling window
- [ ] C) Session window
- [ ] D) Hopping window

<details>
<summary>Answer</summary>
**B)** Tumbling windows divide time into fixed, non-overlapping periods. For example, counting events every 5 seconds produces distinct buckets with no overlap.
</details>

### 3. Which alert condition detects values changing too rapidly?
- [ ] A) Threshold
- [ ] B) Missing Data
- [ ] C) Rate of Change
- [ ] D) Anomaly

<details>
<summary>Answer</summary>
**C)** Rate of Change alerts detect when values change faster than expected (e.g., temperature increasing more than 10 degrees per second), which often indicates equipment problems or sensor malfunctions.
</details>

## True/False

### 4. IoT aggregations reduce data volume while preserving analytical insights.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Aggregations like averages, min/max, and counts summarize high-volume raw data into smaller, more manageable datasets while retaining the insights needed for analysis and alerting.
</details>

### 5. Sliding windows produce the same results as tumbling windows with the same duration.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Sliding windows overlap (e.g., moving average of last 10 readings, updated every reading), while tumbling windows don't overlap. Sliding windows produce more frequent, smoother results but require more computation.
</details>

## Code Challenge

### 6. Design alert conditions for this industrial sensor scenario:

A temperature sensor monitors a furnace that should operate between 800-1000 degrees Celsius. You need alerts for:
- Temperature too high or too low
- Sensor not reporting (possible failure)
- Rapid temperature changes (possible malfunction)

<details>
<summary>Answer</summary>
```csharp
public class FurnaceAlertConditions
{
    // 1. Threshold alert - temperature out of range
    public bool CheckThreshold(double temperature)
    {
        return temperature < 800 || temperature > 1000;
    }

    // 2. Missing data alert - no reading for 60 seconds
    public bool CheckMissingData(DateTime lastReading)
    {
        return DateTime.UtcNow - lastReading > TimeSpan.FromSeconds(60);
    }

    // 3. Rate of change alert - more than 50 degrees per minute
    public bool CheckRateOfChange(double current, double previous, TimeSpan interval)
    {
        var ratePerMinute = Math.Abs(current - previous) / interval.TotalMinutes;
        return ratePerMinute > 50;
    }
}

// Alert types to emit
public record HighTemperatureAlert(string SensorId, double Temperature);
public record LowTemperatureAlert(string SensorId, double Temperature);
public record SensorOfflineAlert(string SensorId, DateTime LastSeen);
public record RapidChangeAlert(string SensorId, double RatePerMinute);
```
</details>

### 7. What's wrong with this sensor data aggregation?

```csharp
var readings = new List<SensorReading>();

await foreach (var reading in sensorStream)
{
    readings.Add(reading);

    // Calculate average every 100 readings
    if (readings.Count >= 100)
    {
        var avg = readings.Average(r => r.Value);
        await ProduceAggregate(new { Average = avg });
        readings.Clear();
    }
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Count-based instead of time-based** - Using 100 readings means aggregation frequency depends on sensor rate, not time. A sensor sending 1000/sec aggregates every 0.1s, while one sending 1/sec aggregates every 100s.

2. **No timestamp tracking** - The aggregate has no time context (when did these readings occur?).

3. **Memory growth** - If the sensor stops, readings list grows indefinitely waiting for 100.

4. **No sensor grouping** - Multiple sensors would be mixed together.

Fix:
```csharp
var windowedReadings = new Dictionary<string, List<SensorReading>>();
var windowStart = DateTime.UtcNow;
var windowDuration = TimeSpan.FromSeconds(10);

await foreach (var reading in sensorStream)
{
    // Group by sensor
    if (!windowedReadings.ContainsKey(reading.SensorId))
        windowedReadings[reading.SensorId] = new();

    windowedReadings[reading.SensorId].Add(reading);

    // Time-based window
    if (DateTime.UtcNow - windowStart >= windowDuration)
    {
        foreach (var (sensorId, values) in windowedReadings)
        {
            await ProduceAggregate(new
            {
                SensorId = sensorId,
                WindowStart = windowStart,
                WindowEnd = DateTime.UtcNow,
                Average = values.Average(r => r.Value),
                Count = values.Count
            });
        }
        windowedReadings.Clear();
        windowStart = DateTime.UtcNow;
    }
}
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson

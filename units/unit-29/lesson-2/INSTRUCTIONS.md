# Lesson 11.2: IoT Data Platform - Instructions

## Step 1: Define the Domain Models

Create sensor data types:

```csharp
public record SensorReading(
    string SensorId,
    string DeviceType,
    string Location,
    double Value,
    string Unit,
    DateTime Timestamp);

public record SensorAggregate(
    string SensorId,
    string AggregationType,
    double MinValue,
    double MaxValue,
    double AvgValue,
    int Count,
    DateTime WindowStart,
    DateTime WindowEnd);

public record Alert(
    string AlertId,
    string SensorId,
    string Severity,
    string Message,
    double Value,
    double Threshold,
    DateTime Timestamp);
```

## Step 2: Create Sensor Simulators

Generate realistic sensor data:

```csharp
var sensors = new[]
{
    ("TEMP-001", "temperature", "Building-A", 20.0, 25.0, "celsius"),
    ("TEMP-002", "temperature", "Building-B", 18.0, 22.0, "celsius"),
    ("HUMID-001", "humidity", "Building-A", 40.0, 60.0, "percent"),
    ("PRESS-001", "pressure", "Building-A", 1010.0, 1020.0, "hPa")
};

var random = new Random();
foreach (var (id, type, location, min, max, unit) in sensors)
{
    var value = min + random.NextDouble() * (max - min);
    var reading = new SensorReading(id, type, location, value, unit, DateTime.UtcNow);
    await producer.ProduceAsync("sensor-readings",
        Encoding.UTF8.GetBytes(id),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reading)));
}
```

## Step 3: Create the Ingestion Service

Consume and archive raw sensor data:

```csharp
await using var rawConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "sensor-ingestion";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

rawConsumer.Subscribe("sensor-readings");

// Store in raw archive topic with additional metadata
var result = await rawConsumer.ConsumeAsync(timeout);
if (result != null)
{
    // Add ingestion timestamp and forward to raw archive
    await archiveProducer.ProduceAsync("sensor-raw", result.Key, result.Value);
}
```

## Step 4: Create the Aggregation Service

Compute windowed aggregations:

```csharp
// Collect readings per sensor
var readingsBySensor = new Dictionary<string, List<SensorReading>>();

// Aggregate when window completes
foreach (var (sensorId, readings) in readingsBySensor)
{
    var aggregate = new SensorAggregate(
        sensorId,
        "1-minute",
        readings.Min(r => r.Value),
        readings.Max(r => r.Value),
        readings.Average(r => r.Value),
        readings.Count,
        windowStart,
        windowEnd);

    await aggProducer.ProduceAsync("sensor-aggregates",
        Encoding.UTF8.GetBytes(sensorId),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(aggregate)));
}
```

## Step 5: Create the Alerting Service

Monitor for threshold violations:

```csharp
var thresholds = new Dictionary<string, (double min, double max)>
{
    ["temperature"] = (15.0, 30.0),
    ["humidity"] = (30.0, 70.0),
    ["pressure"] = (990.0, 1030.0)
};

// Check each reading against thresholds
if (reading.Value < threshold.min || reading.Value > threshold.max)
{
    var alert = new Alert(
        $"ALERT-{Guid.NewGuid():N}",
        reading.SensorId,
        reading.Value > threshold.max * 1.2 ? "CRITICAL" : "WARNING",
        $"{reading.DeviceType} out of range: {reading.Value} {reading.Unit}",
        reading.Value,
        reading.Value > threshold.max ? threshold.max : threshold.min,
        DateTime.UtcNow);

    await alertProducer.ProduceAsync("alerts",
        Encoding.UTF8.GetBytes(reading.SensorId),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(alert)));
}
```

## Step 6: Create the Dashboard Consumer

Display real-time metrics:

```csharp
// Subscribe to aggregates topic
aggConsumer.Subscribe("sensor-aggregates");

// Display dashboard
Console.WriteLine($"Sensor: {agg.SensorId}");
Console.WriteLine($"  Min: {agg.MinValue:F2}");
Console.WriteLine($"  Max: {agg.MaxValue:F2}");
Console.WriteLine($"  Avg: {agg.AvgValue:F2}");
Console.WriteLine($"  Count: {agg.Count}");
```

## Exercises

1. **Basic Platform**: Complete the TODO items in `start/Program.cs`
2. **Add Device Types**: Support additional sensor types (motion, light, CO2)
3. **Sliding Windows**: Implement sliding window aggregations
4. **Alert Correlation**: Group related alerts from same location
5. **Data Quality**: Add validation and filter bad readings

## Testing Your Implementation

Run the completed solution:

```bash
cd completed
dotnet run
```

Expected output:
- Sensor readings generated and ingested
- Aggregations computed per sensor
- Alerts triggered for out-of-range values
- Dashboard showing real-time metrics

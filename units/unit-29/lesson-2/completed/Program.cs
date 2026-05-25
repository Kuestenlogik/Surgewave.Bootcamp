// Surgewave Bootcamp - Unit 11, Lesson 2: IoT Data Platform
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.2 - IoT Data Platform ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// ============== Configuration ==============
var thresholds = new Dictionary<string, (double min, double max)>
{
    ["temperature"] = (15.0, 30.0),
    ["humidity"] = (30.0, 70.0),
    ["pressure"] = (990.0, 1030.0)
};

var sensors = new[]
{
    ("TEMP-001", "temperature", "Building-A", 20.0, 25.0, "celsius"),
    ("TEMP-002", "temperature", "Building-B", 18.0, 22.0, "celsius"),
    ("HUMID-001", "humidity", "Building-A", 40.0, 60.0, "percent"),
    ("PRESS-001", "pressure", "Building-A", 1010.0, 1020.0, "hPa")
};

// ============== Step 1: Sensor Simulators ==============
Console.WriteLine("--- Step 1: Generating Sensor Data ---");

await using var sensorProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

var random = new Random(42);
var totalReadings = 0;

for (var round = 0; round < 5; round++)
{
    foreach (var (id, type, location, min, max, unit) in sensors)
    {
        var value = min + random.NextDouble() * (max - min);

        // Add some anomalies for demonstration
        if (round == 3 && id == "TEMP-001") value = 35.0; // High temperature alert
        if (round == 4 && id == "HUMID-001") value = 25.0; // Low humidity alert

        var reading = new SensorReading(id, type, location, value, unit, DateTime.UtcNow);
        await sensorProducer.ProduceAsync("sensor-readings",
            Encoding.UTF8.GetBytes(id),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reading)));

        Console.WriteLine($"  [{id}] {value:F2} {unit} @ {location}");
        totalReadings++;
    }
    await Task.Delay(100); // Small delay between rounds
}

Console.WriteLine($"  Generated {totalReadings} sensor readings");
Console.WriteLine();

// ============== Step 2: Aggregation Service ==============
Console.WriteLine("--- Step 2: Computing Aggregations ---");

await using var aggConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "aggregation-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await using var aggProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

aggConsumer.Subscribe("sensor-readings");

// Collect readings by sensor
var readingsBySensor = new Dictionary<string, List<SensorReading>>();
var timeout = TimeSpan.FromMilliseconds(500);
var deadline = DateTime.UtcNow.AddSeconds(3);

while (DateTime.UtcNow < deadline)
{
    var result = await aggConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var reading = JsonSerializer.Deserialize<SensorReading>(result.Value)!;
        if (!readingsBySensor.ContainsKey(reading.SensorId))
            readingsBySensor[reading.SensorId] = new List<SensorReading>();
        readingsBySensor[reading.SensorId].Add(reading);
    }
}

// Compute and emit aggregations
var windowStart = DateTime.UtcNow.AddSeconds(-5);
var windowEnd = DateTime.UtcNow;

foreach (var (sensorId, readings) in readingsBySensor)
{
    var aggregate = new SensorAggregate(
        sensorId,
        "5-second",
        readings.Min(r => r.Value),
        readings.Max(r => r.Value),
        readings.Average(r => r.Value),
        readings.Count,
        windowStart,
        windowEnd);

    await aggProducer.ProduceAsync("sensor-aggregates",
        Encoding.UTF8.GetBytes(sensorId),
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(aggregate)));

    Console.WriteLine($"  [{sensorId}] Min={aggregate.MinValue:F2}, Max={aggregate.MaxValue:F2}, Avg={aggregate.AvgValue:F2}, Count={aggregate.Count}");
}

Console.WriteLine();

// ============== Step 3: Alerting Service ==============
Console.WriteLine("--- Step 3: Processing Alerts ---");

await using var alertConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "alerting-service";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await using var alertProducer = new SurgewaveProducer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
});

alertConsumer.Subscribe("sensor-readings");

var alertsGenerated = 0;
deadline = DateTime.UtcNow.AddSeconds(3);

while (DateTime.UtcNow < deadline)
{
    var result = await alertConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var reading = JsonSerializer.Deserialize<SensorReading>(result.Value)!;

        if (thresholds.TryGetValue(reading.DeviceType, out var threshold))
        {
            string? severity = null;
            double alertThreshold = 0;

            if (reading.Value > threshold.max)
            {
                severity = reading.Value > threshold.max * 1.1 ? "CRITICAL" : "WARNING";
                alertThreshold = threshold.max;
            }
            else if (reading.Value < threshold.min)
            {
                severity = reading.Value < threshold.min * 0.9 ? "CRITICAL" : "WARNING";
                alertThreshold = threshold.min;
            }

            if (severity != null)
            {
                var alert = new Alert(
                    $"ALERT-{Guid.NewGuid().ToString()[..8]}",
                    reading.SensorId,
                    severity,
                    $"{reading.DeviceType} out of range: {reading.Value:F2} {reading.Unit} (threshold: {alertThreshold:F2})",
                    reading.Value,
                    alertThreshold,
                    DateTime.UtcNow);

                await alertProducer.ProduceAsync("alerts",
                    Encoding.UTF8.GetBytes(reading.SensorId),
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(alert)));

                Console.WriteLine($"  [{severity}] {alert.Message}");
                alertsGenerated++;
            }
        }
    }
}

Console.WriteLine($"  Generated {alertsGenerated} alerts");
Console.WriteLine();

// ============== Step 4: Dashboard ==============
Console.WriteLine("--- Step 4: Dashboard ---");
Console.WriteLine();
Console.WriteLine("=== SENSOR AGGREGATES ===");

await using var dashAggConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "dashboard-agg";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

dashAggConsumer.Subscribe("sensor-aggregates");
deadline = DateTime.UtcNow.AddSeconds(2);

while (DateTime.UtcNow < deadline)
{
    var result = await dashAggConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var agg = JsonSerializer.Deserialize<SensorAggregate>(result.Value)!;
        Console.WriteLine($"+---------------------------+");
        Console.WriteLine($"| Sensor: {agg.SensorId,-17} |");
        Console.WriteLine($"+---------------------------+");
        Console.WriteLine($"| Min:   {agg.MinValue,10:F2}        |");
        Console.WriteLine($"| Max:   {agg.MaxValue,10:F2}        |");
        Console.WriteLine($"| Avg:   {agg.AvgValue,10:F2}        |");
        Console.WriteLine($"| Count: {agg.Count,10}        |");
        Console.WriteLine($"+---------------------------+");
        Console.WriteLine();
    }
}

Console.WriteLine("=== ACTIVE ALERTS ===");

await using var dashAlertConsumer = new SurgewaveConsumer<byte[], byte[]>(options =>
{
    options.BootstrapServers = $"localhost:{runtime.Port}";
    options.GroupId = "dashboard-alerts";
    options.AutoOffsetReset = AutoOffsetReset.Earliest;
});

dashAlertConsumer.Subscribe("alerts");
deadline = DateTime.UtcNow.AddSeconds(2);
var alerts = new List<Alert>();

while (DateTime.UtcNow < deadline)
{
    var result = await dashAlertConsumer.ConsumeAsync(timeout);
    if (result?.Value != null)
    {
        var alert = JsonSerializer.Deserialize<Alert>(result.Value)!;
        alerts.Add(alert);
    }
}

if (alerts.Count > 0)
{
    Console.WriteLine($"+-----------+------------+----------+----------------------------------------+");
    Console.WriteLine($"| Severity  | Sensor     | Value    | Message                                |");
    Console.WriteLine($"+-----------+------------+----------+----------------------------------------+");
    foreach (var alert in alerts)
    {
        Console.WriteLine($"| {alert.Severity,-9} | {alert.SensorId,-10} | {alert.Value,8:F2} | {alert.Message,-38} |");
    }
    Console.WriteLine($"+-----------+------------+----------+----------------------------------------+");
}
else
{
    Console.WriteLine("  No active alerts");
}

Console.WriteLine();
Console.WriteLine("IoT Data Platform complete!");

// ============== Domain Models ==============

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

// Surgewave Bootcamp - Unit 11, Lesson 2: IoT Data Platform
//
// In this lesson, you'll build an IoT data platform with:
// - Sensor data ingestion
// - Real-time aggregation
// - Threshold-based alerting
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 11.2 - IoT Data Platform ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
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

// TODO: Create a producer for sensor readings
// Hint: await using var sensorProducer = new SurgewaveProducer<byte[], byte[]>(options =>
// {
//     options.BootstrapServers = $"localhost:{runtime.Port}";
// });

// TODO: Generate sensor readings (simulate multiple rounds)
// Hint: var random = new Random(42);
// for (var round = 0; round < 5; round++)
// {
//     foreach (var (id, type, location, min, max, unit) in sensors)
//     {
//         var value = min + random.NextDouble() * (max - min);
//         // Add some anomalies
//         if (round == 3 && id == "TEMP-001") value = 35.0; // High temp alert
//         var reading = new SensorReading(id, type, location, value, unit, DateTime.UtcNow);
//         await sensorProducer.ProduceAsync("sensor-readings",
//             Encoding.UTF8.GetBytes(id),
//             Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reading)));
//     }
// }

Console.WriteLine("Sensor data generated!");
Console.WriteLine();

// ============== Step 2: Aggregation Service ==============
Console.WriteLine("--- Step 2: Computing Aggregations ---");

// TODO: Create a consumer for sensor-readings
// TODO: Create a producer for sensor-aggregates

// TODO: Collect readings by sensor and compute aggregations
// Hint: var readingsBySensor = new Dictionary<string, List<SensorReading>>();
// Process readings, group by sensor ID, then compute min/max/avg/count

Console.WriteLine("Aggregations computed!");
Console.WriteLine();

// ============== Step 3: Alerting Service ==============
Console.WriteLine("--- Step 3: Processing Alerts ---");

// TODO: Create a consumer for sensor-readings (different group)
// TODO: Create a producer for alerts

// TODO: Check each reading against thresholds
// Emit alerts for out-of-range values

Console.WriteLine("Alerts processed!");
Console.WriteLine();

// ============== Step 4: Dashboard ==============
Console.WriteLine("--- Step 4: Dashboard ---");

// TODO: Create a consumer for sensor-aggregates
// TODO: Display aggregations in a dashboard format

// TODO: Create a consumer for alerts
// TODO: Display active alerts

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

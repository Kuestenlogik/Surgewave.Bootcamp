# Lesson 10.6: Instructions

Follow these step-by-step instructions to complete the lesson.

## Step 1: Set Up the Environment

```csharp
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .WithSchemaRegistry()   // Enable Schema Registry
    .Build()
    .StartAsync();

Console.WriteLine("Surgewave + Schema Registry running on localhost:9092");
```

## Step 2: Produce Realistic Sample Messages

Produce messages with diverse field types so the inference engine has rich data to analyse:

```csharp
await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = "localhost:9092");

var orders = Enumerable.Range(1, 50).Select(i => new
{
    id        = i,
    email     = $"user{i}@example.com",
    createdAt = DateTime.UtcNow.AddDays(-i).ToString("O"),
    orderId   = Guid.NewGuid().ToString(),
    total     = Math.Round(i * 9.99, 2),
    profileUrl = $"https://example.com/users/{i}"
});

foreach (var order in orders)
{
    await producer.ProduceAsync("orders", $"order-{order.id}",
        JsonSerializer.Serialize(order));
}
Console.WriteLine($"Produced 50 messages to 'orders'");
```

## Step 3: Implement a Schema Inference Engine

Build the inference engine that processes sampled JSON:

```csharp
public class SimpleSchemaInferenceEngine
{
    public InferredSchema Infer(IReadOnlyList<string> jsonSamples)
    {
        var properties = new Dictionary<string, (HashSet<JsonValueKind> Types, List<string> Strings, int Count)>();

        foreach (var json in jsonSamples)
        {
            var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!properties.ContainsKey(prop.Name))
                    properties[prop.Name] = (new(), new(), 0);

                var (types, strings, count) = properties[prop.Name];
                types.Add(prop.Value.ValueKind);
                if (prop.Value.ValueKind == JsonValueKind.String)
                    strings.Add(prop.Value.GetString()!);
                properties[prop.Name] = (types, strings, count + 1);
            }
        }

        var schemaProps = new Dictionary<string, object>();
        var required    = new List<string>();

        foreach (var (name, (types, strings, count)) in properties)
        {
            if (count >= jsonSamples.Count * 0.95)
                required.Add(name);

            var jsonType = types.Contains(JsonValueKind.Number)   ? "number"  :
                           types.Contains(JsonValueKind.String)   ? "string"  :
                           types.Contains(JsonValueKind.True) ||
                           types.Contains(JsonValueKind.False)     ? "boolean" : "string";

            var format = DetectFormat(strings);
            schemaProps[name] = format != null
                ? (object)new { type = jsonType, format }
                : new { type = jsonType };
        }

        return new InferredSchema(
            Properties: schemaProps,
            Required: required,
            SampleCount: jsonSamples.Count);
    }

    private static string? DetectFormat(List<string> samples)
    {
        if (samples.Count == 0) return null;

        double threshold = 0.9;

        if (MatchRate(samples, s => DateTime.TryParse(s, out _) && s.Contains('T')) >= threshold)
            return "date-time";
        if (MatchRate(samples, s => s.Contains('@') && s.Contains('.')) >= threshold)
            return "email";
        if (MatchRate(samples, s => s.StartsWith("http://") || s.StartsWith("https://")) >= threshold)
            return "uri";
        if (MatchRate(samples, s => Guid.TryParse(s, out _)) >= threshold)
            return "uuid";

        return null;
    }

    private static double MatchRate(List<string> samples, Func<string, bool> predicate)
        => samples.Count > 0 ? samples.Count(predicate) / (double)samples.Count : 0;
}

public record InferredSchema(
    Dictionary<string, object> Properties,
    List<string> Required,
    int SampleCount);
```

## Step 4: Run Inference and Inspect Results

```csharp
// Collect the messages we just produced
var samples = new List<string>();
await using var sampleConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = "localhost:9092";
    opt.GroupId = $"infer-{Guid.NewGuid()}";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});
sampleConsumer.Subscribe("orders");

while (samples.Count < 50)
{
    var r = await sampleConsumer.ConsumeAsync(TimeSpan.FromMilliseconds(500));
    if (r == null) break;
    samples.Add(r.Value);
}
Console.WriteLine($"Collected {samples.Count} samples");

// Infer the schema
var engine   = new SimpleSchemaInferenceEngine();
var inferred = engine.Infer(samples);

Console.WriteLine($"\nInferred {inferred.Properties.Count} properties:");
foreach (var (name, spec) in inferred.Properties)
    Console.WriteLine($"  {name}: {JsonSerializer.Serialize(spec)}");

Console.WriteLine($"\nRequired fields ({inferred.Required.Count}): {string.Join(", ", inferred.Required)}");
```

## Step 5: Implement the Evolution Advisor

```csharp
public class SchemaEvolutionAdvisor
{
    public EvolutionAdvice Analyse(
        InferredSchema oldSchema,
        InferredSchema newSchema,
        string subject)
    {
        var changes = new List<SchemaChange>();

        // Detect removed fields
        foreach (var name in oldSchema.Properties.Keys.Except(newSchema.Properties.Keys))
            changes.Add(new SchemaChange(name, ChangeType.Removed,
                WasRequired: oldSchema.Required.Contains(name)));

        // Detect added fields
        foreach (var name in newSchema.Properties.Keys.Except(oldSchema.Properties.Keys))
            changes.Add(new SchemaChange(name, ChangeType.Added,
                IsRequired: newSchema.Required.Contains(name)));

        var level = changes.Any(c => c is { ChangeType: ChangeType.Removed, WasRequired: true })
            ? BreakingLevel.Major
            : changes.Any(c => c.ChangeType == ChangeType.Added)
                ? BreakingLevel.Minor
                : BreakingLevel.None;

        var migrationCode = GenerateMigrationCode(changes);

        return new EvolutionAdvice(subject, changes, level, migrationCode);
    }

    private static string GenerateMigrationCode(List<SchemaChange> changes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated migration — review before use");

        foreach (var c in changes.Where(x => x.ChangeType == ChangeType.Added))
        {
            sb.AppendLine($"// New field: '{c.FieldName}' ({(c.IsRequired ? "required" : "optional")})");
            sb.AppendLine($"public string? {ToPascalCase(c.FieldName)} {{ get; set; }} = null;");
        }

        foreach (var c in changes.Where(x => x.ChangeType == ChangeType.Removed && x.WasRequired))
            sb.AppendLine($"// BREAKING: Required field '{c.FieldName}' removed. Update all consumers.");

        return sb.ToString();
    }

    private static string ToPascalCase(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}

public enum ChangeType { Added, Removed, TypeChanged }
public enum BreakingLevel { None, Minor, Major }

public record SchemaChange(
    string FieldName,
    ChangeType ChangeType,
    bool WasRequired = false,
    bool IsRequired = false);

public record EvolutionAdvice(
    string Subject,
    List<SchemaChange> Changes,
    BreakingLevel BreakingLevel,
    string MigrationCode);
```

## Step 6: Demonstrate Evolution Scenarios

```csharp
var advisor = new SchemaEvolutionAdvisor();

// Scenario A: Add optional field (Minor)
Console.WriteLine("\n--- Scenario A: Add Optional Field ---");
var schemaWithPhone = inferred with
{
    Properties = new(inferred.Properties) { ["phone"] = new { type = "string" } }
};
var adviceA = advisor.Analyse(inferred, schemaWithPhone, "orders-value");
Console.WriteLine($"Breaking level: {adviceA.BreakingLevel}");
Console.WriteLine(adviceA.MigrationCode);

// Scenario B: Remove required field (Major)
Console.WriteLine("--- Scenario B: Remove Required Field ---");
var schemaWithoutEmail = inferred with
{
    Properties = inferred.Properties
        .Where(p => p.Key != "email")
        .ToDictionary(p => p.Key, p => p.Value)
};
var adviceB = advisor.Analyse(inferred, schemaWithoutEmail, "orders-value");
Console.WriteLine($"Breaking level: {adviceB.BreakingLevel}  ← requires coordination!");
Console.WriteLine(adviceB.MigrationCode);
```

## Verification

Run the completed solution:

```bash
cd completed
dotnet run
```

Expected output:
- 50 messages produced with realistic formats
- Inferred schema with `email`, `date-time`, `uri`, and `uuid` formats detected
- Evolution analysis correctly classifying Minor vs Major changes
- Generated C# migration code for each scenario

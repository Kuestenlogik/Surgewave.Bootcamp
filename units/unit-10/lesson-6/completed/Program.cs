// Surgewave Bootcamp - Unit 10, Lesson 6: Live Schema Inference & AI-Assisted Evolution
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// ──────────────────────────────────────────────────────────────────────────────
// Domain types
// ──────────────────────────────────────────────────────────────────────────────

public enum ChangeType { Added, Removed }
public enum BreakingLevel { None, Minor, Major }

public record SchemaChange(
    string FieldName,
    ChangeType ChangeType,
    bool WasRequired = false,
    bool IsRequired = false);

public record InferredSchema(
    Dictionary<string, SchemaProperty> Properties,
    List<string> Required,
    int SampleCount);

public record SchemaProperty(string Type, string? Format = null);

public record EvolutionAdvice(
    string Subject,
    List<SchemaChange> Changes,
    BreakingLevel BreakingLevel,
    string MigrationCode)
{
    public bool IsSafeToRegisterImmediately =>
        BreakingLevel is BreakingLevel.None or BreakingLevel.Minor;
}

// ──────────────────────────────────────────────────────────────────────────────
// Schema Inference Engine
// ──────────────────────────────────────────────────────────────────────────────

public class SimpleSchemaInferenceEngine
{
    private const double RequiredThreshold  = 0.95;
    private const double FormatThreshold    = 0.90;

    private static readonly Regex DateTimePattern =
        new(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", RegexOptions.Compiled);
    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex UriPattern =
        new(@"^(https?|ftp)://", RegexOptions.Compiled);
    private static readonly Regex UuidPattern =
        new(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public InferredSchema Infer(IReadOnlyList<string> jsonSamples)
    {
        // field → (types seen, string values, count)
        var stats = new Dictionary<string, (HashSet<JsonValueKind> Types, List<string> Strings, int Count)>();

        foreach (var json in jsonSamples)
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!stats.TryGetValue(prop.Name, out var entry))
                    entry = stats[prop.Name] = (new(), new(), 0);

                entry.Types.Add(prop.Value.ValueKind);
                if (prop.Value.ValueKind == JsonValueKind.String)
                    entry.Strings.Add(prop.Value.GetString()!);
                stats[prop.Name] = (entry.Types, entry.Strings, entry.Count + 1);
            }
        }

        var properties = new Dictionary<string, SchemaProperty>();
        var required   = new List<string>();

        foreach (var (name, (types, strings, count)) in stats)
        {
            if (count >= jsonSamples.Count * RequiredThreshold)
                required.Add(name);

            var jsonType = types.Contains(JsonValueKind.Number)                          ? "number"
                         : types.Contains(JsonValueKind.True) ||
                           types.Contains(JsonValueKind.False)                            ? "boolean"
                         : "string";

            var format = DetectFormat(strings);
            properties[name] = new SchemaProperty(jsonType, format);
        }

        return new InferredSchema(properties, required, jsonSamples.Count);
    }

    private static string? DetectFormat(List<string> samples)
    {
        if (samples.Count == 0) return null;

        if (MatchRate(samples, DateTimePattern) >= FormatThreshold) return "date-time";
        if (MatchRate(samples, EmailPattern)    >= FormatThreshold) return "email";
        if (MatchRate(samples, UriPattern)      >= FormatThreshold) return "uri";
        if (MatchRate(samples, UuidPattern)     >= FormatThreshold) return "uuid";

        return null;
    }

    private static double MatchRate(List<string> samples, Regex regex)
        => samples.Count(s => regex.IsMatch(s)) / (double)samples.Count;
}

// ──────────────────────────────────────────────────────────────────────────────
// Schema Evolution Advisor
// ──────────────────────────────────────────────────────────────────────────────

public class SchemaEvolutionAdvisor
{
    public EvolutionAdvice Analyse(InferredSchema oldSchema, InferredSchema newSchema, string subject)
    {
        var changes = new List<SchemaChange>();

        foreach (var name in oldSchema.Properties.Keys.Except(newSchema.Properties.Keys))
            changes.Add(new SchemaChange(name, ChangeType.Removed,
                WasRequired: oldSchema.Required.Contains(name)));

        foreach (var name in newSchema.Properties.Keys.Except(oldSchema.Properties.Keys))
            changes.Add(new SchemaChange(name, ChangeType.Added,
                IsRequired: newSchema.Required.Contains(name)));

        var level = changes.Any(c => c is { ChangeType: ChangeType.Removed, WasRequired: true })
            ? BreakingLevel.Major
            : changes.Count > 0
                ? BreakingLevel.Minor
                : BreakingLevel.None;

        return new EvolutionAdvice(subject, changes, level, GenerateMigrationCode(changes));
    }

    private static string GenerateMigrationCode(List<SchemaChange> changes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated migration — review before committing");
        sb.AppendLine($"// Generated: {DateTime.UtcNow:O}");
        sb.AppendLine();

        foreach (var c in changes.Where(x => x.ChangeType == ChangeType.Added))
        {
            var label = c.IsRequired ? "required" : "optional";
            sb.AppendLine($"// New {label} field: '{c.FieldName}'");
            sb.AppendLine($"public string? {ToPascalCase(c.FieldName)} {{ get; set; }} = null;");
            sb.AppendLine();
        }

        foreach (var c in changes.Where(x => x.ChangeType == ChangeType.Removed && x.WasRequired))
        {
            sb.AppendLine($"// BREAKING: Required field '{c.FieldName}' removed.");
            sb.AppendLine($"// Coordinate with ALL consumer teams before registering this schema.");
            sb.AppendLine();
        }

        return sb.Length > 100 ? sb.ToString() : "// No model changes required.\n";
    }

    private static string ToPascalCase(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}

// ──────────────────────────────────────────────────────────────────────────────
// Main Program
// ──────────────────────────────────────────────────────────────────────────────

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Surgewave Bootcamp: Lesson 10.6 - Schema Inference & AI Evolution ===");
        Console.WriteLine();

        await using var runtime = await SurgewaveRuntime.CreateBuilder()
            .WithPort(9092)
            .WithStorageMode(StorageMode.Memory)
            .WithAutoCreateTopics(true)
            .Build()
            .StartAsync();

        Console.WriteLine("Surgewave running on localhost:9092");
        Console.WriteLine();

        // ── Step 1: Produce realistic sample messages ──
        Console.WriteLine("--- Step 1: Producing 50 Orders ---");
        await using var producer = new SurgewaveProducer<string, string>(opt =>
            opt.BootstrapServers = "localhost:9092");

        for (int i = 1; i <= 50; i++)
        {
            var order = new
            {
                id         = i,
                email      = $"user{i}@example.com",
                createdAt  = DateTime.UtcNow.AddDays(-i).ToString("O"),
                orderId    = Guid.NewGuid().ToString(),
                total      = Math.Round(i * 9.99, 2),
                profileUrl = $"https://example.com/users/{i}"
            };
            await producer.ProduceAsync("orders", $"order-{i:D3}", JsonSerializer.Serialize(order));
        }
        Console.WriteLine("  50 orders produced (id, email, createdAt, orderId, total, profileUrl)");
        Console.WriteLine();

        // ── Step 2: Collect samples ──
        Console.WriteLine("--- Step 2: Collecting Samples for Inference ---");
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
        Console.WriteLine($"  Collected {samples.Count} samples");
        Console.WriteLine();

        // ── Step 3: Infer schema ──
        Console.WriteLine("--- Step 3: Inferring Schema ---");
        var engine   = new SimpleSchemaInferenceEngine();
        var inferred = engine.Infer(samples);

        Console.WriteLine($"  {inferred.Properties.Count} properties detected:");
        foreach (var (name, prop) in inferred.Properties)
        {
            var fmt = prop.Format != null ? $" [format: {prop.Format}]" : "";
            var req = inferred.Required.Contains(name) ? " *required*" : "";
            Console.WriteLine($"    {name}: {prop.Type}{fmt}{req}");
        }
        Console.WriteLine();

        // ── Step 4: Evolution — Scenario A (add optional field → Minor) ──
        Console.WriteLine("--- Step 4: Scenario A — Add Optional Field ---");
        var schemaWithPhone = inferred with
        {
            Properties = new(inferred.Properties)
            {
                ["phone"] = new SchemaProperty("string")
            }
        };

        var advisor = new SchemaEvolutionAdvisor();
        var adviceA = advisor.Analyse(inferred, schemaWithPhone, "orders-value");

        Console.WriteLine($"  Changes:       {adviceA.Changes.Count} (added: phone)");
        Console.WriteLine($"  Breaking level: {adviceA.BreakingLevel}");
        Console.WriteLine($"  Safe to register immediately: {adviceA.IsSafeToRegisterImmediately}");
        Console.WriteLine();
        Console.WriteLine("  Generated migration code:");
        foreach (var line in adviceA.MigrationCode.Split('\n'))
            Console.WriteLine($"  {line}");

        // ── Step 5: Evolution — Scenario B (remove required field → Major) ──
        Console.WriteLine("--- Step 5: Scenario B — Remove Required Field ---");
        var propsWithoutEmail = inferred.Properties
            .Where(p => p.Key != "email")
            .ToDictionary(p => p.Key, p => p.Value);

        var schemaWithoutEmail = inferred with { Properties = propsWithoutEmail };
        var adviceB = advisor.Analyse(inferred, schemaWithoutEmail, "orders-value");

        Console.WriteLine($"  Changes:        {adviceB.Changes.Count} (removed: email)");
        Console.WriteLine($"  Breaking level: {adviceB.BreakingLevel}  <-- requires coordination!");
        Console.WriteLine($"  Safe to register immediately: {adviceB.IsSafeToRegisterImmediately}");
        Console.WriteLine();
        Console.WriteLine("  Generated migration code:");
        foreach (var line in adviceB.MigrationCode.Split('\n'))
            Console.WriteLine($"  {line}");

        // ── Summary ──
        Console.WriteLine("--- Summary ---");
        Console.WriteLine("  Format detection results:");
        Console.WriteLine("    email      → format: email    (RFC 5322 pattern)");
        Console.WriteLine("    createdAt  → format: date-time (ISO 8601 pattern)");
        Console.WriteLine("    orderId    → format: uuid      (8-4-4-4-12 hex)");
        Console.WriteLine("    profileUrl → format: uri       (https:// prefix)");
        Console.WriteLine();
        Console.WriteLine("  Evolution classification:");
        Console.WriteLine("    Add optional field  → Minor (safe, no consumer coordination needed)");
        Console.WriteLine("    Remove required field → Major (coordinate all consumers first)");
        Console.WriteLine();
        Console.WriteLine("Lesson complete!");
    }
}

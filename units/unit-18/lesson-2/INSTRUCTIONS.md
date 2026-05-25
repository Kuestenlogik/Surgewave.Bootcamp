# Lesson 22.2: Instructions

Follow these step-by-step instructions to implement Ollama local LLM integration with Surgewave.

## Step 1: Create an Ollama Client

Build a client to communicate with the Ollama API:

```csharp
public class OllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _baseUrl = baseUrl;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, string model = "nomic-embed-text")
    {
        var request = new { model, prompt = text };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/embeddings", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);
        var embedding = parsed.RootElement
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        return embedding;
    }

    public async Task<string> GenerateAsync(string prompt, string model = "llama3")
    {
        var request = new { model, prompt, stream = false };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);
        return parsed.RootElement.GetProperty("response").GetString() ?? "";
    }
}
```

## Step 2: Check Ollama Availability

Verify Ollama is running before processing:

```csharp
async Task<bool> IsOllamaAvailable(OllamaClient client)
{
    try
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await httpClient.GetAsync("http://localhost:11434/api/tags");
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}

var ollamaClient = new OllamaClient();
if (!await IsOllamaAvailable(ollamaClient))
{
    Console.WriteLine("Ollama is not running. Using simulated embeddings.");
    // Fall back to simulated embeddings
}
```

## Step 3: Define Document Types for Local Processing

Create types for documents and enriched results:

```csharp
public record Document(
    string Id,
    string Title,
    string Content,
    DateTime Timestamp
);

public record EnrichedDocument(
    string Id,
    string Title,
    string Content,
    string Summary,
    float[] Embedding,
    DateTime ProcessedAt
);
```

## Step 4: Produce Documents to Surgewave

Stream documents through Surgewave for processing:

```csharp
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

var documents = new Document[]
{
    new("doc-1", "Data Privacy in AI",
        "Data privacy is a critical concern when using AI systems. Organizations must ensure that personal information is protected and that AI models are trained on properly anonymized data.",
        DateTime.UtcNow),
    new("doc-2", "Edge Computing Benefits",
        "Edge computing brings computation closer to data sources, reducing latency and bandwidth usage. This is particularly important for real-time AI applications.",
        DateTime.UtcNow),
    // Add more documents...
};

Console.WriteLine("\n--- Producing Documents ---");
foreach (var doc in documents)
{
    var json = JsonSerializer.Serialize(doc);
    await producer.ProduceAsync("documents", doc.Id, json);
    Console.WriteLine($"Produced: {doc.Title}");
}
```

## Step 5: Process with Local LLM

Consume documents and enrich with local AI:

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "ollama-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, "documents");

var enrichedDocs = new List<EnrichedDocument>();

Console.WriteLine("\n--- Processing with Ollama ---");
for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
    Console.WriteLine($"Processing: {doc.Title}");

    // Generate embedding locally
    var embedding = await ollamaClient.GenerateEmbeddingAsync(doc.Content);
    Console.WriteLine($"  Embedding: {embedding.Length} dimensions");

    // Generate summary locally
    var summaryPrompt = $"Summarize this text in one sentence:\n\n{doc.Content}";
    var summary = await ollamaClient.GenerateAsync(summaryPrompt);
    Console.WriteLine($"  Summary: {summary.Trim()}");

    var enriched = new EnrichedDocument(
        doc.Id,
        doc.Title,
        doc.Content,
        summary.Trim(),
        embedding,
        DateTime.UtcNow
    );

    enrichedDocs.Add(enriched);
}
```

## Step 6: Implement Streaming Responses

For long responses, stream the output:

```csharp
public async IAsyncEnumerable<string> StreamGenerateAsync(
    string prompt,
    string model = "llama3")
{
    var request = new { model, prompt, stream = true };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PostAsync(
        $"{_baseUrl}/api/generate",
        content,
        HttpCompletionOption.ResponseHeadersRead);

    await using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(line)) continue;

        var parsed = JsonDocument.Parse(line);
        var token = parsed.RootElement.GetProperty("response").GetString();
        if (!string.IsNullOrEmpty(token))
        {
            yield return token;
        }

        if (parsed.RootElement.TryGetProperty("done", out var done) && done.GetBoolean())
        {
            break;
        }
    }
}

// Usage
Console.Write("Streaming response: ");
await foreach (var token in ollamaClient.StreamGenerateAsync("Explain quantum computing"))
{
    Console.Write(token);
}
Console.WriteLine();
```

## Step 7: Compare Local vs Cloud Performance

Track and compare performance metrics:

```csharp
var metrics = new Dictionary<string, List<double>>();

async Task<(T Result, double ElapsedMs)> MeasureAsync<T>(Func<Task<T>> operation)
{
    var sw = Stopwatch.StartNew();
    var result = await operation();
    sw.Stop();
    return (result, sw.Elapsed.TotalMilliseconds);
}

// Measure local embedding generation
var (embedding, localMs) = await MeasureAsync(() =>
    ollamaClient.GenerateEmbeddingAsync("Test text for embedding"));
Console.WriteLine($"Local embedding: {localMs:F0}ms ({embedding.Length} dims)");

// Display comparison
Console.WriteLine("\n--- Performance Comparison ---");
Console.WriteLine($"Local Ollama (nomic-embed-text): ~{localMs:F0}ms per embedding");
Console.WriteLine("Cloud OpenAI (text-embedding-3-small): ~50-200ms per embedding");
Console.WriteLine("\nNote: Local speed depends on hardware (GPU vs CPU)");
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Ollama client with embedding and generation
- Document streaming through Surgewave
- Local summarization with LLM
- Streaming responses
- Performance metrics

## Sample Output

```
=== Surgewave Bootcamp: Lesson 22.2 - Ollama Local LLM ===

Surgewave broker started on port 12345
Ollama available: Yes (using simulated for demo)

--- Producing Documents ---
Produced: Data Privacy in AI
Produced: Edge Computing Benefits
Produced: Machine Learning Operations
Produced: Distributed Systems Architecture

--- Processing with Ollama ---
Processing: Data Privacy in AI
  Embedding: 768 dimensions
  Summary: Data privacy requires protecting personal information in AI systems.
Processing: Edge Computing Benefits
  Embedding: 768 dimensions
  Summary: Edge computing reduces latency by processing data closer to sources.
Processing: Machine Learning Operations
  Embedding: 768 dimensions
  Summary: MLOps streamlines the deployment and management of ML models.
Processing: Distributed Systems Architecture
  Embedding: 768 dimensions
  Summary: Distributed systems enable scalability through multiple machines.

--- Semantic Search (Local Embeddings) ---
Query: "How to protect user data?"
  [0.8923] Data Privacy in AI
  [0.6234] Edge Computing Benefits
  [0.5012] Machine Learning Operations

--- Performance Metrics ---
Average embedding time: 45ms
Average summarization time: 1200ms
Total documents processed: 4

Lesson complete!
```

## Knowledge Check

1. What are the main advantages of using local LLMs over cloud APIs?
2. What is the default Ollama port and API endpoint for embeddings?
3. Why might you choose nomic-embed-text over all-minilm for embeddings?
4. How does streaming responses improve user experience?
5. What hardware considerations affect local LLM performance?

---

**Next:** [Lesson 22.3: Vector Databases - Qdrant](../lesson-3/README.md)

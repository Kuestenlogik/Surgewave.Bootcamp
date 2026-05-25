// Surgewave Bootcamp - Unit 22, Lesson 2: Ollama Local LLM Integration
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.2 - Ollama Local LLM ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "documents";

// Step 1 - Check Ollama availability and create client
async Task<bool> IsOllamaAvailable()
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

var useRealOllama = await IsOllamaAvailable();
Console.WriteLine($"Ollama available: {(useRealOllama ? "Yes" : "No (using simulated)")}");

// Simulated Ollama functions for when Ollama is not running
float[] GenerateSimulatedEmbedding(string text)
{
    var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var embedding = new float[768]; // nomic-embed-text dimensions

    foreach (var word in words)
    {
        var hash = word.GetHashCode();
        var rng = new Random(hash);
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] += (float)(rng.NextDouble() * 2 - 1) / words.Length;
        }
    }

    var magnitude = Math.Sqrt(embedding.Sum(x => (double)x * x));
    if (magnitude > 0)
    {
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] /= (float)magnitude;
        }
    }

    return embedding;
}

string GenerateSimulatedSummary(string content)
{
    // Extract key phrases for a simple summary
    var sentences = content.Split('.', StringSplitOptions.RemoveEmptyEntries);
    if (sentences.Length == 0) return "No content to summarize.";

    var firstSentence = sentences[0].Trim();
    return firstSentence.Length > 80 ? firstSentence[..80] + "..." : firstSentence + ".";
}

// Real Ollama client (used when Ollama is running)
async Task<float[]> GenerateOllamaEmbeddingAsync(string text)
{
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    var request = new { model = "nomic-embed-text", prompt = text };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync("http://localhost:11434/api/embeddings", content);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadAsStringAsync();
    var parsed = JsonDocument.Parse(result);
    return parsed.RootElement.GetProperty("embedding")
        .EnumerateArray()
        .Select(e => e.GetSingle())
        .ToArray();
}

async Task<string> GenerateOllamaSummaryAsync(string text)
{
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    var prompt = $"Summarize this text in one short sentence:\n\n{text}";
    var request = new { model = "llama3", prompt, stream = false };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync("http://localhost:11434/api/generate", content);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadAsStringAsync();
    var parsed = JsonDocument.Parse(result);
    return parsed.RootElement.GetProperty("response").GetString()?.Trim() ?? "";
}

// Unified functions that use real or simulated based on availability
async Task<float[]> GenerateEmbeddingAsync(string text)
{
    if (useRealOllama)
        return await GenerateOllamaEmbeddingAsync(text);
    return GenerateSimulatedEmbedding(text);
}

async Task<string> GenerateSummaryAsync(string text)
{
    if (useRealOllama)
        return await GenerateOllamaSummaryAsync(text);
    return GenerateSimulatedSummary(text);
}

// Step 2 - Create producer and send documents
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

var documents = new Document[]
{
    new("doc-1", "Data Privacy in AI",
        "Data privacy is a critical concern when using AI systems. Organizations must ensure that personal information is protected and that AI models are trained on properly anonymized data to prevent unauthorized access.",
        DateTime.UtcNow),
    new("doc-2", "Edge Computing Benefits",
        "Edge computing brings computation closer to data sources, reducing latency and bandwidth usage. This is particularly important for real-time AI applications in IoT and mobile devices.",
        DateTime.UtcNow),
    new("doc-3", "Machine Learning Operations",
        "MLOps combines machine learning with DevOps practices to streamline the deployment, monitoring, and management of ML models in production environments at scale.",
        DateTime.UtcNow),
    new("doc-4", "Distributed Systems Architecture",
        "Distributed systems enable horizontal scalability by spreading workloads across multiple machines. They require careful consideration of consistency, availability, and partition tolerance.",
        DateTime.UtcNow),
    new("doc-5", "Natural Language Understanding",
        "Natural language understanding enables computers to comprehend human language, extract meaning, and respond appropriately. It forms the foundation of chatbots and virtual assistants.",
        DateTime.UtcNow),
};

Console.WriteLine("\n--- Producing Documents ---");
foreach (var doc in documents)
{
    var json = JsonSerializer.Serialize(doc);
    await producer.ProduceAsync(topic, doc.Id, json);
    Console.WriteLine($"Produced: {doc.Title}");
}

// Step 3 - Create consumer and process with local LLM
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "ollama-processor";
    opt.EnableAutoCommit = false;
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

var enrichedDocs = new List<EnrichedDocument>();
var embeddingTimes = new List<double>();
var summaryTimes = new List<double>();

Console.WriteLine("\n--- Processing with Ollama ---");
for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
    Console.WriteLine($"Processing: {doc.Title}");

    // Generate embedding locally with timing
    var sw = Stopwatch.StartNew();
    var embedding = await GenerateEmbeddingAsync(doc.Content);
    embeddingTimes.Add(sw.Elapsed.TotalMilliseconds);
    Console.WriteLine($"  Embedding: {embedding.Length} dimensions ({sw.ElapsedMilliseconds}ms)");

    // Generate summary locally with timing
    sw.Restart();
    var summary = await GenerateSummaryAsync(doc.Content);
    summaryTimes.Add(sw.Elapsed.TotalMilliseconds);
    Console.WriteLine($"  Summary: {summary}");

    var enriched = new EnrichedDocument(
        doc.Id,
        doc.Title,
        doc.Content,
        summary,
        embedding,
        DateTime.UtcNow
    );

    enrichedDocs.Add(enriched);
    await consumer.CommitAsync(result);
}

// Step 4 - Cosine similarity function
static double CosineSimilarity(float[] a, float[] b)
{
    if (a.Length != b.Length) return 0;

    double dotProduct = 0, normA = 0, normB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dotProduct += a[i] * b[i];
        normA += a[i] * a[i];
        normB += b[i] * b[i];
    }

    if (normA == 0 || normB == 0) return 0;
    return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
}

// Step 5 - Semantic search with local embeddings
Console.WriteLine("\n--- Semantic Search (Local Embeddings) ---");

var searchQueries = new[]
{
    "How to protect user data?",
    "Running AI on local devices",
    "Managing ML models in production"
};

foreach (var query in searchQueries)
{
    Console.WriteLine($"\nQuery: \"{query}\"");

    var queryEmbedding = await GenerateEmbeddingAsync(query);
    var results = enrichedDocs
        .Select(d => (Doc: d, Score: CosineSimilarity(queryEmbedding, d.Embedding)))
        .OrderByDescending(x => x.Score)
        .Take(3);

    foreach (var (doc, score) in results)
    {
        Console.WriteLine($"  [{score:F4}] {doc.Title}");
    }
}

// Step 6 - Display performance metrics
Console.WriteLine("\n--- Performance Metrics ---");
Console.WriteLine($"Total documents processed: {enrichedDocs.Count}");
Console.WriteLine($"Embedding dimensions: {enrichedDocs[0].Embedding.Length}");
Console.WriteLine($"Average embedding time: {embeddingTimes.Average():F0}ms");
Console.WriteLine($"Average summary time: {summaryTimes.Average():F0}ms");
Console.WriteLine($"Processing mode: {(useRealOllama ? "Real Ollama" : "Simulated")}");

// Step 7 - Display summaries
Console.WriteLine("\n--- Generated Summaries ---");
foreach (var doc in enrichedDocs)
{
    Console.WriteLine($"{doc.Title}:");
    Console.WriteLine($"  {doc.Summary}");
}

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Local LLMs keep data private - nothing leaves your network");
Console.WriteLine("2. Ollama makes running local models simple (pull and run)");
Console.WriteLine("3. nomic-embed-text produces 768-dimension embeddings locally");
Console.WriteLine("4. Local inference speed depends on hardware (GPU vs CPU)");
Console.WriteLine("5. Consider local LLMs for sensitive data and offline scenarios");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types
public record Document(string Id, string Title, string Content, DateTime Timestamp);
public record EnrichedDocument(string Id, string Title, string Content, string Summary, float[] Embedding, DateTime ProcessedAt);

// Surgewave Bootcamp - Unit 22, Lesson 2: Ollama Local LLM Integration
//
// In this lesson, you'll learn:
// - How to use Ollama for local LLM inference
// - Generating embeddings with local models
// - Text summarization without cloud APIs
// - Privacy-preserving AI processing
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

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

// TODO: Step 1 - Create an Ollama client
// For this demo, we'll use simulated responses if Ollama is not running.
// public class OllamaClient
// {
//     private readonly HttpClient _httpClient;
//     private readonly string _baseUrl;
//
//     public OllamaClient(string baseUrl = "http://localhost:11434")
//     {
//         _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
//         _baseUrl = baseUrl;
//     }
//
//     public async Task<float[]> GenerateEmbeddingAsync(string text, string model = "nomic-embed-text")
//     {
//         var request = new { model, prompt = text };
//         var json = JsonSerializer.Serialize(request);
//         var content = new StringContent(json, Encoding.UTF8, "application/json");
//         var response = await _httpClient.PostAsync($"{_baseUrl}/api/embeddings", content);
//         response.EnsureSuccessStatusCode();
//         var result = await response.Content.ReadAsStringAsync();
//         var parsed = JsonDocument.Parse(result);
//         return parsed.RootElement.GetProperty("embedding")
//             .EnumerateArray().Select(e => e.GetSingle()).ToArray();
//     }
//
//     public async Task<string> GenerateAsync(string prompt, string model = "llama3")
//     {
//         var request = new { model, prompt, stream = false };
//         var json = JsonSerializer.Serialize(request);
//         var content = new StringContent(json, Encoding.UTF8, "application/json");
//         var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
//         response.EnsureSuccessStatusCode();
//         var result = await response.Content.ReadAsStringAsync();
//         var parsed = JsonDocument.Parse(result);
//         return parsed.RootElement.GetProperty("response").GetString() ?? "";
//     }
// }

// Simulated Ollama client for demo (replace with real client if Ollama is running)
bool useRealOllama = false; // Set to true if Ollama is running

// Simulated embedding generator (768 dimensions like nomic-embed-text)
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

// Simulated summarization
string GenerateSimulatedSummary(string content)
{
    // Simple extractive summary - take first sentence
    var firstSentence = content.Split('.').FirstOrDefault()?.Trim() ?? content;
    return firstSentence.Length > 100 ? firstSentence[..100] + "..." : firstSentence + ".";
}

// TODO: Step 2 - Check Ollama availability
// async Task<bool> IsOllamaAvailable()
// {
//     try
//     {
//         using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
//         var response = await httpClient.GetAsync("http://localhost:11434/api/tags");
//         return response.IsSuccessStatusCode;
//     }
//     catch
//     {
//         return false;
//     }
// }
//
// useRealOllama = await IsOllamaAvailable();
Console.WriteLine($"Ollama available: {(useRealOllama ? "Yes" : "No (using simulated)")}");

// TODO: Step 3 - Define document types
// public record Document(string Id, string Title, string Content, DateTime Timestamp);
// public record EnrichedDocument(string Id, string Title, string Content, string Summary, float[] Embedding, DateTime ProcessedAt);

// TODO: Step 4 - Produce documents to Surgewave
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// var documents = new Document[]
// {
//     new("doc-1", "Data Privacy in AI",
//         "Data privacy is a critical concern when using AI systems. Organizations must ensure that personal information is protected and that AI models are trained on properly anonymized data.",
//         DateTime.UtcNow),
//     // Add more documents...
// };
//
// Console.WriteLine("\n--- Producing Documents ---");
// foreach (var doc in documents)
// {
//     var json = JsonSerializer.Serialize(doc);
//     await producer.ProduceAsync(topic, doc.Id, json);
//     Console.WriteLine($"Produced: {doc.Title}");
// }

// TODO: Step 5 - Create consumer and process with local LLM
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
//     opt.GroupId = "ollama-processor";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// await consumer.SubscribeAsync(default, topic);
//
// var enrichedDocs = new List<EnrichedDocument>();
//
// Console.WriteLine("\n--- Processing with Ollama ---");
// for (int i = 0; i < documents.Length; i++)
// {
//     var result = await consumer.ConsumeAsync();
//     if (result == null) continue;
//
//     var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
//     Console.WriteLine($"Processing: {doc.Title}");
//
//     // Generate embedding locally
//     var embedding = GenerateSimulatedEmbedding(doc.Content);
//     Console.WriteLine($"  Embedding: {embedding.Length} dimensions");
//
//     // Generate summary locally
//     var summary = GenerateSimulatedSummary(doc.Content);
//     Console.WriteLine($"  Summary: {summary}");
//
//     var enriched = new EnrichedDocument(
//         doc.Id, doc.Title, doc.Content, summary, embedding, DateTime.UtcNow);
//     enrichedDocs.Add(enriched);
// }

// TODO: Step 6 - Implement semantic search with local embeddings
// Console.WriteLine("\n--- Semantic Search (Local Embeddings) ---");
// var query = "How to protect user data?";
// Console.WriteLine($"Query: \"{query}\"");
//
// var queryEmbedding = GenerateSimulatedEmbedding(query);
// var results = enrichedDocs
//     .Select(d => (Doc: d, Score: CosineSimilarity(queryEmbedding, d.Embedding)))
//     .OrderByDescending(x => x.Score)
//     .Take(3);
//
// foreach (var (doc, score) in results)
// {
//     Console.WriteLine($"  [{score:F4}] {doc.Title}");
// }

// TODO: Step 7 - Display performance metrics
// Console.WriteLine("\n--- Performance Metrics ---");
// Console.WriteLine($"Total documents processed: {enrichedDocs.Count}");
// Console.WriteLine($"Embedding dimensions: 768 (nomic-embed-text)");
// Console.WriteLine($"Processing mode: {(useRealOllama ? "Real Ollama" : "Simulated")}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Local LLMs keep data private - nothing leaves your network");
Console.WriteLine("2. Ollama makes running local models simple (pull and run)");
Console.WriteLine("3. nomic-embed-text produces 768-dimension embeddings locally");
Console.WriteLine("4. Local inference speed depends on hardware (GPU vs CPU)");
Console.WriteLine("5. Consider local LLMs for sensitive data and offline scenarios");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Cosine similarity helper
static double CosineSimilarity(float[] a, float[] b)
{
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

// Record types (uncomment when implementing)
// public record Document(string Id, string Title, string Content, DateTime Timestamp);
// public record EnrichedDocument(string Id, string Title, string Content, string Summary, float[] Embedding, DateTime ProcessedAt);

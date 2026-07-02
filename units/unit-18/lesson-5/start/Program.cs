// Surgewave Bootcamp - Unit 22, Lesson 5: Semantic Search Implementation
//
// In this lesson, you'll learn:
// - Building a complete semantic search system
// - Real-time document indexing
// - Search query processing with Surgewave
// - Hybrid search combining semantic and keyword matching
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.5 - Semantic Search ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var documentTopic = "documents";
var queryTopic = "search-queries";
var resultsTopic = "search-results";
var vectorDimensions = 1536;

// Simulated embedding generator
float[] GenerateEmbedding(string text)
{
    var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var embedding = new float[vectorDimensions];

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

// Cosine similarity
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

// TODO: Step 1 - Define search domain model
// public record SearchableDocument(string Id, string Title, string Content, string Category, DateTime CreatedAt);
// public record IndexedDocument(string Id, string Title, string Content, string Category, DateTime CreatedAt, float[] Embedding);
// public record SearchQuery(string Id, string Query, string? Category, int Limit, double MinScore);
// public record SearchResult(string DocumentId, string Title, string Snippet, double Score, string Category);
// public record SearchResponse(string QueryId, string Query, List<SearchResult> Results, int TotalMatches, double SearchTimeMs);

// TODO: Step 2 - Create the search index
// var searchIndex = new Dictionary<string, IndexedDocument>();
//
// void IndexDocument(IndexedDocument doc)
// {
//     searchIndex[doc.Id] = doc;
// }
//
// List<SearchResult> Search(float[] queryEmbedding, int limit = 10, double minScore = 0.5, string? category = null)
// {
//     return searchIndex.Values
//         .Where(d => category == null || d.Category == category)
//         .Select(d => new { Doc = d, Score = CosineSimilarity(queryEmbedding, d.Embedding) })
//         .Where(x => x.Score >= minScore)
//         .OrderByDescending(x => x.Score)
//         .Take(limit)
//         .Select(x => new SearchResult(x.Doc.Id, x.Doc.Title, CreateSnippet(x.Doc.Content), x.Score, x.Doc.Category))
//         .ToList();
// }
//
// string CreateSnippet(string content, int maxLength = 100)
// {
//     if (content.Length <= maxLength) return content;
//     return content[..maxLength].TrimEnd() + "...";
// }

// TODO: Step 3 - Create document indexing pipeline
// var documents = new SearchableDocument[]
// {
//     new("doc-1", "Introduction to Machine Learning",
//         "Machine learning is a subset of AI that enables computers to learn from data without explicit programming.",
//         "technology", DateTime.UtcNow),
//     // Add more documents...
// };
//
// await using var docProducer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// Console.WriteLine("\n--- Indexing Documents ---");
// foreach (var doc in documents)
// {
//     await docProducer.ProduceAsync(documentTopic, doc.Id, JsonSerializer.Serialize(doc));
// }

// TODO: Step 4 - Consume and index documents
// await using var indexConsumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
//     opt.GroupId = "search-indexer";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// await indexConsumer.SubscribeAsync(default, documentTopic);
//
// for (int i = 0; i < documents.Length; i++)
// {
//     var result = await indexConsumer.ConsumeAsync();
//     if (result == null) continue;
//
//     var doc = JsonSerializer.Deserialize<SearchableDocument>(result.Value)!;
//     var embedding = GenerateEmbedding(doc.Content);
//
//     IndexDocument(new IndexedDocument(doc.Id, doc.Title, doc.Content, doc.Category, doc.CreatedAt, embedding));
//     Console.WriteLine($"Indexed: {doc.Title}");
// }

// TODO: Step 5 - Process search queries
// Console.WriteLine("\n--- Processing Search Queries ---");
// var queries = new[]
// {
//     "How do neural networks learn?",
//     "cloud infrastructure",
//     "data analysis techniques"
// };
//
// foreach (var queryText in queries)
// {
//     var sw = Stopwatch.StartNew();
//     var queryEmbedding = GenerateEmbedding(queryText);
//     var results = Search(queryEmbedding, limit: 3, minScore: 0.3);
//     sw.Stop();
//
//     Console.WriteLine($"\nQuery: \"{queryText}\"");
//     Console.WriteLine($"Results ({results.Count} found in {sw.ElapsedMilliseconds}ms):");
//     foreach (var result in results)
//     {
//         Console.WriteLine($"  [{result.Score:F4}] {result.Title}");
//         Console.WriteLine($"    \"{result.Snippet}\"");
//     }
// }

// TODO: Step 6 - Track search metrics
// var latencies = new List<double>();
// // Record latencies during search
// // Report P50, P95, P99 latencies

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Semantic search finds documents by meaning, not just keywords");
Console.WriteLine("2. Hybrid search combines semantic and keyword for best results");
Console.WriteLine("3. Real-time indexing with Surgewave enables live document updates");
Console.WriteLine("4. Track search latency metrics (P50, P95, P99) for performance");
Console.WriteLine("5. Filter by minimum score to avoid irrelevant results");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types (uncomment when implementing)
// public record SearchableDocument(string Id, string Title, string Content, string Category, DateTime CreatedAt);
// public record IndexedDocument(string Id, string Title, string Content, string Category, DateTime CreatedAt, float[] Embedding);
// public record SearchQuery(string Id, string Query, string? Category, int Limit, double MinScore);
// public record SearchResult(string DocumentId, string Title, string Snippet, double Score, string Category);
// public record SearchResponse(string QueryId, string Query, List<SearchResult> Results, int TotalMatches, double SearchTimeMs);

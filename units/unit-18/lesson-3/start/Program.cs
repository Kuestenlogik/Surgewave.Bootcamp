// Surgewave Bootcamp - Unit 22, Lesson 3: Qdrant Vector Database
//
// In this lesson, you'll learn:
// - How to store embeddings in a vector database
// - Creating collections and indexing documents
// - Performing vector similarity search
// - Filtering search results with metadata
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.3 - Qdrant Vector Database ===");
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
var collectionName = "documents";
var vectorDimensions = 1536;

// TODO: Step 1 - Create a Qdrant client
// For this demo, we'll use a simulated in-memory vector store
// public class QdrantClient
// {
//     private readonly HttpClient _httpClient;
//     private readonly string _baseUrl;
//
//     public QdrantClient(string baseUrl = "http://localhost:6333") { ... }
//
//     public async Task<bool> CreateCollectionAsync(string name, int vectorSize, string distance = "Cosine") { ... }
//     public async Task<bool> UpsertPointsAsync(string collection, IEnumerable<QdrantPoint> points) { ... }
//     public async Task<List<QdrantSearchResult>> SearchAsync(string collection, float[] vector, int limit = 10) { ... }
// }

// Simulated in-memory vector store (replace with real Qdrant when available)
var vectorStore = new Dictionary<string, (float[] Vector, Dictionary<string, object> Payload)>();

void UpsertPoint(string id, float[] vector, Dictionary<string, object> payload)
{
    vectorStore[id] = (vector, payload);
}

List<(string Id, float Score, Dictionary<string, object> Payload)> Search(
    float[] queryVector,
    int limit = 5,
    Func<Dictionary<string, object>, bool>? filter = null)
{
    return vectorStore
        .Where(kv => filter == null || filter(kv.Value.Payload))
        .Select(kv => (Id: kv.Key, Score: (float)CosineSimilarity(queryVector, kv.Value.Vector), Payload: kv.Value.Payload))
        .OrderByDescending(x => x.Score)
        .Take(limit)
        .ToList();
}

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

// TODO: Step 2 - Check Qdrant availability
// bool qdrantAvailable = await IsQdrantAvailable();
Console.WriteLine($"Qdrant available: No (using simulated in-memory store)");

// TODO: Step 3 - Create collection
Console.WriteLine("\n--- Creating Collection ---");
Console.WriteLine($"Collection: {collectionName}");
Console.WriteLine($"Vector dimensions: {vectorDimensions}");
Console.WriteLine($"Distance metric: Cosine");

// TODO: Step 4 - Define document types and produce to Surgewave
// public record Document(string Id, string Title, string Content, string Category, DateTime Timestamp);

// var documents = new Document[]
// {
//     new("doc-1", "AI in Healthcare", "Artificial intelligence is transforming healthcare...", "tech", DateTime.UtcNow),
//     // Add more documents...
// };
//
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// foreach (var doc in documents)
// {
//     await producer.ProduceAsync(topic, doc.Id, JsonSerializer.Serialize(doc));
// }

// TODO: Step 5 - Consume and index documents
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
//     opt.GroupId = "qdrant-indexer";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// await consumer.SubscribeAsync(default, topic);
//
// Console.WriteLine("\n--- Indexing Documents ---");
// for (int i = 0; i < documents.Length; i++)
// {
//     var result = await consumer.ConsumeAsync();
//     if (result == null) continue;
//
//     var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
//     var embedding = GenerateEmbedding(doc.Content);
//
//     UpsertPoint(doc.Id, embedding, new Dictionary<string, object>
//     {
//         ["title"] = doc.Title,
//         ["category"] = doc.Category,
//         ["timestamp"] = doc.Timestamp.ToString("O")
//     });
//
//     Console.WriteLine($"Indexed: {doc.Title}");
// }

// TODO: Step 6 - Perform vector search
// Console.WriteLine("\n--- Vector Search ---");
// var query = "How is AI used in medical diagnosis?";
// Console.WriteLine($"Query: \"{query}\"");
//
// var queryEmbedding = GenerateEmbedding(query);
// var results = Search(queryEmbedding, limit: 3);
//
// Console.WriteLine("Results:");
// foreach (var (id, score, payload) in results)
// {
//     Console.WriteLine($"  [{score:F4}] {payload["title"]}");
// }

// TODO: Step 7 - Perform filtered search
// Console.WriteLine("\n--- Filtered Vector Search ---");
// Console.WriteLine("Filter: category = 'tech'");
//
// var filteredResults = Search(
//     queryEmbedding,
//     limit: 3,
//     filter: payload => payload["category"].ToString() == "tech");
//
// Console.WriteLine("Results:");
// foreach (var (id, score, payload) in filteredResults)
// {
//     Console.WriteLine($"  [{score:F4}] {payload["title"]} ({payload["category"]})");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Vector databases enable fast similarity search over embeddings");
Console.WriteLine("2. HNSW index provides O(log n) search complexity");
Console.WriteLine("3. Cosine distance is ideal for normalized text embeddings");
Console.WriteLine("4. Payloads store metadata alongside vectors for filtering");
Console.WriteLine("5. Batch upserts are more efficient than single inserts");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types (uncomment when implementing)
// public record Document(string Id, string Title, string Content, string Category, DateTime Timestamp);
// public record QdrantPoint(string Id, float[] Vector, Dictionary<string, object> Payload);
// public record QdrantSearchResult(string Id, float Score, JsonElement Payload);

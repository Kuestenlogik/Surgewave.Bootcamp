// Surgewave Bootcamp - Unit 22, Lesson 3: Qdrant Vector Database
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.3 - Qdrant Vector Database ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var topic = "documents";
var collectionName = "documents";
var vectorDimensions = 1536;

// Step 1 - Simulated in-memory vector store (replace with real Qdrant client)
var vectorStore = new Dictionary<string, (float[] Vector, Dictionary<string, object> Payload)>();

void UpsertPoint(string id, float[] vector, Dictionary<string, object> payload)
{
    vectorStore[id] = (vector, payload);
    Console.WriteLine($"Indexed: {payload["title"]}");
}

void UpsertBatch(IEnumerable<(string Id, float[] Vector, Dictionary<string, object> Payload)> points)
{
    foreach (var (id, vector, payload) in points)
    {
        vectorStore[id] = (vector, payload);
    }
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

// Step 2 - Check Qdrant availability (simulated)
Console.WriteLine($"Qdrant available: No (using simulated in-memory store)");

// Step 3 - Create collection
Console.WriteLine("\n--- Creating Collection ---");
Console.WriteLine($"Collection: {collectionName}");
Console.WriteLine($"Vector dimensions: {vectorDimensions}");
Console.WriteLine($"Distance metric: Cosine");

// Step 4 - Define documents and produce to Surgewave
var documents = new Document[]
{
    new("doc-1", "AI in Healthcare",
        "Artificial intelligence is transforming healthcare through improved diagnostics and personalized treatment plans using machine learning algorithms.",
        "tech", DateTime.UtcNow),
    new("doc-2", "Climate Change Research",
        "Scientists are using machine learning and data analysis to model climate patterns and predict future environmental changes on our planet.",
        "science", DateTime.UtcNow),
    new("doc-3", "Quantum Computing Advances",
        "Quantum computers are achieving new milestones in processing power, enabling complex calculations that classical computers cannot perform efficiently.",
        "tech", DateTime.UtcNow),
    new("doc-4", "Renewable Energy Solutions",
        "Solar and wind power technologies are becoming more efficient and cost-effective, driving the transition to sustainable energy sources worldwide.",
        "science", DateTime.UtcNow),
    new("doc-5", "Space Exploration Technology",
        "New spacecraft designs and propulsion systems are making deep space exploration more feasible, with missions planned to Mars and beyond.",
        "science", DateTime.UtcNow),
    new("doc-6", "Neural Network Architectures",
        "Deep learning models with transformer architectures are achieving breakthrough results in natural language processing and computer vision tasks.",
        "tech", DateTime.UtcNow),
};

await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("\n--- Producing Documents to Surgewave ---");
foreach (var doc in documents)
{
    await producer.ProduceAsync(topic, doc.Id, JsonSerializer.Serialize(doc));
    Console.WriteLine($"Produced: {doc.Title}");
}

// Step 5 - Consume and index documents
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "qdrant-indexer";
    opt.EnableAutoCommit = false;
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

Console.WriteLine("\n--- Indexing Documents ---");
for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
    var embedding = GenerateEmbedding(doc.Content);

    UpsertPoint(doc.Id, embedding, new Dictionary<string, object>
    {
        ["title"] = doc.Title,
        ["content"] = doc.Content,
        ["category"] = doc.Category,
        ["timestamp"] = doc.Timestamp.ToString("O")
    });

    await consumer.CommitAsync(result);
}

Console.WriteLine($"Total documents indexed: {vectorStore.Count}");

// Step 6 - Perform vector search
Console.WriteLine("\n--- Vector Search ---");
var searchQueries = new[]
{
    "How is AI used in medical diagnosis?",
    "Sustainable power generation methods",
    "Advanced computing technologies"
};

foreach (var query in searchQueries)
{
    Console.WriteLine($"\nQuery: \"{query}\"");
    var queryEmbedding = GenerateEmbedding(query);
    var results = Search(queryEmbedding, limit: 3);

    Console.WriteLine("Results:");
    foreach (var (id, score, payload) in results)
    {
        Console.WriteLine($"  [{score:F4}] {payload["title"]}");
    }
}

// Step 7 - Perform filtered search
Console.WriteLine("\n--- Filtered Vector Search ---");

var filterQuery = "machine learning applications";
Console.WriteLine($"Query: \"{filterQuery}\"");

var filterQueryEmbedding = GenerateEmbedding(filterQuery);

// Filter by category = "tech"
Console.WriteLine("\nFilter: category = 'tech'");
var techResults = Search(
    filterQueryEmbedding,
    limit: 3,
    filter: payload => payload["category"].ToString() == "tech");

Console.WriteLine("Results:");
foreach (var (id, score, payload) in techResults)
{
    Console.WriteLine($"  [{score:F4}] {payload["title"]} ({payload["category"]})");
}

// Filter by category = "science"
Console.WriteLine("\nFilter: category = 'science'");
var scienceResults = Search(
    filterQueryEmbedding,
    limit: 3,
    filter: payload => payload["category"].ToString() == "science");

Console.WriteLine("Results:");
foreach (var (id, score, payload) in scienceResults)
{
    Console.WriteLine($"  [{score:F4}] {payload["title"]} ({payload["category"]})");
}

// Step 8 - Display statistics
Console.WriteLine("\n--- Collection Statistics ---");
Console.WriteLine($"Total documents: {vectorStore.Count}");
Console.WriteLine($"Vector dimensions: {vectorDimensions}");
var categories = vectorStore.Values
    .GroupBy(v => v.Payload["category"].ToString())
    .Select(g => $"{g.Key}: {g.Count()}");
Console.WriteLine($"Categories: {string.Join(", ", categories)}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Vector databases enable fast similarity search over embeddings");
Console.WriteLine("2. HNSW index provides O(log n) search complexity");
Console.WriteLine("3. Cosine distance is ideal for normalized text embeddings");
Console.WriteLine("4. Payloads store metadata alongside vectors for filtering");
Console.WriteLine("5. Batch upserts are more efficient than single inserts");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types
public record Document(string Id, string Title, string Content, string Category, DateTime Timestamp);

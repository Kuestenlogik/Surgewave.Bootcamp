# Lesson 22.3: Instructions

Follow these step-by-step instructions to implement vector database operations with Qdrant.

## Step 1: Create a Qdrant Client

Build a client to interact with the Qdrant API:

```csharp
public class QdrantClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public QdrantClient(string baseUrl = "http://localhost:6333")
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _baseUrl = baseUrl;
    }

    public async Task<bool> CreateCollectionAsync(string name, int vectorSize, string distance = "Cosine")
    {
        var request = new
        {
            vectors = new { size = vectorSize, distance }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"{_baseUrl}/collections/{name}", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpsertPointsAsync(string collection, IEnumerable<QdrantPoint> points)
    {
        var request = new { points = points.Select(p => new
        {
            id = p.Id,
            vector = p.Vector,
            payload = p.Payload
        })};

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(
            $"{_baseUrl}/collections/{collection}/points",
            content);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<QdrantSearchResult>> SearchAsync(
        string collection,
        float[] vector,
        int limit = 10,
        Dictionary<string, object>? filter = null)
    {
        var request = new Dictionary<string, object>
        {
            ["vector"] = vector,
            ["limit"] = limit,
            ["with_payload"] = true
        };

        if (filter != null)
        {
            request["filter"] = filter;
        }

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/collections/{collection}/points/search",
            content);

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);

        return parsed.RootElement.GetProperty("result")
            .EnumerateArray()
            .Select(r => new QdrantSearchResult(
                r.GetProperty("id").GetString() ?? "",
                r.GetProperty("score").GetSingle(),
                r.GetProperty("payload")))
            .ToList();
    }
}

public record QdrantPoint(string Id, float[] Vector, Dictionary<string, object> Payload);
public record QdrantSearchResult(string Id, float Score, JsonElement Payload);
```

## Step 2: Check Qdrant Availability

Verify Qdrant is running:

```csharp
async Task<bool> IsQdrantAvailable(QdrantClient client)
{
    try
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await httpClient.GetAsync("http://localhost:6333/collections");
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}
```

## Step 3: Create a Collection

Set up a collection for your vectors:

```csharp
var qdrantClient = new QdrantClient();
var collectionName = "documents";
var vectorDimensions = 1536; // Match your embedding model

// Create collection (idempotent)
var created = await qdrantClient.CreateCollectionAsync(
    collectionName,
    vectorDimensions,
    "Cosine");

Console.WriteLine($"Collection '{collectionName}' created: {created}");
```

## Step 4: Process Documents and Store in Qdrant

Stream documents through Surgewave and store embeddings:

```csharp
// Producer: Send documents to Surgewave
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

var documents = new[]
{
    new Document("doc-1", "AI in Healthcare",
        "Artificial intelligence is transforming healthcare through improved diagnostics.",
        "tech", DateTime.UtcNow),
    new Document("doc-2", "Climate Change Research",
        "Scientists are using machine learning to model climate patterns.",
        "science", DateTime.UtcNow),
    // More documents...
};

foreach (var doc in documents)
{
    await producer.ProduceAsync("documents", doc.Id, JsonSerializer.Serialize(doc));
}

// Consumer: Generate embeddings and store in Qdrant
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "qdrant-indexer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, "documents");

var points = new List<QdrantPoint>();

for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;

    // Generate embedding
    var embedding = GenerateEmbedding(doc.Content);

    // Create Qdrant point with payload
    var point = new QdrantPoint(
        doc.Id,
        embedding,
        new Dictionary<string, object>
        {
            ["title"] = doc.Title,
            ["category"] = doc.Category,
            ["timestamp"] = doc.Timestamp.ToString("O")
        });

    points.Add(point);
}

// Batch upsert to Qdrant
await qdrantClient.UpsertPointsAsync(collectionName, points);
Console.WriteLine($"Indexed {points.Count} documents to Qdrant");
```

## Step 5: Perform Vector Search

Search for similar documents:

```csharp
Console.WriteLine("\n--- Vector Search ---");
var query = "How is AI used in medical diagnosis?";
Console.WriteLine($"Query: \"{query}\"");

var queryEmbedding = GenerateEmbedding(query);

var searchResults = await qdrantClient.SearchAsync(
    collectionName,
    queryEmbedding,
    limit: 5);

Console.WriteLine("\nResults:");
foreach (var result in searchResults)
{
    var title = result.Payload.GetProperty("title").GetString();
    Console.WriteLine($"  [{result.Score:F4}] {title}");
}
```

## Step 6: Implement Filtered Search

Combine vector search with metadata filtering:

```csharp
Console.WriteLine("\n--- Filtered Vector Search ---");

// Search only in "tech" category
var filter = new Dictionary<string, object>
{
    ["must"] = new[]
    {
        new Dictionary<string, object>
        {
            ["key"] = "category",
            ["match"] = new { value = "tech" }
        }
    }
};

var filteredResults = await qdrantClient.SearchAsync(
    collectionName,
    queryEmbedding,
    limit: 5,
    filter: filter);

Console.WriteLine("Results (filtered by category='tech'):");
foreach (var result in filteredResults)
{
    var title = result.Payload.GetProperty("title").GetString();
    var category = result.Payload.GetProperty("category").GetString();
    Console.WriteLine($"  [{result.Score:F4}] {title} ({category})");
}
```

## Step 7: Create a Search Pipeline

Build a complete search pipeline with Surgewave:

```csharp
// Search queries come through Surgewave topic
await using var searchProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

await using var searchConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "search-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

// Subscribe to search queries
await searchConsumer.SubscribeAsync(default, "search-queries");

// Process search queries
while (!cancellationToken.IsCancellationRequested)
{
    var query = await searchConsumer.ConsumeAsync(cancellationToken);
    if (query == null) continue;

    var searchRequest = JsonSerializer.Deserialize<SearchRequest>(query.Value)!;

    // Generate query embedding and search
    var queryEmbedding = GenerateEmbedding(searchRequest.Query);
    var results = await qdrantClient.SearchAsync(collectionName, queryEmbedding, 5);

    // Publish results
    var response = new SearchResponse(searchRequest.Id, results);
    await searchProducer.ProduceAsync("search-results", searchRequest.Id,
        JsonSerializer.Serialize(response));
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Qdrant client with collection management
- Document indexing with embeddings
- Vector similarity search
- Filtered search with metadata
- Surgewave integration for search pipeline

## Sample Output

```
=== Surgewave Bootcamp: Lesson 22.3 - Qdrant Vector Database ===

Surgewave broker started on port 12345
Qdrant available: Yes (using simulated for demo)

--- Creating Collection ---
Collection 'documents' created: true
Vector dimensions: 1536
Distance metric: Cosine

--- Indexing Documents ---
Indexed: AI in Healthcare
Indexed: Climate Change Research
Indexed: Quantum Computing Advances
Indexed: Renewable Energy Solutions
Indexed: Space Exploration Technology
Total documents indexed: 5

--- Vector Search ---
Query: "How is AI used in medical diagnosis?"
Results:
  [0.9234] AI in Healthcare
  [0.7845] Climate Change Research
  [0.6123] Quantum Computing Advances

--- Filtered Vector Search ---
Query: "machine learning applications"
Filter: category = "tech"
Results:
  [0.8567] AI in Healthcare
  [0.7234] Quantum Computing Advances

Lesson complete!
```

## Knowledge Check

1. Why can't traditional databases efficiently perform vector similarity search?
2. What is the purpose of the HNSW index in Qdrant?
3. When should you use Cosine distance vs Euclidean distance?
4. What are the benefits of combining vector search with metadata filtering?
5. Why should you batch upsert operations in Qdrant?

---

**Next:** [Lesson 22.4: RAG Pipeline Architecture](../lesson-4/README.md)

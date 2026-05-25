# Lesson 22.1: Instructions

Follow these step-by-step instructions to implement OpenAI embeddings with Surgewave.

## Step 1: Set Up the OpenAI Embedding Client

Create a simple embedding client that calls the OpenAI API:

```csharp
public class OpenAIEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OpenAIEmbeddingClient(string apiKey, string model = "text-embedding-3-small")
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _model = model;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new { input = text, model = _model };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/embeddings",
            content);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);
        var embedding = parsed.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        return embedding;
    }
}
```

## Step 2: Define Document and Enriched Document Types

Create types to represent documents before and after embedding:

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
    DateTime Timestamp,
    float[] Embedding
);
```

## Step 3: Create the Document Producer

Produce documents to a Surgewave topic:

```csharp
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

var documents = new[]
{
    new Document("doc-1", "Introduction to Machine Learning",
        "Machine learning is a subset of artificial intelligence that enables systems to learn from data.",
        DateTime.UtcNow),
    new Document("doc-2", "Deep Learning Fundamentals",
        "Deep learning uses neural networks with multiple layers to analyze various factors of data.",
        DateTime.UtcNow),
    new Document("doc-3", "Cloud Computing Overview",
        "Cloud computing delivers computing services over the internet including servers, storage, and databases.",
        DateTime.UtcNow),
    new Document("doc-4", "Natural Language Processing",
        "NLP is a branch of AI that helps computers understand, interpret, and manipulate human language.",
        DateTime.UtcNow),
};

foreach (var doc in documents)
{
    var json = JsonSerializer.Serialize(doc);
    await producer.ProduceAsync("documents", doc.Id, json);
    Console.WriteLine($"Produced: {doc.Title}");
}
```

## Step 4: Create the Embedding Consumer

Consume documents and enrich them with embeddings:

```csharp
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "embedding-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, "documents");

var enrichedDocs = new List<EnrichedDocument>();

for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;

    // Generate embedding for the document content
    Console.WriteLine($"Generating embedding for: {doc.Title}");
    var embedding = await embeddingClient.GenerateEmbeddingAsync(doc.Content);

    var enriched = new EnrichedDocument(
        doc.Id,
        doc.Title,
        doc.Content,
        doc.Timestamp,
        embedding
    );

    enrichedDocs.Add(enriched);
    Console.WriteLine($"  Embedding generated: {embedding.Length} dimensions");
}
```

## Step 5: Implement Cosine Similarity

Create a function to compare embeddings:

```csharp
static double CosineSimilarity(float[] a, float[] b)
{
    if (a.Length != b.Length)
        throw new ArgumentException("Vectors must have same length");

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
```

## Step 6: Find Similar Documents

Compare all documents and find the most similar pairs:

```csharp
Console.WriteLine("\n--- Document Similarities ---");

for (int i = 0; i < enrichedDocs.Count; i++)
{
    for (int j = i + 1; j < enrichedDocs.Count; j++)
    {
        var similarity = CosineSimilarity(
            enrichedDocs[i].Embedding,
            enrichedDocs[j].Embedding);

        Console.WriteLine($"{enrichedDocs[i].Title} <-> {enrichedDocs[j].Title}");
        Console.WriteLine($"  Similarity: {similarity:F4}");
    }
}
```

## Step 7: Implement Semantic Search

Search for documents similar to a query:

```csharp
async Task<List<(EnrichedDocument Doc, double Score)>> SemanticSearch(
    string query,
    List<EnrichedDocument> documents,
    int topK = 3)
{
    var queryEmbedding = await embeddingClient.GenerateEmbeddingAsync(query);

    var results = documents
        .Select(doc => (Doc: doc, Score: CosineSimilarity(queryEmbedding, doc.Embedding)))
        .OrderByDescending(x => x.Score)
        .Take(topK)
        .ToList();

    return results;
}

// Example search
Console.WriteLine("\n--- Semantic Search ---");
var searchQuery = "How do computers learn from data?";
Console.WriteLine($"Query: \"{searchQuery}\"");

var searchResults = await SemanticSearch(searchQuery, enrichedDocs);

Console.WriteLine("\nTop Results:");
foreach (var (doc, score) in searchResults)
{
    Console.WriteLine($"  [{score:F4}] {doc.Title}");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- OpenAI embedding client with error handling
- Document streaming through Surgewave
- Embedding generation for each document
- Cosine similarity calculation
- Semantic search functionality

## Sample Output

```
=== Surgewave Bootcamp: Lesson 22.1 - OpenAI Embeddings ===

Surgewave broker started on port 12345

--- Producing Documents ---
Produced: Introduction to Machine Learning
Produced: Deep Learning Fundamentals
Produced: Cloud Computing Overview
Produced: Natural Language Processing

--- Generating Embeddings ---
Generating embedding for: Introduction to Machine Learning
  Embedding generated: 1536 dimensions
Generating embedding for: Deep Learning Fundamentals
  Embedding generated: 1536 dimensions
Generating embedding for: Cloud Computing Overview
  Embedding generated: 1536 dimensions
Generating embedding for: Natural Language Processing
  Embedding generated: 1536 dimensions

--- Document Similarities ---
Introduction to Machine Learning <-> Deep Learning Fundamentals
  Similarity: 0.8923
Introduction to Machine Learning <-> Cloud Computing Overview
  Similarity: 0.7245
Introduction to Machine Learning <-> Natural Language Processing
  Similarity: 0.8567
Deep Learning Fundamentals <-> Cloud Computing Overview
  Similarity: 0.7012
Deep Learning Fundamentals <-> Natural Language Processing
  Similarity: 0.8234
Cloud Computing Overview <-> Natural Language Processing
  Similarity: 0.6891

--- Semantic Search ---
Query: "How do computers learn from data?"

Top Results:
  [0.9012] Introduction to Machine Learning
  [0.8567] Deep Learning Fundamentals
  [0.7234] Natural Language Processing

Lesson complete!
```

## Knowledge Check

1. What is an embedding and why is it useful for search?
2. Why is cosine similarity preferred over Euclidean distance for text embeddings?
3. How many dimensions does OpenAI's text-embedding-3-small model produce?
4. What strategies can you use to handle OpenAI API rate limits?
5. Why should you batch embedding requests when processing many documents?

---

**Next:** [Lesson 22.2: Ollama Local LLM Integration](../lesson-2/README.md)

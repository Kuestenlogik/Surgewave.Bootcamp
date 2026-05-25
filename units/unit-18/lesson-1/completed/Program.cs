// Surgewave Bootcamp - Unit 22, Lesson 1: OpenAI Embeddings
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.1 - OpenAI Embeddings ===");
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

// Step 1 - Create an embedding client
// For this demo, we use simulated embeddings. In production:
// var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
// var embeddingClient = new OpenAIEmbeddingClient(apiKey);

// Simulated embedding generator that produces consistent embeddings based on content
// Similar content will produce similar embeddings
float[] GenerateSimulatedEmbedding(string text)
{
    // Create a deterministic embedding based on text content
    var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var embedding = new float[1536];

    // Use word hashes to create a content-based embedding
    foreach (var word in words)
    {
        var hash = word.GetHashCode();
        var rng = new Random(hash);
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] += (float)(rng.NextDouble() * 2 - 1) / words.Length;
        }
    }

    // Normalize the embedding to unit length
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

// Step 2 - Document types are defined at the bottom of this file

// Step 3 - Create a producer and send documents
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

var documents = new Document[]
{
    new("doc-1", "Introduction to Machine Learning",
        "Machine learning is a subset of artificial intelligence that enables systems to learn from data and improve their performance over time.",
        DateTime.UtcNow),
    new("doc-2", "Deep Learning Fundamentals",
        "Deep learning uses neural networks with multiple layers to analyze various factors of data, enabling complex pattern recognition.",
        DateTime.UtcNow),
    new("doc-3", "Cloud Computing Overview",
        "Cloud computing delivers computing services over the internet including servers, storage, databases, and networking capabilities.",
        DateTime.UtcNow),
    new("doc-4", "Natural Language Processing",
        "NLP is a branch of artificial intelligence that helps computers understand, interpret, and manipulate human language effectively.",
        DateTime.UtcNow),
    new("doc-5", "Data Science Principles",
        "Data science combines statistics, machine learning, and domain expertise to extract insights and knowledge from structured and unstructured data.",
        DateTime.UtcNow),
};

Console.WriteLine("\n--- Producing Documents ---");
foreach (var doc in documents)
{
    var json = JsonSerializer.Serialize(doc);
    await producer.ProduceAsync(topic, doc.Id, json);
    Console.WriteLine($"Produced: {doc.Title}");
}

// Step 4 - Create a consumer and generate embeddings
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "embedding-processor";
    opt.EnableAutoCommit = false;
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

var enrichedDocs = new List<EnrichedDocument>();

Console.WriteLine("\n--- Generating Embeddings ---");
for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;

    // Generate embedding for the document content
    Console.WriteLine($"Generating embedding for: {doc.Title}");
    var embedding = GenerateSimulatedEmbedding(doc.Content);

    var enriched = new EnrichedDocument(
        doc.Id,
        doc.Title,
        doc.Content,
        doc.Timestamp,
        embedding
    );

    enrichedDocs.Add(enriched);
    Console.WriteLine($"  Embedding generated: {embedding.Length} dimensions");

    await consumer.CommitAsync(result);
}

// Step 5 - Cosine similarity function
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

// Step 6 - Calculate and display document similarities
Console.WriteLine("\n--- Document Similarities ---");
var similarities = new List<(string Doc1, string Doc2, double Score)>();

for (int i = 0; i < enrichedDocs.Count; i++)
{
    for (int j = i + 1; j < enrichedDocs.Count; j++)
    {
        var similarity = CosineSimilarity(
            enrichedDocs[i].Embedding,
            enrichedDocs[j].Embedding);

        similarities.Add((enrichedDocs[i].Title, enrichedDocs[j].Title, similarity));
    }
}

// Show top 5 most similar pairs
Console.WriteLine("Top 5 Most Similar Document Pairs:");
foreach (var (doc1, doc2, score) in similarities.OrderByDescending(s => s.Score).Take(5))
{
    Console.WriteLine($"  [{score:F4}] {doc1} <-> {doc2}");
}

// Step 7 - Implement semantic search
Console.WriteLine("\n--- Semantic Search ---");

var searchQueries = new[]
{
    "How do computers learn from data?",
    "What is cloud infrastructure?",
    "How can AI understand human speech?"
};

foreach (var searchQuery in searchQueries)
{
    Console.WriteLine($"\nQuery: \"{searchQuery}\"");

    var queryEmbedding = GenerateSimulatedEmbedding(searchQuery);
    var searchResults = enrichedDocs
        .Select(doc => (Doc: doc, Score: CosineSimilarity(queryEmbedding, doc.Embedding)))
        .OrderByDescending(x => x.Score)
        .Take(3)
        .ToList();

    Console.WriteLine("Top Results:");
    foreach (var (doc, score) in searchResults)
    {
        Console.WriteLine($"  [{score:F4}] {doc.Title}");
    }
}

// Display embedding statistics
Console.WriteLine("\n--- Embedding Statistics ---");
Console.WriteLine($"Total documents indexed: {enrichedDocs.Count}");
Console.WriteLine($"Embedding dimensions: {enrichedDocs[0].Embedding.Length}");
Console.WriteLine($"Total similarity comparisons: {similarities.Count}");
var avgSimilarity = similarities.Average(s => s.Score);
Console.WriteLine($"Average similarity: {avgSimilarity:F4}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Embeddings capture semantic meaning as numerical vectors");
Console.WriteLine("2. Similar texts produce similar embeddings");
Console.WriteLine("3. Cosine similarity measures how similar two embeddings are");
Console.WriteLine("4. Semantic search finds relevant content based on meaning, not keywords");
Console.WriteLine("5. OpenAI's text-embedding-3-small produces 1536-dimensional vectors");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types
public record Document(string Id, string Title, string Content, DateTime Timestamp);
public record EnrichedDocument(string Id, string Title, string Content, DateTime Timestamp, float[] Embedding);

// Surgewave Bootcamp - Unit 22, Lesson 1: OpenAI Embeddings
//
// In this lesson, you'll learn:
// - How to generate embeddings using OpenAI API
// - Streaming documents through Surgewave
// - Calculating similarity between embeddings
// - Implementing semantic search
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.1 - OpenAI Embeddings ===");
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

// TODO: Step 1 - Create an OpenAI embedding client
// You'll need to set your API key. For this demo, we'll use simulated embeddings.
// In production, use:
// var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
// var embeddingClient = new OpenAIEmbeddingClient(apiKey);

// For this lesson, we'll simulate embeddings to avoid requiring an API key
// Simulated embedding generator (replace with real OpenAI client in production)
var random = new Random(42); // Fixed seed for reproducibility

float[] GenerateSimulatedEmbedding(string text)
{
    // Simulate 1536-dimensional embedding (same as text-embedding-3-small)
    // In production, this would call OpenAI API
    var hash = text.GetHashCode();
    var rng = new Random(hash);
    var embedding = new float[1536];
    for (int i = 0; i < embedding.Length; i++)
    {
        embedding[i] = (float)(rng.NextDouble() * 2 - 1);
    }
    // Normalize the embedding
    var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
    for (int i = 0; i < embedding.Length; i++)
    {
        embedding[i] /= (float)magnitude;
    }
    return embedding;
}

// TODO: Step 2 - Define Document and EnrichedDocument types
// public record Document(string Id, string Title, string Content, DateTime Timestamp);
// public record EnrichedDocument(string Id, string Title, string Content, DateTime Timestamp, float[] Embedding);

// TODO: Step 3 - Create a producer and send documents
// await using var producer = new SurgewaveProducer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
// });
//
// var documents = new[]
// {
//     new Document("doc-1", "Introduction to Machine Learning",
//         "Machine learning is a subset of artificial intelligence that enables systems to learn from data.",
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

// TODO: Step 4 - Create a consumer and generate embeddings
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
//     opt.GroupId = "embedding-processor";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// await consumer.SubscribeAsync(default, topic);
//
// var enrichedDocs = new List<EnrichedDocument>();
//
// Console.WriteLine("\n--- Generating Embeddings ---");
// // Consume and enrich documents...

// TODO: Step 5 - Implement cosine similarity
// static double CosineSimilarity(float[] a, float[] b)
// {
//     double dotProduct = 0, normA = 0, normB = 0;
//     for (int i = 0; i < a.Length; i++)
//     {
//         dotProduct += a[i] * b[i];
//         normA += a[i] * a[i];
//         normB += b[i] * b[i];
//     }
//     if (normA == 0 || normB == 0) return 0;
//     return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
// }

// TODO: Step 6 - Calculate and display document similarities
// Console.WriteLine("\n--- Document Similarities ---");
// for (int i = 0; i < enrichedDocs.Count; i++)
// {
//     for (int j = i + 1; j < enrichedDocs.Count; j++)
//     {
//         var similarity = CosineSimilarity(enrichedDocs[i].Embedding, enrichedDocs[j].Embedding);
//         Console.WriteLine($"{enrichedDocs[i].Title} <-> {enrichedDocs[j].Title}");
//         Console.WriteLine($"  Similarity: {similarity:F4}");
//     }
// }

// TODO: Step 7 - Implement semantic search
// Console.WriteLine("\n--- Semantic Search ---");
// var searchQuery = "How do computers learn from data?";
// Console.WriteLine($"Query: \"{searchQuery}\"");
//
// var queryEmbedding = GenerateSimulatedEmbedding(searchQuery);
// var searchResults = enrichedDocs
//     .Select(doc => (Doc: doc, Score: CosineSimilarity(queryEmbedding, doc.Embedding)))
//     .OrderByDescending(x => x.Score)
//     .Take(3)
//     .ToList();
//
// Console.WriteLine("\nTop Results:");
// foreach (var (doc, score) in searchResults)
// {
//     Console.WriteLine($"  [{score:F4}] {doc.Title}");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Embeddings capture semantic meaning as numerical vectors");
Console.WriteLine("2. Similar texts produce similar embeddings");
Console.WriteLine("3. Cosine similarity measures how similar two embeddings are");
Console.WriteLine("4. Semantic search finds relevant content based on meaning, not keywords");
Console.WriteLine("5. OpenAI's text-embedding-3-small produces 1536-dimensional vectors");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types for reference (uncomment when implementing)
// public record Document(string Id, string Title, string Content, DateTime Timestamp);
// public record EnrichedDocument(string Id, string Title, string Content, DateTime Timestamp, float[] Embedding);

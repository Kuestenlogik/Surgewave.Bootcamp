// Surgewave Bootcamp - Unit 22, Lesson 4: RAG Pipeline Architecture
//
// In this lesson, you'll learn:
// - How to build a Retrieval-Augmented Generation pipeline
// - Document chunking strategies
// - Context retrieval and prompt augmentation
// - Generating answers with citations
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.4 - RAG Pipeline ===");
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

// TODO: Step 1 - Implement document chunking
// public class DocumentChunker
// {
//     private readonly int _chunkSize;
//     private readonly int _overlap;
//
//     public DocumentChunker(int chunkSize = 300, int overlap = 50)
//     {
//         _chunkSize = chunkSize;
//         _overlap = overlap;
//     }
//
//     public IEnumerable<DocumentChunk> ChunkDocument(Document document)
//     {
//         // Split document into overlapping chunks
//         // Break at sentence boundaries when possible
//     }
// }

// TODO: Step 2 - Define data types
// public record Document(string Id, string Title, string Content, DateTime Timestamp);
// public record DocumentChunk(string Id, string DocumentId, string DocumentTitle, string Content, int ChunkIndex);
// public record IndexedChunk(string Id, string DocumentId, string DocumentTitle, string Content, int ChunkIndex, float[] Embedding);
// public record RetrievedContext(string Source, string Content, double Score);

// TODO: Step 3 - Produce documents to Surgewave
// var documents = new Document[]
// {
//     new("doc-1", "AI in Healthcare",
//         "Artificial intelligence is revolutionizing healthcare through improved diagnostics. " +
//         "Machine learning algorithms can analyze medical images with high accuracy. " +
//         "AI-powered systems assist doctors in making better treatment decisions. " +
//         "Predictive analytics help identify patients at risk of developing diseases.",
//         DateTime.UtcNow),
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

// TODO: Step 4 - Consume, chunk, and index documents
// var chunker = new DocumentChunker(chunkSize: 200, overlap: 30);
// var indexedChunks = new List<IndexedChunk>();
//
// await using var consumer = new SurgewaveConsumer<string, string>(opt =>
// {
//     opt.BootstrapServers = $"localhost:{runtime.Port}";
//     opt.GroupId = "rag-indexer";
//     opt.AutoOffsetReset = AutoOffsetReset.Earliest;
// });
//
// await consumer.SubscribeAsync(default, topic);
//
// Console.WriteLine("\n--- Chunking and Indexing Documents ---");
// // Process documents, chunk them, generate embeddings, and store

// TODO: Step 5 - Implement context retrieval
// List<RetrievedContext> RetrieveContext(float[] queryEmbedding, int topK = 3, double minScore = 0.5)
// {
//     return indexedChunks
//         .Select(c => new RetrievedContext(c.DocumentTitle, c.Content, CosineSimilarity(queryEmbedding, c.Embedding)))
//         .Where(r => r.Score >= minScore)
//         .OrderByDescending(r => r.Score)
//         .Take(topK)
//         .ToList();
// }

// TODO: Step 6 - Build augmented prompt
// string BuildRAGPrompt(string question, List<RetrievedContext> context)
// {
//     var sb = new StringBuilder();
//     sb.AppendLine("You are a helpful assistant. Answer based ONLY on the provided context.");
//     sb.AppendLine("If the answer is not in the context, say so. Cite your sources.");
//     sb.AppendLine();
//     sb.AppendLine("Context:");
//     foreach (var ctx in context)
//     {
//         sb.AppendLine($"[Source: {ctx.Source}]");
//         sb.AppendLine(ctx.Content);
//         sb.AppendLine();
//     }
//     sb.AppendLine($"Question: {question}");
//     sb.AppendLine();
//     sb.AppendLine("Answer:");
//     return sb.ToString();
// }

// TODO: Step 7 - Process RAG queries
// Console.WriteLine("\n--- RAG Query Processing ---");
// var queries = new[]
// {
//     "How is AI being used in healthcare?",
//     "What are the benefits of renewable energy?",
// };
//
// foreach (var query in queries)
// {
//     Console.WriteLine($"\nQuery: \"{query}\"");
//
//     // Generate query embedding
//     var queryEmbedding = GenerateEmbedding(query);
//
//     // Retrieve relevant context
//     var context = RetrieveContext(queryEmbedding);
//
//     Console.WriteLine($"Retrieved {context.Count} relevant chunks:");
//     foreach (var ctx in context)
//     {
//         Console.WriteLine($"  [{ctx.Score:F4}] {ctx.Source}");
//     }
//
//     // Build augmented prompt
//     var prompt = BuildRAGPrompt(query, context);
//     Console.WriteLine("\nAugmented Prompt Preview:");
//     Console.WriteLine(prompt[..Math.Min(300, prompt.Length)] + "...");
// }

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. RAG grounds LLM responses in your actual documents");
Console.WriteLine("2. Document chunking with overlap preserves context");
Console.WriteLine("3. Retrieve more documents than you use for better filtering");
Console.WriteLine("4. Always handle the case when no relevant documents are found");
Console.WriteLine("5. Include citations to build trust in AI responses");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types (uncomment when implementing)
// public record Document(string Id, string Title, string Content, DateTime Timestamp);
// public record DocumentChunk(string Id, string DocumentId, string DocumentTitle, string Content, int ChunkIndex);
// public record IndexedChunk(string Id, string DocumentId, string DocumentTitle, string Content, int ChunkIndex, float[] Embedding);
// public record RetrievedContext(string Source, string Content, double Score);

// Surgewave Bootcamp - Unit 22, Lesson 4: RAG Pipeline Architecture
// COMPLETED SOLUTION

using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.4 - RAG Pipeline ===");
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

// Step 1 - Document chunker with overlap
IEnumerable<DocumentChunk> ChunkDocument(Document document, int chunkSize = 200, int overlap = 30)
{
    var text = document.Content;
    var chunks = new List<DocumentChunk>();
    var chunkIndex = 0;

    for (int i = 0; i < text.Length; i += chunkSize - overlap)
    {
        var end = Math.Min(i + chunkSize, text.Length);
        var chunkText = text[i..end];

        // Try to break at sentence boundary
        if (end < text.Length)
        {
            var lastPeriod = chunkText.LastIndexOf('.');
            if (lastPeriod > chunkSize / 2)
            {
                chunkText = chunkText[..(lastPeriod + 1)];
            }
        }

        chunks.Add(new DocumentChunk(
            Id: $"{document.Id}-chunk-{chunkIndex}",
            DocumentId: document.Id,
            DocumentTitle: document.Title,
            Content: chunkText.Trim(),
            ChunkIndex: chunkIndex
        ));

        chunkIndex++;

        // Break if we've processed all content
        if (end >= text.Length) break;
    }

    return chunks;
}

// Step 2 - Define documents with longer content for chunking
var documents = new Document[]
{
    new("doc-1", "AI in Healthcare",
        "Artificial intelligence is revolutionizing healthcare through improved diagnostics. " +
        "Machine learning algorithms can analyze medical images with high accuracy, often detecting " +
        "conditions that human doctors might miss. AI-powered systems assist doctors in making better " +
        "treatment decisions by analyzing patient data and medical literature. Predictive analytics " +
        "help identify patients at risk of developing diseases, enabling preventive care.",
        DateTime.UtcNow),
    new("doc-2", "Renewable Energy",
        "Renewable energy sources are becoming increasingly important in the fight against climate " +
        "change. Solar power technology has advanced significantly, with panels becoming more efficient " +
        "and affordable. Wind farms now generate substantial portions of electricity in many countries. " +
        "Battery storage technology is improving, solving the intermittency problem of renewables. " +
        "Many governments offer incentives for adopting clean energy solutions.",
        DateTime.UtcNow),
    new("doc-3", "Machine Learning Basics",
        "Machine learning is a subset of artificial intelligence that enables computers to learn from " +
        "data without being explicitly programmed. Supervised learning uses labeled data to train models " +
        "for classification and regression tasks. Unsupervised learning finds patterns in unlabeled data. " +
        "Deep learning uses neural networks with many layers to achieve state-of-the-art results in " +
        "image recognition, natural language processing, and many other domains.",
        DateTime.UtcNow),
};

// Step 3 - Produce documents to Surgewave
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("--- Producing Documents to Surgewave ---");
foreach (var doc in documents)
{
    await producer.ProduceAsync(topic, doc.Id, JsonSerializer.Serialize(doc));
    Console.WriteLine($"Produced: {doc.Title}");
}

// Step 4 - Consume, chunk, and index documents
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "rag-indexer";
    opt.EnableAutoCommit = false;
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, topic);

var indexedChunks = new List<IndexedChunk>();

Console.WriteLine("\n--- Chunking and Indexing Documents ---");
for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
    var chunks = ChunkDocument(doc);

    foreach (var chunk in chunks)
    {
        var embedding = GenerateEmbedding(chunk.Content);

        var indexed = new IndexedChunk(
            chunk.Id,
            chunk.DocumentId,
            chunk.DocumentTitle,
            chunk.Content,
            chunk.ChunkIndex,
            embedding
        );

        indexedChunks.Add(indexed);
        Console.WriteLine($"  Indexed: {chunk.DocumentTitle} (chunk {chunk.ChunkIndex + 1})");
    }

    await consumer.CommitAsync(result);
}

Console.WriteLine($"Total chunks indexed: {indexedChunks.Count}");

// Step 5 - Context retrieval function
List<RetrievedContext> RetrieveContext(float[] queryEmbedding, int topK = 3, double minScore = 0.3)
{
    return indexedChunks
        .Select(c => new RetrievedContext(c.DocumentTitle, c.Content, CosineSimilarity(queryEmbedding, c.Embedding)))
        .Where(r => r.Score >= minScore)
        .OrderByDescending(r => r.Score)
        .Take(topK)
        .ToList();
}

// Step 6 - RAG prompt builder
string BuildRAGPrompt(string question, List<RetrievedContext> context)
{
    var sb = new StringBuilder();

    // System instructions
    sb.AppendLine("You are a helpful assistant. Answer questions based ONLY on the provided context.");
    sb.AppendLine("If the answer is not in the context, say \"I don't have enough information to answer that.\"");
    sb.AppendLine("Always cite your sources by mentioning the document name.");
    sb.AppendLine();

    // Context section
    if (context.Count == 0)
    {
        sb.AppendLine("Context: No relevant documents found.");
    }
    else
    {
        sb.AppendLine("Context:");
        sb.AppendLine();
        foreach (var ctx in context)
        {
            sb.AppendLine($"[Source: {ctx.Source}] (relevance: {ctx.Score:F2})");
            sb.AppendLine(ctx.Content);
            sb.AppendLine();
        }
    }

    // Question
    sb.AppendLine($"Question: {question}");
    sb.AppendLine();
    sb.AppendLine("Answer (with citations):");

    return sb.ToString();
}

// Simulated LLM response generator
string GenerateSimulatedAnswer(string question, List<RetrievedContext> context)
{
    if (context.Count == 0)
    {
        return "I don't have enough information in the provided documents to answer this question.";
    }

    // Extract key information from context for a simulated answer
    var topSource = context[0];
    var keywords = question.ToLowerInvariant().Split(' ');

    var sb = new StringBuilder();
    sb.Append("Based on the provided context, ");

    if (keywords.Contains("ai") || keywords.Contains("healthcare") || keywords.Contains("medical"))
    {
        sb.Append("AI is being used in healthcare for improved diagnostics, analyzing medical images, ");
        sb.Append("and helping doctors make better treatment decisions through predictive analytics.");
    }
    else if (keywords.Contains("renewable") || keywords.Contains("energy") || keywords.Contains("solar"))
    {
        sb.Append("renewable energy sources like solar and wind power are becoming more efficient ");
        sb.Append("and affordable, with improved battery storage solving intermittency issues.");
    }
    else if (keywords.Contains("machine") || keywords.Contains("learning"))
    {
        sb.Append("machine learning enables computers to learn from data through supervised and ");
        sb.Append("unsupervised learning approaches, with deep learning achieving state-of-the-art results.");
    }
    else
    {
        sb.Append("the documents discuss topics related to AI, healthcare, renewable energy, and machine learning.");
    }

    sb.AppendLine();
    sb.AppendLine();
    sb.Append($"Source: {topSource.Source}");

    return sb.ToString();
}

// Step 7 - Process RAG queries
Console.WriteLine("\n--- RAG Query Processing ---");
var queries = new[]
{
    "How is AI being used in healthcare?",
    "What are the benefits of renewable energy?",
    "What is the difference between supervised and unsupervised learning?",
    "What is quantum computing?"  // This should show no-results handling
};

foreach (var query in queries)
{
    Console.WriteLine($"\n{'=',-60}");
    Console.WriteLine($"Query: \"{query}\"");
    Console.WriteLine($"{'=',-60}");

    // Generate query embedding
    var queryEmbedding = GenerateEmbedding(query);

    // Retrieve relevant context
    var context = RetrieveContext(queryEmbedding);

    if (context.Count > 0)
    {
        Console.WriteLine($"\nRetrieved {context.Count} relevant chunks:");
        foreach (var ctx in context)
        {
            Console.WriteLine($"  [{ctx.Score:F4}] {ctx.Source}");
            Console.WriteLine($"    \"{ctx.Content[..Math.Min(80, ctx.Content.Length)]}...\"");
        }
    }
    else
    {
        Console.WriteLine("\nNo relevant documents found for this query.");
    }

    // Build augmented prompt
    var prompt = BuildRAGPrompt(query, context);

    Console.WriteLine("\n--- Augmented Prompt ---");
    // Show truncated prompt for display
    var promptPreview = prompt.Length > 500 ? prompt[..500] + "\n[...]" : prompt;
    Console.WriteLine(promptPreview);

    // Generate answer
    var answer = GenerateSimulatedAnswer(query, context);

    Console.WriteLine("\n--- Generated Answer ---");
    Console.WriteLine(answer);
}

// Display RAG pipeline statistics
Console.WriteLine("\n--- RAG Pipeline Statistics ---");
Console.WriteLine($"Documents processed: {documents.Length}");
Console.WriteLine($"Total chunks created: {indexedChunks.Count}");
Console.WriteLine($"Average chunks per document: {indexedChunks.Count / (double)documents.Length:F1}");
Console.WriteLine($"Embedding dimensions: {vectorDimensions}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. RAG grounds LLM responses in your actual documents");
Console.WriteLine("2. Document chunking with overlap preserves context");
Console.WriteLine("3. Retrieve more documents than you use for better filtering");
Console.WriteLine("4. Always handle the case when no relevant documents are found");
Console.WriteLine("5. Include citations to build trust in AI responses");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types
public record Document(string Id, string Title, string Content, DateTime Timestamp);
public record DocumentChunk(string Id, string DocumentId, string DocumentTitle, string Content, int ChunkIndex);
public record IndexedChunk(string Id, string DocumentId, string DocumentTitle, string Content, int ChunkIndex, float[] Embedding);
public record RetrievedContext(string Source, string Content, double Score);

// Surgewave Bootcamp - Unit 22 Project: AI Document Search
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== AI Document Search Application ===");
Console.WriteLine();

// Step 1 - Initialize Surgewave runtime
Console.WriteLine("[Setup] Initializing Surgewave runtime...");

await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"[Setup] Surgewave broker started on port {runtime.Port}");

var vectorDimensions = 1536;
Console.WriteLine($"[Setup] Vector dimensions: {vectorDimensions}");
Console.WriteLine("[Setup] Embedding mode: Simulated");

var documentTopic = "documents";

// Step 2 - Embedding generator
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

// Step 3 - Document chunker
IEnumerable<DocumentChunk> ChunkDocument(Document doc, int chunkSize = 250, int overlap = 40)
{
    var text = doc.Content;
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
            $"{doc.Id}-chunk-{chunkIndex}",
            doc.Id,
            doc.Title,
            chunkText.Trim(),
            doc.Category,
            chunkIndex
        ));

        chunkIndex++;
        if (end >= text.Length) break;
    }

    return chunks;
}

// Step 4 - Search index
var searchIndex = new Dictionary<string, IndexedChunk>();
var indexLock = new object();

void IndexChunk(IndexedChunk chunk)
{
    lock (indexLock)
    {
        searchIndex[chunk.Id] = chunk;
    }
}

string CreateSnippet(string content, int maxLength = 100)
{
    if (content.Length <= maxLength) return content;
    var snippet = content[..maxLength];
    var lastSpace = snippet.LastIndexOf(' ');
    if (lastSpace > maxLength * 0.7)
    {
        snippet = snippet[..lastSpace];
    }
    return snippet.TrimEnd() + "...";
}

List<SearchResult> SemanticSearch(float[] queryEmbedding, int limit, double minScore, string? category)
{
    lock (indexLock)
    {
        return searchIndex.Values
            .Where(c => category == null || c.Category == category)
            .Select(c => new { Chunk = c, Score = CosineSimilarity(queryEmbedding, c.Embedding) })
            .Where(x => x.Score >= minScore)
            .GroupBy(x => x.Chunk.DocumentId)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => new SearchResult(x.Chunk.DocumentId, x.Chunk.Title, CreateSnippet(x.Chunk.Content), x.Score, x.Chunk.Category))
            .ToList();
    }
}

List<SearchResult> KeywordSearch(string query, int limit, string? category)
{
    var keywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    lock (indexLock)
    {
        return searchIndex.Values
            .Where(c => category == null || c.Category == category)
            .Select(c => new
            {
                Chunk = c,
                Score = CalculateKeywordScore(c, keywords)
            })
            .Where(x => x.Score > 0)
            .GroupBy(x => x.Chunk.DocumentId)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => new SearchResult(x.Chunk.DocumentId, x.Chunk.Title, CreateSnippet(x.Chunk.Content), x.Score, x.Chunk.Category))
            .ToList();
    }
}

double CalculateKeywordScore(IndexedChunk chunk, string[] keywords)
{
    var content = chunk.Content.ToLowerInvariant();
    var title = chunk.Title.ToLowerInvariant();
    var titleMatches = keywords.Count(k => title.Contains(k)) * 2.0;
    var contentMatches = keywords.Count(k => content.Contains(k));
    return (titleMatches + contentMatches) / (keywords.Length * 3.0);
}

List<SearchResult> HybridSearch(string query, float[] queryEmbedding, int limit, string? category)
{
    var semanticResults = SemanticSearch(queryEmbedding, limit * 2, 0.2, category);
    var keywordResults = KeywordSearch(query, limit * 2, category);

    var scores = new Dictionary<string, double>();
    var documents = new Dictionary<string, SearchResult>();
    var k = 60;
    var semanticWeight = 0.7;
    var keywordWeight = 0.3;

    for (int i = 0; i < semanticResults.Count; i++)
    {
        var result = semanticResults[i];
        scores[result.DocumentId] = semanticWeight / (k + i + 1);
        documents[result.DocumentId] = result;
    }

    for (int i = 0; i < keywordResults.Count; i++)
    {
        var result = keywordResults[i];
        var rrf = keywordWeight / (k + i + 1);
        scores[result.DocumentId] = scores.GetValueOrDefault(result.DocumentId) + rrf;
        if (!documents.ContainsKey(result.DocumentId))
        {
            documents[result.DocumentId] = result;
        }
    }

    return scores
        .OrderByDescending(kv => kv.Value)
        .Take(limit)
        .Select(kv => documents[kv.Key] with { Score = kv.Value })
        .ToList();
}

// Step 5 - Metrics tracker
var latencies = new List<double>();

void RecordLatency(double ms)
{
    latencies.Add(ms);
}

void PrintMetricsReport()
{
    if (latencies.Count == 0)
    {
        Console.WriteLine("[Metrics] No searches recorded");
        return;
    }

    var docCount = searchIndex.Values.Select(c => c.DocumentId).Distinct().Count();
    var sorted = latencies.OrderBy(x => x).ToList();

    Console.WriteLine($"[Metrics] Documents indexed: {docCount}");
    Console.WriteLine($"[Metrics] Total chunks: {searchIndex.Count}");
    Console.WriteLine($"[Metrics] Search queries: {latencies.Count}");
    Console.WriteLine($"[Metrics] Average latency: {latencies.Average():F0}ms");
    Console.WriteLine($"[Metrics] P50 latency: {sorted[(int)(sorted.Count * 0.5)]:F0}ms");
    Console.WriteLine($"[Metrics] P95 latency: {sorted[Math.Min((int)(sorted.Count * 0.95), sorted.Count - 1)]:F0}ms");
    Console.WriteLine($"[Metrics] P99 latency: {sorted[Math.Min((int)(sorted.Count * 0.99), sorted.Count - 1)]:F0}ms");
}

// Step 6 - Document indexing
Console.WriteLine("\n=== Phase 1: Document Indexing ===");
Console.WriteLine("[Index] Processing document stream...");

var documents = new Document[]
{
    new("doc-1", "Introduction to Machine Learning",
        "Machine learning is a subset of artificial intelligence that enables computers to learn from data without explicit programming. It uses algorithms to identify patterns, make predictions, and improve automatically through experience. Common techniques include supervised learning, unsupervised learning, and reinforcement learning.",
        "technology", DateTime.UtcNow),
    new("doc-2", "Cloud Computing Fundamentals",
        "Cloud computing provides scalable infrastructure and services over the internet. Organizations can deploy applications without managing physical servers, reducing costs and complexity. Major providers include AWS, Azure, and Google Cloud. Key concepts include IaaS, PaaS, and SaaS service models.",
        "infrastructure", DateTime.UtcNow),
    new("doc-3", "Data Science Best Practices",
        "Data science combines statistics, programming, and domain knowledge to extract insights from data. Effective data scientists use visualization techniques, machine learning algorithms, and clear communication. The data science workflow includes data collection, cleaning, analysis, and presentation of findings.",
        "data", DateTime.UtcNow),
    new("doc-4", "Natural Language Processing",
        "NLP enables computers to understand and generate human language. Modern NLP uses deep learning transformers to achieve state-of-the-art results. Applications include sentiment analysis, machine translation, chatbots, and text summarization. Popular frameworks include Hugging Face and spaCy.",
        "technology", DateTime.UtcNow),
    new("doc-5", "Kubernetes Container Orchestration",
        "Kubernetes automates deployment, scaling, and management of containerized applications. It provides features like load balancing, service discovery, and automated rollouts. Kubernetes clusters consist of nodes running pods, managed by the control plane. Helm charts simplify application deployment.",
        "infrastructure", DateTime.UtcNow),
};

// Produce documents to Surgewave
await using var producer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

foreach (var doc in documents)
{
    await producer.ProduceAsync(documentTopic, doc.Id, JsonSerializer.Serialize(doc));
}

// Consume and index
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "document-indexer";
    opt.EnableAutoCommit = false;
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, documentTopic);

var totalChunks = 0;
for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
    var chunks = ChunkDocument(doc).ToList();

    foreach (var chunk in chunks)
    {
        var embedding = GenerateEmbedding(chunk.Content);
        var indexed = new IndexedChunk(chunk.Id, chunk.DocumentId, chunk.Title, chunk.Content, chunk.Category, chunk.ChunkIndex, embedding);
        IndexChunk(indexed);
    }

    totalChunks += chunks.Count;
    Console.WriteLine($"[Index] Indexed: {doc.Title} ({doc.Category}) - {chunks.Count} chunks");

    await consumer.CommitAsync(result);
}

Console.WriteLine($"[Index] Total: {documents.Length} documents, {totalChunks} chunks indexed");

// Step 7 - Search demo
Console.WriteLine("\n=== Phase 2: Search Demo ===");

var searchQueries = new[]
{
    (Query: "How do machines learn from data?", Category: (string?)null, Hybrid: false),
    (Query: "kubernetes deployment", Category: (string?)null, Hybrid: true),
    (Query: "quantum computing advances", Category: (string?)null, Hybrid: false),
    (Query: "cloud infrastructure", Category: "infrastructure", Hybrid: false),
    (Query: "text analysis techniques", Category: "technology", Hybrid: true),
};

foreach (var (query, category, hybrid) in searchQueries)
{
    Console.WriteLine();
    Console.WriteLine($"[Search] Query: \"{query}\"");
    if (category != null) Console.WriteLine($"[Search] Category filter: {category}");
    Console.WriteLine($"[Search] Mode: {(hybrid ? "Hybrid (semantic + keyword)" : "Semantic")}");

    var sw = Stopwatch.StartNew();
    var queryEmbedding = GenerateEmbedding(query);

    List<SearchResult> results;
    if (hybrid)
    {
        results = HybridSearch(query, queryEmbedding, 3, category);
    }
    else
    {
        results = SemanticSearch(queryEmbedding, 3, 0.3, category);
    }

    sw.Stop();
    RecordLatency(sw.Elapsed.TotalMilliseconds);

    if (results.Count == 0)
    {
        Console.WriteLine($"[Search] Results (0 found in {sw.ElapsedMilliseconds}ms):");
        Console.WriteLine("  No relevant documents found for this query.");
    }
    else
    {
        Console.WriteLine($"[Search] Results ({results.Count} found in {sw.ElapsedMilliseconds}ms):");
        var rank = 1;
        foreach (var result in results)
        {
            Console.WriteLine($"  {rank}. [{result.Score:F4}] {result.Title}");
            Console.WriteLine($"     \"{result.Snippet}\"");
            rank++;
        }
    }
}

// Step 8 - Additional searches for metrics
Console.WriteLine("\n[Search] Running additional queries for metrics...");
var additionalQueries = new[]
{
    "machine learning algorithms",
    "container orchestration",
    "data visualization",
    "neural networks",
    "cloud services"
};

foreach (var query in additionalQueries)
{
    var sw = Stopwatch.StartNew();
    var embedding = GenerateEmbedding(query);
    var _ = SemanticSearch(embedding, 3, 0.3, null);
    sw.Stop();
    RecordLatency(sw.Elapsed.TotalMilliseconds);
}
Console.WriteLine($"[Search] Completed {additionalQueries.Length} additional queries");

// Step 9 - Performance report
Console.WriteLine("\n=== Phase 3: Performance Report ===");
PrintMetricsReport();

// Index statistics
Console.WriteLine("\n[Stats] Index breakdown by category:");
var byCategory = searchIndex.Values
    .GroupBy(c => c.Category)
    .Select(g => $"  {g.Key}: {g.Select(c => c.DocumentId).Distinct().Count()} docs, {g.Count()} chunks");
foreach (var stat in byCategory)
{
    Console.WriteLine(stat);
}

Console.WriteLine("\n=== Application Complete ===");

// Record types
public record Document(string Id, string Title, string Content, string Category, DateTime CreatedAt);
public record DocumentChunk(string Id, string DocumentId, string Title, string Content, string Category, int ChunkIndex);
public record IndexedChunk(string Id, string DocumentId, string Title, string Content, string Category, int ChunkIndex, float[] Embedding);
public record SearchResult(string DocumentId, string Title, string Snippet, double Score, string Category);

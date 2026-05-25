// Surgewave Bootcamp - Unit 22, Lesson 5: Semantic Search Implementation
// COMPLETED SOLUTION

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 22.5 - Semantic Search ===");
Console.WriteLine();

// Start an in-memory Surgewave broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(0)
    .WithStorageMode(StorageMode.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Surgewave broker started on port {runtime.Port}");

var documentTopic = "documents";
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

// Step 1 - Search index
var searchIndex = new Dictionary<string, IndexedDocument>();
var indexLock = new object();

void IndexDocument(IndexedDocument doc)
{
    lock (indexLock)
    {
        searchIndex[doc.Id] = doc;
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

List<SearchResult> Search(float[] queryEmbedding, int limit = 10, double minScore = 0.3, string? category = null)
{
    lock (indexLock)
    {
        return searchIndex.Values
            .Where(d => category == null || d.Category == category)
            .Select(d => new { Doc = d, Score = CosineSimilarity(queryEmbedding, d.Embedding) })
            .Where(x => x.Score >= minScore)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => new SearchResult(x.Doc.Id, x.Doc.Title, CreateSnippet(x.Doc.Content), x.Score, x.Doc.Category))
            .ToList();
    }
}

// Keyword-based search for hybrid approach
List<SearchResult> KeywordSearch(string query, int limit = 10)
{
    var keywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    lock (indexLock)
    {
        return searchIndex.Values
            .Select(d => new
            {
                Doc = d,
                Score = CalculateKeywordScore(d, keywords)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => new SearchResult(x.Doc.Id, x.Doc.Title, CreateSnippet(x.Doc.Content), x.Score, x.Doc.Category))
            .ToList();
    }
}

double CalculateKeywordScore(IndexedDocument doc, string[] keywords)
{
    var content = doc.Content.ToLowerInvariant();
    var title = doc.Title.ToLowerInvariant();
    var titleMatches = keywords.Count(k => title.Contains(k)) * 2.0; // Title matches worth more
    var contentMatches = keywords.Count(k => content.Contains(k));
    return (titleMatches + contentMatches) / (keywords.Length * 3.0);
}

// Hybrid search with Reciprocal Rank Fusion
List<SearchResult> HybridSearch(string query, float[] queryEmbedding, int limit = 10, double semanticWeight = 0.7, double keywordWeight = 0.3)
{
    var semanticResults = Search(queryEmbedding, limit * 2, 0.2);
    var keywordResults = KeywordSearch(query, limit * 2);

    var scores = new Dictionary<string, double>();
    var documents = new Dictionary<string, SearchResult>();
    var k = 60; // RRF constant

    // Add semantic scores
    for (int i = 0; i < semanticResults.Count; i++)
    {
        var result = semanticResults[i];
        scores[result.DocumentId] = semanticWeight / (k + i + 1);
        documents[result.DocumentId] = result;
    }

    // Add keyword scores
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

// Step 2 - Metrics tracking
var latencies = new List<double>();

void RecordLatency(double ms)
{
    latencies.Add(ms);
}

void PrintMetrics()
{
    if (latencies.Count == 0)
    {
        Console.WriteLine("No searches recorded");
        return;
    }

    var sorted = latencies.OrderBy(x => x).ToList();
    Console.WriteLine($"Total queries: {latencies.Count}");
    Console.WriteLine($"Average latency: {latencies.Average():F1}ms");
    Console.WriteLine($"P50 latency: {sorted[(int)(sorted.Count * 0.5)]:F1}ms");
    Console.WriteLine($"P95 latency: {sorted[Math.Min((int)(sorted.Count * 0.95), sorted.Count - 1)]:F1}ms");
    Console.WriteLine($"P99 latency: {sorted[Math.Min((int)(sorted.Count * 0.99), sorted.Count - 1)]:F1}ms");
}

// Step 3 - Create documents
var documents = new SearchableDocument[]
{
    new("doc-1", "Introduction to Machine Learning",
        "Machine learning is a subset of artificial intelligence that enables computers to learn from data without being explicitly programmed. It uses algorithms to identify patterns and make predictions.",
        "technology", DateTime.UtcNow),
    new("doc-2", "Deep Learning with Neural Networks",
        "Deep learning uses neural networks with multiple layers to analyze complex patterns. Neural networks learn through backpropagation, adjusting weights to minimize prediction errors.",
        "technology", DateTime.UtcNow),
    new("doc-3", "Cloud Computing Fundamentals",
        "Cloud computing provides scalable infrastructure and services over the internet. Organizations can deploy applications without managing physical servers, reducing costs and complexity.",
        "infrastructure", DateTime.UtcNow),
    new("doc-4", "Natural Language Processing Guide",
        "NLP enables computers to understand and generate human language. Modern NLP uses deep learning transformers to achieve state-of-the-art results in translation and text analysis.",
        "technology", DateTime.UtcNow),
    new("doc-5", "Data Science Best Practices",
        "Data science combines statistics, programming, and domain knowledge to extract insights from data. Effective data scientists use visualization, machine learning, and clear communication.",
        "data", DateTime.UtcNow),
    new("doc-6", "Kubernetes Container Orchestration",
        "Kubernetes automates deployment, scaling, and management of containerized applications. It provides features like load balancing, service discovery, and automated rollouts.",
        "infrastructure", DateTime.UtcNow),
};

// Step 4 - Produce and index documents via Surgewave
await using var docProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

Console.WriteLine("--- Indexing Documents via Surgewave ---");
foreach (var doc in documents)
{
    await docProducer.ProduceAsync(documentTopic, doc.Id, JsonSerializer.Serialize(doc));
}

await using var indexConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "search-indexer";
    opt.EnableAutoCommit = false;
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await indexConsumer.SubscribeAsync(default, documentTopic);

for (int i = 0; i < documents.Length; i++)
{
    var result = await indexConsumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<SearchableDocument>(result.Value)!;
    var embedding = GenerateEmbedding(doc.Content);

    IndexDocument(new IndexedDocument(doc.Id, doc.Title, doc.Content, doc.Category, doc.CreatedAt, embedding));
    Console.WriteLine($"Indexed: {doc.Title} [{doc.Category}]");

    await indexConsumer.CommitAsync(result);
}

Console.WriteLine($"Total documents indexed: {searchIndex.Count}");

// Step 5 - Semantic search demo
Console.WriteLine("\n--- Semantic Search ---");
var semanticQueries = new[]
{
    "How do neural networks learn?",
    "cloud infrastructure deployment",
    "analyzing text with AI"
};

foreach (var queryText in semanticQueries)
{
    var sw = Stopwatch.StartNew();
    var queryEmbedding = GenerateEmbedding(queryText);
    var results = Search(queryEmbedding, limit: 3, minScore: 0.3);
    sw.Stop();

    RecordLatency(sw.Elapsed.TotalMilliseconds);

    Console.WriteLine($"\nQuery: \"{queryText}\"");
    Console.WriteLine($"Results ({results.Count} found in {sw.ElapsedMilliseconds}ms):");
    foreach (var result in results)
    {
        Console.WriteLine($"  [{result.Score:F4}] {result.Title} [{result.Category}]");
        Console.WriteLine($"    \"{result.Snippet}\"");
    }
}

// Step 6 - Hybrid search demo
Console.WriteLine("\n--- Hybrid Search (Semantic + Keyword) ---");
var hybridQueries = new[]
{
    "machine learning algorithms",
    "Kubernetes deployment",
    "data visualization techniques"
};

foreach (var queryText in hybridQueries)
{
    var sw = Stopwatch.StartNew();
    var queryEmbedding = GenerateEmbedding(queryText);
    var results = HybridSearch(queryText, queryEmbedding, limit: 3);
    sw.Stop();

    RecordLatency(sw.Elapsed.TotalMilliseconds);

    Console.WriteLine($"\nQuery: \"{queryText}\"");
    Console.WriteLine($"Results ({results.Count} found in {sw.ElapsedMilliseconds}ms):");
    foreach (var result in results)
    {
        Console.WriteLine($"  [{result.Score:F4}] {result.Title} [{result.Category}]");
        Console.WriteLine($"    \"{result.Snippet}\"");
    }
}

// Step 7 - Category-filtered search
Console.WriteLine("\n--- Category-Filtered Search ---");
var categoryQuery = "deployment and scaling";
var queryEmbed = GenerateEmbedding(categoryQuery);

Console.WriteLine($"Query: \"{categoryQuery}\"");

Console.WriteLine("\nFilter: category = 'infrastructure'");
var infraResults = Search(queryEmbed, limit: 3, minScore: 0.2, category: "infrastructure");
foreach (var result in infraResults)
{
    Console.WriteLine($"  [{result.Score:F4}] {result.Title}");
}

Console.WriteLine("\nFilter: category = 'technology'");
var techResults = Search(queryEmbed, limit: 3, minScore: 0.2, category: "technology");
foreach (var result in techResults)
{
    Console.WriteLine($"  [{result.Score:F4}] {result.Title}");
}

// Step 8 - Print metrics
Console.WriteLine("\n--- Search Performance Metrics ---");
PrintMetrics();

// Display index statistics
Console.WriteLine("\n--- Index Statistics ---");
Console.WriteLine($"Total documents: {searchIndex.Count}");
Console.WriteLine($"Vector dimensions: {vectorDimensions}");
var categories = searchIndex.Values
    .GroupBy(d => d.Category)
    .Select(g => $"{g.Key}: {g.Count()}");
Console.WriteLine($"Categories: {string.Join(", ", categories)}");

Console.WriteLine();
Console.WriteLine("--- Key Takeaways ---");
Console.WriteLine("1. Semantic search finds documents by meaning, not just keywords");
Console.WriteLine("2. Hybrid search combines semantic and keyword for best results");
Console.WriteLine("3. Real-time indexing with Surgewave enables live document updates");
Console.WriteLine("4. Track search latency metrics (P50, P95, P99) for performance");
Console.WriteLine("5. Filter by minimum score to avoid irrelevant results");

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// Record types
public record SearchableDocument(string Id, string Title, string Content, string Category, DateTime CreatedAt);
public record IndexedDocument(string Id, string Title, string Content, string Category, DateTime CreatedAt, float[] Embedding);
public record SearchResult(string DocumentId, string Title, string Snippet, double Score, string Category);

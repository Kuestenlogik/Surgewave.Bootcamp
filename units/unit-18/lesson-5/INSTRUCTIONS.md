# Lesson 22.5: Instructions

Follow these step-by-step instructions to implement a complete semantic search system.

## Step 1: Define the Search Domain Model

Create types for search requests and results:

```csharp
public record SearchableDocument(
    string Id,
    string Title,
    string Content,
    string Category,
    DateTime CreatedAt,
    Dictionary<string, string> Metadata
);

public record IndexedDocument(
    string Id,
    string Title,
    string Content,
    string Category,
    DateTime CreatedAt,
    float[] Embedding
);

public record SearchQuery(
    string Id,
    string Query,
    string? Category,
    int Limit,
    double MinScore
);

public record SearchResult(
    string DocumentId,
    string Title,
    string Snippet,
    double Score,
    string Category
);

public record SearchResponse(
    string QueryId,
    string Query,
    List<SearchResult> Results,
    int TotalMatches,
    double SearchTimeMs
);
```

## Step 2: Build the Search Index

Create an in-memory search index (or use Qdrant):

```csharp
public class SemanticSearchIndex
{
    private readonly Dictionary<string, IndexedDocument> _documents = new();
    private readonly object _lock = new();

    public void Index(IndexedDocument document)
    {
        lock (_lock)
        {
            _documents[document.Id] = document;
        }
    }

    public void IndexBatch(IEnumerable<IndexedDocument> documents)
    {
        lock (_lock)
        {
            foreach (var doc in documents)
            {
                _documents[doc.Id] = doc;
            }
        }
    }

    public List<SearchResult> Search(
        float[] queryEmbedding,
        int limit = 10,
        double minScore = 0.5,
        string? category = null)
    {
        lock (_lock)
        {
            var results = _documents.Values
                .Where(d => category == null || d.Category == category)
                .Select(d => new
                {
                    Doc = d,
                    Score = CosineSimilarity(queryEmbedding, d.Embedding)
                })
                .Where(x => x.Score >= minScore)
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => new SearchResult(
                    x.Doc.Id,
                    x.Doc.Title,
                    CreateSnippet(x.Doc.Content, 150),
                    x.Score,
                    x.Doc.Category
                ))
                .ToList();

            return results;
        }
    }

    private static string CreateSnippet(string content, int maxLength)
    {
        if (content.Length <= maxLength) return content;
        return content[..maxLength].TrimEnd() + "...";
    }
}
```

## Step 3: Create the Document Indexing Pipeline

Stream documents through Surgewave for indexing:

```csharp
// Producer for new documents
await using var docProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

// Index consumer
await using var indexConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "search-indexer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await indexConsumer.SubscribeAsync(default, "documents");

// Indexing loop
Console.WriteLine("--- Starting Document Indexing ---");
while (!cancellationToken.IsCancellationRequested)
{
    var result = await indexConsumer.ConsumeAsync(cancellationToken);
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<SearchableDocument>(result.Value)!;

    // Generate embedding
    var embedding = GenerateEmbedding(doc.Content);

    // Index document
    var indexed = new IndexedDocument(
        doc.Id,
        doc.Title,
        doc.Content,
        doc.Category,
        doc.CreatedAt,
        embedding
    );

    searchIndex.Index(indexed);
    Console.WriteLine($"Indexed: {doc.Title}");

    await indexConsumer.CommitAsync(result);
}
```

## Step 4: Create the Search Query Pipeline

Process search queries through Surgewave:

```csharp
// Query producer
await using var queryProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

// Query consumer
await using var searchConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "search-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await searchConsumer.SubscribeAsync(default, "search-queries");

// Results producer
await using var resultsProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

// Search processing loop
while (!cancellationToken.IsCancellationRequested)
{
    var queryResult = await searchConsumer.ConsumeAsync(cancellationToken);
    if (queryResult == null) continue;

    var sw = Stopwatch.StartNew();
    var query = JsonSerializer.Deserialize<SearchQuery>(queryResult.Value)!;

    // Generate query embedding
    var queryEmbedding = GenerateEmbedding(query.Query);

    // Execute search
    var results = searchIndex.Search(
        queryEmbedding,
        query.Limit,
        query.MinScore,
        query.Category
    );

    sw.Stop();

    // Create response
    var response = new SearchResponse(
        query.Id,
        query.Query,
        results,
        results.Count,
        sw.Elapsed.TotalMilliseconds
    );

    // Publish results
    await resultsProducer.ProduceAsync(
        "search-results",
        query.Id,
        JsonSerializer.Serialize(response)
    );

    Console.WriteLine($"Search completed: \"{query.Query}\" -> {results.Count} results in {sw.ElapsedMilliseconds}ms");
}
```

## Step 5: Implement Hybrid Search

Combine semantic and keyword search:

```csharp
public class HybridSearchIndex
{
    private readonly SemanticSearchIndex _semanticIndex;
    private readonly double _semanticWeight;
    private readonly double _keywordWeight;

    public HybridSearchIndex(double semanticWeight = 0.7, double keywordWeight = 0.3)
    {
        _semanticIndex = new SemanticSearchIndex();
        _semanticWeight = semanticWeight;
        _keywordWeight = keywordWeight;
    }

    public List<SearchResult> HybridSearch(
        string query,
        float[] queryEmbedding,
        int limit = 10)
    {
        // Get semantic results
        var semanticResults = _semanticIndex.Search(queryEmbedding, limit * 2, 0.3);

        // Get keyword results
        var keywordResults = KeywordSearch(query, limit * 2);

        // Combine with Reciprocal Rank Fusion
        var combined = ReciprocalRankFusion(semanticResults, keywordResults);

        return combined.Take(limit).ToList();
    }

    private List<SearchResult> KeywordSearch(string query, int limit)
    {
        var keywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return _documents.Values
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

    private double CalculateKeywordScore(IndexedDocument doc, string[] keywords)
    {
        var content = doc.Content.ToLowerInvariant();
        var title = doc.Title.ToLowerInvariant();

        var matches = keywords.Count(k => content.Contains(k) || title.Contains(k));
        return (double)matches / keywords.Length;
    }

    private List<SearchResult> ReciprocalRankFusion(
        List<SearchResult> semanticResults,
        List<SearchResult> keywordResults,
        int k = 60)
    {
        var scores = new Dictionary<string, double>();

        // Add semantic scores
        for (int i = 0; i < semanticResults.Count; i++)
        {
            var docId = semanticResults[i].DocumentId;
            scores[docId] = _semanticWeight / (k + i + 1);
        }

        // Add keyword scores
        for (int i = 0; i < keywordResults.Count; i++)
        {
            var docId = keywordResults[i].DocumentId;
            var rrf = _keywordWeight / (k + i + 1);
            scores[docId] = scores.GetValueOrDefault(docId) + rrf;
        }

        // Combine results
        var allResults = semanticResults.Concat(keywordResults)
            .GroupBy(r => r.DocumentId)
            .Select(g => g.First() with { Score = scores[g.Key] })
            .OrderByDescending(r => r.Score)
            .ToList();

        return allResults;
    }
}
```

## Step 6: Add Search Result Highlighting

Highlight matching terms in snippets:

```csharp
string CreateHighlightedSnippet(string content, string query, int maxLength = 150)
{
    var keywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var lowerContent = content.ToLowerInvariant();

    // Find best starting position (where most keywords match)
    var bestStart = 0;
    var bestScore = 0;

    for (int i = 0; i < content.Length - maxLength; i += 20)
    {
        var window = lowerContent.Substring(i, Math.Min(maxLength, content.Length - i));
        var score = keywords.Count(k => window.Contains(k));
        if (score > bestScore)
        {
            bestScore = score;
            bestStart = i;
        }
    }

    var snippet = content.Substring(bestStart, Math.Min(maxLength, content.Length - bestStart));

    // Add ellipsis
    if (bestStart > 0) snippet = "..." + snippet;
    if (bestStart + maxLength < content.Length) snippet += "...";

    return snippet;
}
```

## Step 7: Measure Search Performance

Track and report search metrics:

```csharp
public class SearchMetrics
{
    private readonly List<double> _latencies = new();
    private readonly object _lock = new();

    public void RecordSearch(double latencyMs, int resultCount)
    {
        lock (_lock)
        {
            _latencies.Add(latencyMs);
        }
    }

    public SearchMetricsReport GetReport()
    {
        lock (_lock)
        {
            if (_latencies.Count == 0)
                return new SearchMetricsReport(0, 0, 0, 0, 0);

            var sorted = _latencies.OrderBy(x => x).ToList();
            return new SearchMetricsReport(
                TotalQueries: _latencies.Count,
                AvgLatencyMs: _latencies.Average(),
                P50LatencyMs: sorted[(int)(sorted.Count * 0.5)],
                P95LatencyMs: sorted[(int)(sorted.Count * 0.95)],
                P99LatencyMs: sorted[(int)(sorted.Count * 0.99)]
            );
        }
    }
}

public record SearchMetricsReport(
    int TotalQueries,
    double AvgLatencyMs,
    double P50LatencyMs,
    double P95LatencyMs,
    double P99LatencyMs
);
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Complete semantic search system
- Real-time document indexing via Surgewave
- Search query processing pipeline
- Hybrid search with RRF
- Performance metrics tracking

## Sample Output

```
=== Surgewave Bootcamp: Lesson 22.5 - Semantic Search ===

Surgewave broker started on port 12345

--- Indexing Documents ---
Indexed: Introduction to Machine Learning
Indexed: Deep Learning with Neural Networks
Indexed: Cloud Computing Fundamentals
Indexed: Natural Language Processing Guide
Indexed: Data Science Best Practices
Total documents indexed: 5

--- Processing Search Queries ---

Query: "How do neural networks learn?"
Results (3 found in 12ms):
  [0.9123] Deep Learning with Neural Networks
    "Neural networks learn through backpropagation..."
  [0.8456] Introduction to Machine Learning
    "Machine learning algorithms enable systems..."
  [0.7234] Natural Language Processing Guide
    "NLP uses deep learning for language..."

Query: "cloud infrastructure"
Results (2 found in 8ms):
  [0.8901] Cloud Computing Fundamentals
    "Cloud computing provides scalable infrastructure..."
  [0.6123] Data Science Best Practices
    "Deploy models to cloud platforms..."

--- Search Metrics ---
Total queries: 10
Average latency: 15ms
P50 latency: 12ms
P95 latency: 25ms
P99 latency: 45ms

Lesson complete!
```

## Knowledge Check

1. What is the main advantage of semantic search over keyword search?
2. How does Reciprocal Rank Fusion combine results from multiple sources?
3. Why should you retrieve more results than the requested limit before filtering?
4. What metrics should you track to measure search quality?
5. How can caching improve search performance?

---

**Next:** [Unit 18 Project: AI Document Search](../project/README.md)

# Knowledge Assessment: Lesson 22.5 - Semantic Search Implementation

## Multiple Choice

### 1. What is the main advantage of semantic search over keyword search?
- [ ] A) It's faster to execute
- [ ] B) It understands meaning and context, not just keywords
- [ ] C) It requires less storage
- [ ] D) It doesn't need embeddings

<details>
<summary>Answer</summary>
**B)** Semantic search understands meaning and context, finding relevant documents even when they don't contain the exact query keywords. For example, "car problems" can match "automobile issues" or "vehicle trouble".
</details>

### 2. What is Reciprocal Rank Fusion (RRF) used for?
- [ ] A) Generating embeddings
- [ ] B) Compressing search results
- [ ] C) Combining results from multiple search methods
- [ ] D) Encrypting search queries

<details>
<summary>Answer</summary>
**C)** RRF is a technique for combining ranked results from multiple search methods (e.g., semantic and keyword search) into a single ranked list. It uses the formula: score = sum(1 / (k + rank)) across all methods.
</details>

### 3. Why retrieve more results than needed before applying the final limit?
- [ ] A) To increase latency
- [ ] B) To have options for filtering and re-ranking
- [ ] C) To fill up memory
- [ ] D) To generate more embeddings

<details>
<summary>Answer</summary>
**B)** Retrieving more results (e.g., top-20 when only 10 needed) provides room for additional filtering, re-ranking with a more accurate model, or removing low-quality matches before returning the final results.
</details>

### 4. Which latency metric is most important for user experience?
- [ ] A) P99 (99th percentile)
- [ ] B) Average latency
- [ ] C) Maximum latency
- [ ] D) P50 (50th percentile / median)

<details>
<summary>Answer</summary>
**D)** P50 (median) latency represents the typical user experience - half of all requests complete faster than this. While P99 is important for worst-case scenarios, P50 indicates what most users actually experience.
</details>

### 5. What is a practical weight distribution for hybrid search?
- [ ] A) 100% semantic, 0% keyword
- [ ] B) 50% semantic, 50% keyword
- [ ] C) 70% semantic, 30% keyword
- [ ] D) 0% semantic, 100% keyword

<details>
<summary>Answer</summary>
**C)** A 70/30 split favoring semantic search is common. Semantic search provides the main relevance signal, while keyword search ensures exact matches are found. The optimal ratio depends on your use case and should be tuned through testing.
</details>

## True/False

### 6. Query caching eliminates the need for embedding generation on frequently asked questions.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Caching query embeddings (and potentially search results) can significantly improve performance for frequently asked questions. The same query always produces the same embedding, making it safe to cache.
</details>

### 7. Semantic search completely eliminates the need for keyword search.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Keyword search still has value for exact matches (product codes, names), rare terms, and as a fallback when semantic search fails. Hybrid approaches combining both typically perform best.
</details>

## Code Challenge

### 8. What issues exist in this search implementation?

```csharp
public List<SearchResult> Search(string query)
{
    var embedding = GenerateEmbedding(query);

    var results = _documents.Values
        .Select(d => new { Doc = d, Score = CosineSimilarity(embedding, d.Embedding) })
        .OrderByDescending(x => x.Score)
        .Take(10)
        .ToList();

    return results.Select(r => new SearchResult(r.Doc.Id, r.Doc.Title, r.Score)).ToList();
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **No minimum score filter**: Returns results even if similarity is very low (irrelevant)
2. **No thread safety**: Could have issues with concurrent access to `_documents`
3. **No snippet generation**: Doesn't provide content preview
4. **No timing/metrics**: Can't measure search performance
5. **No category filtering**: Can't narrow search scope
6. **Synchronous embedding**: Blocks on embedding generation

Improved version:

```csharp
public async Task<SearchResponse> SearchAsync(SearchQuery query)
{
    var sw = Stopwatch.StartNew();

    // Generate embedding (could be cached)
    var embedding = await GenerateEmbeddingAsync(query.Query);

    List<SearchResult> results;
    lock (_lock)
    {
        results = _documents.Values
            .Where(d => query.Category == null || d.Category == query.Category)
            .Select(d => new { Doc = d, Score = CosineSimilarity(embedding, d.Embedding) })
            .Where(x => x.Score >= query.MinScore)  // Filter low scores
            .OrderByDescending(x => x.Score)
            .Take(query.Limit)
            .Select(r => new SearchResult(
                r.Doc.Id,
                r.Doc.Title,
                CreateSnippet(r.Doc.Content, query.Query),  // Add snippet
                r.Score,
                r.Doc.Category
            ))
            .ToList();
    }

    sw.Stop();
    _metrics.RecordSearch(sw.Elapsed.TotalMilliseconds, results.Count);

    return new SearchResponse(query.Id, query.Query, results, results.Count, sw.Elapsed.TotalMilliseconds);
}
```
</details>

### 9. What's wrong with this search result snippet generation?

```csharp
string GetSnippet(string content)
{
    return content.Substring(0, 200);
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **No bounds checking**: Will throw if content < 200 characters
2. **Always starts at beginning**: Doesn't show the most relevant part
3. **No query context**: Doesn't highlight or center on matching terms
4. **Cuts mid-word**: May truncate in the middle of a word
5. **No ellipsis**: Doesn't indicate truncation

Improved version:

```csharp
string GetSnippet(string content, string query, int maxLength = 200)
{
    if (string.IsNullOrEmpty(content))
        return "";

    if (content.Length <= maxLength)
        return content;

    // Find best starting position based on query terms
    var keywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var lowerContent = content.ToLowerInvariant();

    var bestStart = 0;
    var bestScore = 0;

    // Scan for window with most keyword matches
    for (int i = 0; i < content.Length - maxLength; i += 20)
    {
        var endPos = Math.Min(i + maxLength, content.Length);
        var window = lowerContent[i..endPos];
        var score = keywords.Count(k => window.Contains(k));

        if (score > bestScore)
        {
            bestScore = score;
            bestStart = i;
        }
    }

    // Extract snippet
    var endIndex = Math.Min(bestStart + maxLength, content.Length);
    var snippet = content[bestStart..endIndex];

    // Try to break at word boundary
    if (endIndex < content.Length)
    {
        var lastSpace = snippet.LastIndexOf(' ');
        if (lastSpace > maxLength * 0.7)
        {
            snippet = snippet[..lastSpace];
        }
    }

    // Add ellipsis
    var prefix = bestStart > 0 ? "..." : "";
    var suffix = endIndex < content.Length ? "..." : "";

    return prefix + snippet.Trim() + suffix;
}
```
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson

# Knowledge Assessment: Lesson 22.3 - Qdrant Vector Database

## Multiple Choice

### 1. What is the default port for Qdrant's REST API?
- [ ] A) 5432
- [ ] B) 6333
- [ ] C) 8080
- [ ] D) 9200

<details>
<summary>Answer</summary>
**B)** Qdrant's REST API runs on port 6333 by default. Port 6334 is used for gRPC.
</details>

### 2. Which distance metric is recommended for text embeddings?
- [ ] A) Euclidean
- [ ] B) Manhattan
- [ ] C) Cosine
- [ ] D) Hamming

<details>
<summary>Answer</summary>
**C)** Cosine distance is recommended for text embeddings because it measures the angle between vectors, focusing on direction rather than magnitude. This is ideal for normalized text embeddings.
</details>

### 3. What is the purpose of HNSW (Hierarchical Navigable Small World) index?
- [ ] A) Compress vectors to save storage
- [ ] B) Enable fast approximate nearest neighbor search
- [ ] C) Encrypt vectors for security
- [ ] D) Convert text to vectors

<details>
<summary>Answer</summary>
**B)** HNSW enables fast approximate nearest neighbor search with O(log n) complexity instead of O(n) brute force search. It builds a graph-based index for efficient similarity search.
</details>

### 4. What is a "payload" in Qdrant terminology?
- [ ] A) The vector data
- [ ] B) The collection configuration
- [ ] C) Metadata associated with a vector
- [ ] D) The search query

<details>
<summary>Answer</summary>
**C)** A payload is metadata associated with a vector (point). It contains additional information like title, category, date, etc. that can be used for filtering and returned with search results.
</details>

### 5. Why is filtered vector search beneficial?
- [ ] A) It increases the accuracy of embeddings
- [ ] B) It reduces search space and improves relevance
- [ ] C) It compresses the vector data
- [ ] D) It automatically generates embeddings

<details>
<summary>Answer</summary>
**B)** Filtered vector search reduces the search space by applying metadata filters first, then searching only within the filtered subset. This improves both performance and relevance.
</details>

## True/False

### 6. Vector databases can completely replace traditional SQL databases for all use cases.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Vector databases are optimized for similarity search, not general-purpose queries. Traditional databases are still better for exact matches, range queries, transactions, and complex joins. Often both are used together.
</details>

### 7. Batch upserting points to Qdrant is more efficient than inserting one at a time.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Batch operations reduce HTTP overhead and allow Qdrant to optimize index updates. Always batch upserts when inserting multiple points.
</details>

## Code Challenge

### 8. What's wrong with this Qdrant collection creation code?

```csharp
public async Task CreateCollection(string name)
{
    var request = new
    {
        vectors = new { size = 1536 }  // Missing distance metric
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json);

    await _httpClient.PutAsync($"{_baseUrl}/collections/{name}", content);
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Missing distance metric**: The `vectors` configuration requires a `distance` parameter
2. **Missing Content-Type**: StringContent needs the media type specified
3. **No error handling**: Should check response status
4. **No return value**: Caller doesn't know if creation succeeded

Corrected version:

```csharp
public async Task<bool> CreateCollection(string name, int vectorSize = 1536, string distance = "Cosine")
{
    var request = new
    {
        vectors = new
        {
            size = vectorSize,
            distance = distance  // Required: "Cosine", "Euclid", or "Dot"
        }
    };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PutAsync($"{_baseUrl}/collections/{name}", content);
    return response.IsSuccessStatusCode;
}
```
</details>

### 9. What potential issue exists in this search implementation?

```csharp
public async Task<List<SearchResult>> Search(string query)
{
    var documents = await GetAllDocuments();  // Returns 1 million documents
    var queryEmbedding = await GenerateEmbedding(query);

    var results = documents
        .AsParallel()
        .Select(d => new { Doc = d, Score = CosineSimilarity(queryEmbedding, d.Embedding) })
        .OrderByDescending(x => x.Score)
        .Take(10)
        .Select(x => new SearchResult(x.Doc.Id, x.Score))
        .ToList();

    return results;
}
```

<details>
<summary>Answer</summary>
This is a brute-force search that:

1. **Loads all documents into memory**: 1 million documents with 1536-dim embeddings = ~6GB memory
2. **O(n) complexity**: Compares query against every document
3. **No indexing**: Doesn't use any optimization for similarity search
4. **Network overhead**: Fetches all documents from storage

This is exactly why vector databases like Qdrant exist:

```csharp
public async Task<List<SearchResult>> Search(string query)
{
    var queryEmbedding = await GenerateEmbedding(query);

    // O(log n) with HNSW index, no memory issues
    var results = await _qdrantClient.SearchAsync(
        collection: "documents",
        vector: queryEmbedding,
        limit: 10);

    return results
        .Select(r => new SearchResult(r.Id, r.Score))
        .ToList();
}
```

Vector databases use specialized indexes (HNSW, IVF) to achieve O(log n) or better search complexity, and they manage memory efficiently.
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson

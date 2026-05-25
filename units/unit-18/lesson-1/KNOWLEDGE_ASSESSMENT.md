# Knowledge Assessment: Lesson 22.1 - OpenAI Embeddings

## Multiple Choice

### 1. What is an embedding?
- [ ] A) A compressed version of a document
- [ ] B) A numerical vector representation that captures semantic meaning
- [ ] C) A hash of the document content
- [ ] D) An encrypted version of text

<details>
<summary>Answer</summary>
**B)** An embedding is a numerical vector representation that captures the semantic meaning of text. Similar texts produce similar embeddings, enabling semantic search and comparison.
</details>

### 2. How many dimensions does OpenAI's text-embedding-3-small model produce?
- [ ] A) 384
- [ ] B) 768
- [ ] C) 1536
- [ ] D) 3072

<details>
<summary>Answer</summary>
**C)** OpenAI's text-embedding-3-small model produces 1536-dimensional vectors. The text-embedding-3-large model produces 3072 dimensions for higher accuracy.
</details>

### 3. Why is cosine similarity preferred over Euclidean distance for text embeddings?
- [ ] A) It's faster to compute
- [ ] B) It measures angle between vectors, focusing on direction rather than magnitude
- [ ] C) It always returns a value between 0 and 1
- [ ] D) It handles missing values better

<details>
<summary>Answer</summary>
**B)** Cosine similarity measures the angle between vectors, focusing on direction rather than magnitude. This is important because two texts can be semantically similar even if their embeddings have different magnitudes (lengths).
</details>

### 4. What is the maximum input size for OpenAI's embedding models?
- [ ] A) 1024 tokens
- [ ] B) 4096 tokens
- [ ] C) 8191 tokens
- [ ] D) 16384 tokens

<details>
<summary>Answer</summary>
**C)** OpenAI's embedding models accept up to 8191 tokens of input. Longer documents must be chunked into smaller pieces.
</details>

### 5. Which cosine similarity score indicates the texts are most semantically similar?
- [ ] A) 0.15
- [ ] B) 0.45
- [ ] C) 0.72
- [ ] D) 0.91

<details>
<summary>Answer</summary>
**D)** A cosine similarity of 0.91 indicates very high semantic similarity. Scores close to 1.0 mean the texts are nearly identical in meaning, while scores near 0 indicate unrelated content.
</details>

## True/False

### 6. Embeddings can be regenerated from the vector back to the original text.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Embeddings are a lossy transformation. You cannot reconstruct the original text from an embedding vector. The embedding captures semantic meaning but discards the specific wording.
</details>

### 7. Batching multiple texts into a single API call reduces the total number of API requests and can lower costs.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - OpenAI's embedding API accepts up to 2048 texts in a single request. Batching reduces API calls, network overhead, and can improve throughput while staying within rate limits.
</details>

## Code Challenge

### 8. What's wrong with this embedding comparison code?

```csharp
static double CompareEmbeddings(float[] a, float[] b)
{
    double sum = 0;
    for (int i = 0; i < a.Length; i++)
    {
        sum += a[i] * b[i];
    }
    return sum;  // Return dot product
}

// Usage
var score = CompareEmbeddings(embedding1, embedding2);
if (score > 0.8)
{
    Console.WriteLine("Documents are similar");
}
```

<details>
<summary>Answer</summary>
The code uses raw dot product instead of cosine similarity. The dot product is not normalized, so the score depends on the magnitude of the vectors, not just their direction. Two similar texts with different-length embeddings would produce inconsistent scores.

Correct implementation:

```csharp
static double CompareEmbeddings(float[] a, float[] b)
{
    double dotProduct = 0, normA = 0, normB = 0;

    for (int i = 0; i < a.Length; i++)
    {
        dotProduct += a[i] * b[i];
        normA += a[i] * a[i];
        normB += b[i] * b[i];
    }

    // Normalize by vector magnitudes to get cosine similarity
    return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
}
```

Cosine similarity normalizes the dot product by the magnitudes of both vectors, giving a score between -1 and 1 that depends only on the angle between vectors.
</details>

### 9. What potential issues does this embedding generation code have?

```csharp
var documents = GetAllDocuments();  // Returns 10,000 documents

foreach (var doc in documents)
{
    var embedding = await embeddingClient.GenerateEmbeddingAsync(doc.Content);
    await SaveEmbedding(doc.Id, embedding);
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **No batching**: Makes 10,000 separate API calls instead of batching (up to 2048 texts per call)
2. **No rate limit handling**: Will likely hit OpenAI's rate limits with 10,000 sequential calls
3. **No error handling**: A single failure stops the entire process
4. **No caching**: If the process restarts, all embeddings are regenerated
5. **Sequential processing**: Could use parallelism (with rate limiting) for better throughput

Improved version:

```csharp
var documents = GetAllDocuments();
var batchSize = 100;  // Process in batches

foreach (var batch in documents.Chunk(batchSize))
{
    try
    {
        var texts = batch.Select(d => d.Content).ToArray();
        var embeddings = await embeddingClient.GenerateBatchEmbeddingsAsync(texts);

        for (int i = 0; i < batch.Length; i++)
        {
            await SaveEmbedding(batch[i].Id, embeddings[i]);
        }
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
    {
        await Task.Delay(TimeSpan.FromSeconds(60));  // Wait and retry
        // Retry logic here
    }
}
```
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson

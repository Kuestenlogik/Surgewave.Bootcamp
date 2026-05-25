# Knowledge Assessment: Lesson 22.2 - Ollama Local LLM Integration

## Multiple Choice

### 1. What is the default port for Ollama's API server?
- [ ] A) 8080
- [ ] B) 11434
- [ ] C) 3000
- [ ] D) 5000

<details>
<summary>Answer</summary>
**B)** Ollama runs on port 11434 by default. The API is accessible at http://localhost:11434.
</details>

### 2. Which Ollama model is best suited for generating text embeddings?
- [ ] A) llama3:8b
- [ ] B) mistral:7b
- [ ] C) nomic-embed-text
- [ ] D) codellama:7b

<details>
<summary>Answer</summary>
**C)** nomic-embed-text is specifically designed for generating text embeddings (768 dimensions). The other models are chat/completion models, not embedding models.
</details>

### 3. What is a key advantage of using local LLMs for sensitive data?
- [ ] A) Faster processing speed
- [ ] B) Better model quality
- [ ] C) Data never leaves your network
- [ ] D) Lower hardware requirements

<details>
<summary>Answer</summary>
**C)** The main advantage for sensitive data is that it never leaves your network. All processing happens locally, eliminating data privacy concerns associated with cloud APIs.
</details>

### 4. How many dimensions does nomic-embed-text produce compared to OpenAI's text-embedding-3-small?
- [ ] A) nomic: 384, OpenAI: 1536
- [ ] B) nomic: 768, OpenAI: 1536
- [ ] C) nomic: 1536, OpenAI: 768
- [ ] D) nomic: 768, OpenAI: 768

<details>
<summary>Answer</summary>
**B)** nomic-embed-text produces 768-dimensional vectors, while OpenAI's text-embedding-3-small produces 1536 dimensions. More dimensions can capture more semantic nuance but require more storage.
</details>

### 5. What HTTP method is used for Ollama's embedding API?
- [ ] A) GET
- [ ] B) POST
- [ ] C) PUT
- [ ] D) PATCH

<details>
<summary>Answer</summary>
**B)** POST is used for all Ollama API endpoints (/api/embeddings, /api/generate, /api/chat). The request body contains the model and prompt/messages.
</details>

## True/False

### 6. Local LLMs running on Ollama have no per-request costs after the initial hardware investment.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Once you have the hardware and have downloaded the models, there are no per-request costs. This makes local LLMs cost-effective for high-volume applications.
</details>

### 7. Ollama models are always faster than cloud API calls.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Local model speed depends heavily on your hardware. Without a GPU, local inference can be significantly slower than cloud APIs. With a good GPU, local inference can be competitive or faster, especially when considering network latency.
</details>

## Code Challenge

### 8. What's wrong with this Ollama client implementation?

```csharp
public class OllamaClient
{
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task<string> GenerateAsync(string prompt)
    {
        var request = new { model = "llama3", prompt = prompt };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json);

        var response = await _httpClient.PostAsync(
            "http://localhost:11434/api/generate",
            content);

        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Missing Content-Type header**: StringContent needs the media type specified
2. **No timeout**: LLM inference can take a long time; default timeout may cause failures
3. **No error handling**: Should check response status and handle errors
4. **Returns raw JSON**: Should parse the response and extract the generated text
5. **Streaming not handled**: API returns streaming responses by default

Corrected version:

```csharp
public class OllamaClient
{
    private readonly HttpClient _httpClient;

    public OllamaClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)  // Long timeout for inference
        };
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        var request = new { model = "llama3", prompt = prompt, stream = false };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            "http://localhost:11434/api/generate",
            content);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(result);
        return parsed.RootElement.GetProperty("response").GetString() ?? "";
    }
}
```
</details>

### 9. What potential issue exists in this document processing pipeline?

```csharp
var documents = await GetDocumentsAsync();  // Returns 1000 documents

foreach (var doc in documents)
{
    var embedding = await ollamaClient.GenerateEmbeddingAsync(doc.Content);
    var summary = await ollamaClient.GenerateAsync($"Summarize: {doc.Content}");

    await SaveEnrichedDocument(doc, embedding, summary);
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Sequential processing**: Each document waits for the previous one. With local LLM, this could take hours for 1000 documents
2. **No progress reporting**: User has no visibility into processing status
3. **No error recovery**: If one document fails, the entire batch fails
4. **No batching**: While Ollama doesn't batch embeddings like OpenAI, you could parallelize with limits
5. **No timeout handling**: Long documents might cause timeouts

Improved version:

```csharp
var documents = await GetDocumentsAsync();
var semaphore = new SemaphoreSlim(2);  // Limit concurrent requests (Ollama is single-threaded)
var processed = 0;

await Parallel.ForEachAsync(documents, async (doc, ct) =>
{
    await semaphore.WaitAsync(ct);
    try
    {
        var embedding = await ollamaClient.GenerateEmbeddingAsync(doc.Content);
        var summary = await ollamaClient.GenerateAsync($"Summarize: {doc.Content}");
        await SaveEnrichedDocument(doc, embedding, summary);

        var count = Interlocked.Increment(ref processed);
        if (count % 10 == 0)
        {
            Console.WriteLine($"Progress: {count}/{documents.Count}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to process {doc.Id}: {ex.Message}");
        // Continue with other documents
    }
    finally
    {
        semaphore.Release();
    }
});
```

Note: For CPU-only Ollama, concurrency > 1 may not help. Monitor resource usage to tune.
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson

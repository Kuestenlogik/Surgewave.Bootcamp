# Lesson 22.4: Instructions

Follow these step-by-step instructions to implement a RAG pipeline with Surgewave.

## Step 1: Implement Document Chunking

Create a chunker that splits documents into manageable pieces:

```csharp
public class DocumentChunker
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    public DocumentChunker(int chunkSize = 500, int overlap = 50)
    {
        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    public IEnumerable<DocumentChunk> ChunkDocument(Document document)
    {
        var text = document.Content;
        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        for (int i = 0; i < text.Length; i += _chunkSize - _overlap)
        {
            var end = Math.Min(i + _chunkSize, text.Length);
            var chunkText = text[i..end];

            // Try to break at sentence boundary
            if (end < text.Length)
            {
                var lastPeriod = chunkText.LastIndexOf('.');
                if (lastPeriod > _chunkSize / 2)
                {
                    chunkText = chunkText[..(lastPeriod + 1)];
                }
            }

            chunks.Add(new DocumentChunk(
                Id: $"{document.Id}-chunk-{chunkIndex}",
                DocumentId: document.Id,
                DocumentTitle: document.Title,
                Content: chunkText.Trim(),
                ChunkIndex: chunkIndex,
                TotalChunks: 0  // Updated after all chunks created
            ));

            chunkIndex++;
        }

        // Update total chunks count
        return chunks.Select(c => c with { TotalChunks = chunks.Count });
    }
}

public record DocumentChunk(
    string Id,
    string DocumentId,
    string DocumentTitle,
    string Content,
    int ChunkIndex,
    int TotalChunks
);
```

## Step 2: Index Chunks with Embeddings

Process documents through Surgewave and index chunks:

```csharp
var chunker = new DocumentChunker(chunkSize: 300, overlap: 50);

// Consume documents from Surgewave
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "rag-indexer";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await consumer.SubscribeAsync(default, "documents");

Console.WriteLine("--- Chunking and Indexing Documents ---");
var allChunks = new List<IndexedChunk>();

for (int i = 0; i < documents.Length; i++)
{
    var result = await consumer.ConsumeAsync();
    if (result == null) continue;

    var doc = JsonSerializer.Deserialize<Document>(result.Value)!;
    var chunks = chunker.ChunkDocument(doc);

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

        allChunks.Add(indexed);
        Console.WriteLine($"  Indexed: {chunk.DocumentTitle} (chunk {chunk.ChunkIndex + 1})");
    }
}
```

## Step 3: Implement Context Retrieval

Retrieve relevant chunks for a query:

```csharp
public class ContextRetriever
{
    private readonly List<IndexedChunk> _chunks;
    private readonly int _topK;

    public ContextRetriever(List<IndexedChunk> chunks, int topK = 5)
    {
        _chunks = chunks;
        _topK = topK;
    }

    public List<RetrievedContext> Retrieve(float[] queryEmbedding, double minScore = 0.5)
    {
        return _chunks
            .Select(c => new RetrievedContext(
                c.DocumentTitle,
                c.Content,
                CosineSimilarity(queryEmbedding, c.Embedding)))
            .Where(r => r.Score >= minScore)
            .OrderByDescending(r => r.Score)
            .Take(_topK)
            .ToList();
    }
}

public record RetrievedContext(string Source, string Content, double Score);
```

## Step 4: Build Augmented Prompts

Create prompts that include retrieved context:

```csharp
public class PromptBuilder
{
    private readonly string _systemPrompt;

    public PromptBuilder()
    {
        _systemPrompt = """
            You are a helpful assistant that answers questions based on the provided context.
            Always base your answers on the given documents.
            If the answer is not in the context, say "I don't have enough information to answer that."
            When possible, cite the source document.
            """;
    }

    public string BuildPrompt(string question, List<RetrievedContext> context)
    {
        var contextSection = new StringBuilder();
        contextSection.AppendLine("Here are relevant documents:\n");

        foreach (var (source, content, score) in context)
        {
            contextSection.AppendLine($"[Source: {source}]");
            contextSection.AppendLine(content);
            contextSection.AppendLine();
        }

        return $"""
            {_systemPrompt}

            {contextSection}

            Question: {question}

            Please provide a helpful answer based on the above context:
            """;
    }
}
```

## Step 5: Process RAG Queries Through Surgewave

Create a query processing pipeline:

```csharp
// Producer sends queries to Surgewave
await using var queryProducer = new SurgewaveProducer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
});

// Consumer processes queries
await using var queryConsumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "rag-processor";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

await queryConsumer.SubscribeAsync(default, "rag-queries");

// RAG processing loop
while (!cancellationToken.IsCancellationRequested)
{
    var queryResult = await queryConsumer.ConsumeAsync(cancellationToken);
    if (queryResult == null) continue;

    var query = JsonSerializer.Deserialize<RAGQuery>(queryResult.Value)!;

    // 1. Generate query embedding
    var queryEmbedding = GenerateEmbedding(query.Question);

    // 2. Retrieve relevant context
    var context = retriever.Retrieve(queryEmbedding);

    // 3. Build augmented prompt
    var prompt = promptBuilder.BuildPrompt(query.Question, context);

    // 4. Generate response (simulated or via LLM)
    var answer = await GenerateAnswer(prompt);

    // 5. Create response with citations
    var response = new RAGResponse(
        query.Id,
        query.Question,
        answer,
        context.Select(c => c.Source).Distinct().ToList()
    );

    // 6. Publish response
    await responseProducer.ProduceAsync("rag-responses", query.Id,
        JsonSerializer.Serialize(response));
}
```

## Step 6: Handle Edge Cases

Implement fallbacks for no-results scenarios:

```csharp
public string BuildPromptWithFallback(string question, List<RetrievedContext> context)
{
    if (context.Count == 0)
    {
        return $"""
            {_systemPrompt}

            No relevant documents were found for this query.
            Please let the user know that you don't have information about their question.

            Question: {question}
            """;
    }

    // Check if best score is low
    if (context[0].Score < 0.6)
    {
        return $"""
            {_systemPrompt}

            Note: The following documents have limited relevance to the question.
            Please indicate uncertainty in your answer.

            {FormatContext(context)}

            Question: {question}
            """;
    }

    return BuildPrompt(question, context);
}
```

## Step 7: Add Citations to Responses

Include source references in answers:

```csharp
public record RAGResponse(
    string QueryId,
    string Question,
    string Answer,
    List<string> Sources,
    DateTime Timestamp
)
{
    public string FormatWithCitations()
    {
        var response = new StringBuilder();
        response.AppendLine(Answer);
        response.AppendLine();
        response.AppendLine("Sources:");
        foreach (var source in Sources)
        {
            response.AppendLine($"  - {source}");
        }
        return response.ToString();
    }
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Document chunking with overlap
- Chunk embedding and indexing
- Context retrieval
- Prompt augmentation
- Response generation with citations
- Edge case handling

## Sample Output

```
=== Surgewave Bootcamp: Lesson 22.4 - RAG Pipeline ===

Surgewave broker started on port 12345

--- Chunking and Indexing Documents ---
  Indexed: AI in Healthcare (chunk 1)
  Indexed: AI in Healthcare (chunk 2)
  Indexed: Climate Change Research (chunk 1)
  Indexed: Quantum Computing (chunk 1)
  Indexed: Quantum Computing (chunk 2)
Total chunks indexed: 5

--- RAG Query Processing ---

Query: "How is AI being used in healthcare?"

Retrieved Context (3 chunks):
  [0.8934] AI in Healthcare (chunk 1)
  [0.8123] AI in Healthcare (chunk 2)
  [0.6245] Quantum Computing (chunk 1)

Augmented Prompt:
  System: You are a helpful assistant...
  Context: [3 relevant documents]
  Question: How is AI being used in healthcare?

Generated Answer:
  AI is transforming healthcare through improved diagnostics,
  personalized treatment plans, and predictive analytics.
  Machine learning algorithms analyze medical images and
  patient data to assist doctors in making better decisions.

  Sources:
    - AI in Healthcare

Lesson complete!
```

## Knowledge Check

1. What is the main benefit of RAG over vanilla LLMs?
2. Why is document chunking important for RAG?
3. What happens if no relevant documents are found?
4. How does chunk overlap improve retrieval quality?
5. Why should you retrieve more documents than you use in the prompt?

---

**Next:** [Lesson 22.5: Semantic Search Implementation](../lesson-5/README.md)

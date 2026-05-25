# Knowledge Assessment: Lesson 22.4 - RAG Pipeline Architecture

## Multiple Choice

### 1. What is the main purpose of RAG (Retrieval-Augmented Generation)?
- [ ] A) To train LLMs on new data
- [ ] B) To reduce LLM inference costs
- [ ] C) To ground LLM responses in specific documents
- [ ] D) To compress embeddings for storage

<details>
<summary>Answer</summary>
**C)** RAG grounds LLM responses in specific documents by retrieving relevant context before generation. This allows the LLM to answer questions based on your data, reducing hallucinations and enabling access to information not in its training data.
</details>

### 2. What is a typical chunk size for RAG applications?
- [ ] A) 50-100 tokens
- [ ] B) 256-512 tokens
- [ ] C) 1000-2000 tokens
- [ ] D) 5000+ tokens

<details>
<summary>Answer</summary>
**B)** 256-512 tokens is a common chunk size. Smaller chunks provide more precise retrieval but may lose context. Larger chunks provide more context but may include irrelevant information.
</details>

### 3. Why do we use overlapping chunks in RAG?
- [ ] A) To reduce storage requirements
- [ ] B) To prevent context loss at chunk boundaries
- [ ] C) To speed up embedding generation
- [ ] D) To improve LLM performance

<details>
<summary>Answer</summary>
**B)** Overlapping chunks prevent context loss at boundaries. Without overlap, important information spanning two chunks might be split, making it hard to retrieve. 10-20% overlap is typically recommended.
</details>

### 4. In a RAG pipeline, what should happen when no relevant documents are found?
- [ ] A) Return an error
- [ ] B) Use the LLM's general knowledge
- [ ] C) Indicate that information is not available
- [ ] D) Retrieve random documents

<details>
<summary>Answer</summary>
**C)** The system should indicate that it doesn't have information to answer the question. This prevents hallucination and maintains trust. The prompt should instruct the LLM to acknowledge when context is insufficient.
</details>

### 5. Why is it recommended to retrieve more documents than you use in the prompt?
- [ ] A) To fill up the context window
- [ ] B) To have backup options if top results are irrelevant
- [ ] C) To increase processing time
- [ ] D) To generate longer responses

<details>
<summary>Answer</summary>
**B)** Retrieving more documents (e.g., top-10) and using fewer (e.g., top-3-5) provides flexibility to filter out low-relevance results. This improves answer quality by ensuring only the most relevant context is included.
</details>

## True/False

### 6. RAG completely eliminates LLM hallucinations.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - RAG significantly reduces hallucinations by grounding responses in retrieved documents, but it doesn't eliminate them entirely. The LLM might still misinterpret context or make incorrect inferences.
</details>

### 7. The same embedding model should be used for both document indexing and query embedding in RAG.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Using the same embedding model ensures that documents and queries exist in the same vector space, making similarity search meaningful. Using different models would result in incompatible embeddings.
</details>

## Code Challenge

### 8. What issues exist in this RAG prompt construction?

```csharp
public string BuildPrompt(string question, List<Document> allDocuments)
{
    var context = string.Join("\n\n", allDocuments.Select(d => d.Content));

    return $"""
        Answer this question: {question}

        Here is some background information:
        {context}
        """;
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **No retrieval**: Uses ALL documents instead of relevant ones
2. **No relevance filtering**: Includes irrelevant documents
3. **No context limit**: May exceed LLM context window
4. **Question before context**: LLM may start answering before reading context
5. **No system instructions**: Missing guidance on how to use context
6. **No source attribution**: Documents not identified for citation

Improved version:

```csharp
public string BuildPrompt(string question, List<RetrievedContext> relevantDocs, int maxContextTokens = 3000)
{
    var systemPrompt = """
        You are a helpful assistant. Answer questions based ONLY on the provided context.
        If the answer is not in the context, say so. Always cite your sources.
        """;

    var contextBuilder = new StringBuilder();
    var currentTokens = 0;

    foreach (var doc in relevantDocs.OrderByDescending(d => d.Score))
    {
        var docText = $"[Source: {doc.Source}]\n{doc.Content}\n\n";
        var tokens = EstimateTokens(docText);

        if (currentTokens + tokens > maxContextTokens) break;

        contextBuilder.Append(docText);
        currentTokens += tokens;
    }

    return $"""
        {systemPrompt}

        Context:
        {contextBuilder}

        Question: {question}

        Answer (with citations):
        """;
}
```
</details>

### 9. What's wrong with this chunking implementation?

```csharp
public List<string> ChunkDocument(string content, int chunkSize)
{
    var chunks = new List<string>();

    for (int i = 0; i < content.Length; i += chunkSize)
    {
        var chunk = content.Substring(i, chunkSize);
        chunks.Add(chunk);
    }

    return chunks;
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Index out of bounds**: `Substring(i, chunkSize)` will throw when near end of string
2. **No overlap**: Context is lost at chunk boundaries
3. **Hard boundaries**: Splits mid-word and mid-sentence
4. **No metadata**: Returns just strings, losing document context
5. **Character-based**: Should consider tokens or semantic boundaries

Improved version:

```csharp
public List<DocumentChunk> ChunkDocument(Document doc, int chunkSize = 500, int overlap = 50)
{
    var chunks = new List<DocumentChunk>();
    var content = doc.Content;
    var chunkIndex = 0;

    for (int i = 0; i < content.Length; i += (chunkSize - overlap))
    {
        // Calculate end position safely
        var end = Math.Min(i + chunkSize, content.Length);
        var chunkText = content[i..end];

        // Try to break at sentence boundary (not mid-sentence)
        if (end < content.Length)
        {
            var lastPeriod = chunkText.LastIndexOf('.');
            var lastNewline = chunkText.LastIndexOf('\n');
            var breakPoint = Math.Max(lastPeriod, lastNewline);

            if (breakPoint > chunkSize / 2)
            {
                chunkText = chunkText[..(breakPoint + 1)];
            }
        }

        chunks.Add(new DocumentChunk(
            Id: $"{doc.Id}-{chunkIndex}",
            DocumentId: doc.Id,
            DocumentTitle: doc.Title,
            Content: chunkText.Trim(),
            ChunkIndex: chunkIndex++
        ));
    }

    return chunks;
}
```
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson

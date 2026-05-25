# Lesson 17.4: Building a RAG Pipeline

Retrieval-Augmented Generation (RAG) pipelines combine document retrieval with LLM generation. Surgewave provides a `rag-chatbot` template with 7 nodes, or you can build one from scratch.

### Option A: Start from the "rag-chatbot" Template

1. Navigate to **Pipelines** > **+ New Pipeline** > **From Template**
2. Select **RAG Chatbot** from the AI/ML category
3. The editor loads a pre-wired 7-node pipeline:

```
ChatEndpoint -> DocumentParser -> Embedder -> Retriever -> PromptBuilder -> LLM -> ChatResponse
```

### Option B: Build from Scratch

Add each node from the palette and wire them in sequence.

### Configure Each Node

Click each node and set its configuration:

**ChatEndpoint:**

| Setting | Value | Notes |
|---------|-------|-------|
| `port` | `8080` | HTTP port for chat API |
| `path` | `/chat` | Endpoint path |

No API key needed -- this node just receives user messages.

**DocumentParser:**

| Setting | Value | Notes |
|---------|-------|-------|
| `chunk.size` | `512` | Characters per chunk |
| `chunk.overlap` | `64` | Overlap between chunks for context |
| `formats` | `pdf,txt,md,html` | Supported document formats |

**Embedder:**

| Setting | Value | Notes |
|---------|-------|-------|
| `provider` | `openai` | Embedding provider |
| `model` | `text-embedding-3-small` | Embedding model |
| `api.key` | `sk-...` | Your OpenAI API key |
| `dimensions` | `1536` | Vector dimensions |

**Retriever:**

| Setting | Value | Notes |
|---------|-------|-------|
| `vector.store.url` | `http://localhost:6334` | Qdrant or compatible vector store |
| `collection` | `documents` | Collection name |
| `top.k` | `5` | Number of results to retrieve |
| `similarity.threshold` | `0.7` | Minimum similarity score |

**PromptBuilder:**

| Setting | Value |
|---------|-------|
| `template` | `Answer the question based on the following context:\n{context}\n\nQuestion: {query}` |
| `context.key` | `retrieved_docs` |
| `query.key` | `user_message` |

**LLM:**

| Setting | Value | Notes |
|---------|-------|-------|
| `provider` | `openai` | LLM provider |
| `model` | `gpt-4o` | Model name |
| `api.key` | `sk-...` | Your OpenAI API key |
| `temperature` | `0.7` | Creativity (0.0 = deterministic, 1.0 = creative) |
| `max.tokens` | `2048` | Maximum response length |

**ChatResponse:**

| Setting | Value | Notes |
|---------|-------|-------|
| `streaming` | `true` | Enable token-by-token streaming |

No API key needed -- this node sends responses back to the client.

### Test with the Chat Drawer

1. With the pipeline running, look for the **Chat FAB** (floating action button) in the bottom-right corner of the editor -- it appears when the pipeline contains a `ChatEndpoint` node
2. Click it to open the **ChatDrawer**
3. Type a message and press Enter
4. Watch the response stream in token-by-token

### Test with the Dedicated Chat Page

Navigate to `/pipelines/{id}/chat` for a full-page chat experience with conversation history.

---

[Back to Unit 17](../README.md)

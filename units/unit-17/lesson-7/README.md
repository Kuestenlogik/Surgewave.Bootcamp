# Lesson 17.7: Pipeline Chat & Streaming

Surgewave provides a full chat API for interacting with pipelines that contain a `ChatEndpoint` node. This enables conversational AI directly integrated with your pipeline processing.

### Chat API Endpoints

All chat endpoints are under `/api/pipelines/{pipelineId}/chat`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/pipelines/{id}/chat` | Send a message and wait for response |
| `POST` | `/api/pipelines/{id}/chat/stream` | Send a message and stream via SSE |
| `POST` | `/api/pipelines/{id}/chat/async` | Fire-and-forget message |
| `GET` | `/api/pipelines/{id}/chat/sessions` | List active sessions |
| `GET` | `/api/pipelines/{id}/chat/sessions/{sid}/history` | Get conversation history |
| `DELETE` | `/api/pipelines/{id}/chat/sessions/{sid}` | Delete a session |
| `GET` | `/api/pipelines/{id}/chat/topics` | Get signal/response topic names |

### Send a Message

```bash
curl -X POST http://localhost:5050/api/pipelines/my-chatbot/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is Surgewave?",
    "sessionId": "session-001"
  }'
```

**Response:**

```json
{
  "sessionId": "session-001",
  "messageId": "msg-abc123",
  "content": "Surgewave is a high-performance message broker...",
  "role": "assistant",
  "timestamp": "2026-03-14T10:30:00Z",
  "metadata": null
}
```

If `sessionId` is omitted, a new session is created automatically.

### Streaming via Server-Sent Events (SSE)

For token-by-token streaming responses:

```bash
curl -N -X POST http://localhost:5050/api/pipelines/my-chatbot/chat/stream \
  -H "Content-Type: application/json" \
  -d '{ "message": "Explain RAG in detail" }'
```

**SSE events:**

```
event: token
data: {"type":"token","content":"Retrieval"}

event: token
data: {"type":"token","content":"-Augmented"}

event: token
data: {"type":"token","content":" Generation"}

event: done
data: {"type":"done","content":"...full response..."}
```

Event types:
- `token` -- individual token/word from the LLM
- `done` -- complete response with full content
- `error` -- error occurred during processing

### Chat Sessions

Sessions maintain conversation history across multiple messages:

```bash
# List all sessions for a pipeline
curl http://localhost:5050/api/pipelines/my-chatbot/chat/sessions

# Get history for a specific session
curl http://localhost:5050/api/pipelines/my-chatbot/chat/sessions/session-001/history

# Delete a session
curl -X DELETE http://localhost:5050/api/pipelines/my-chatbot/chat/sessions/session-001
```

### ChatDrawer in the Pipeline Editor

When editing a pipeline that contains a `ChatEndpoint` node:

1. A **floating action button** (chat icon) appears in the bottom-right corner
2. Click to open the **ChatDrawer** panel
3. Messages are sent through the running pipeline
4. Responses stream back in real-time
5. Conversation history is maintained per session

### Dedicated Chat Page

For a full-page conversational experience, navigate to:

```
http://localhost:5050/pipelines/{pipelineId}/chat
```

This page provides:
- Full-width chat interface
- Session management (create, switch, delete)
- Conversation history with scroll-back
- Streaming response display
- Message timestamps and metadata

---

[Back to Unit 17](../README.md)

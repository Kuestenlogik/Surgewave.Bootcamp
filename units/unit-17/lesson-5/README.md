# Lesson 17.5: Building an Agent Pipeline

Agent pipelines use LLMs with tool access via MCP (Model Context Protocol) servers. The `agent-with-tools` template provides a 3-node starting point.

### Start from Template

1. **Pipelines** > **+ New Pipeline** > **From Template**
2. Select **Agent with Tools** from the AI/ML category
3. The editor loads:

```
ChatEndpoint -> Agent -> ChatResponse
```

### Configure the AgentNode

Click the **Agent** node to configure:

| Setting | Value | Notes |
|---------|-------|-------|
| `provider` | `openai` | LLM provider |
| `model` | `gpt-4o` | Model for reasoning |
| `api.key` | `sk-...` | Your API key |
| `max.iterations` | `10` | Maximum tool-calling iterations |
| `mcp.servers` | `http://localhost:3000` | Comma-separated MCP server URLs |
| `system.prompt` | `You are a helpful AI assistant with access to tools.` | Agent instructions |
| `tools.enabled` | `true` | Enable tool calling |

### Add Tool Access via MCP

MCP servers provide tools that the agent can invoke. Examples:

- **Filesystem MCP server**: Read/write files
- **Database MCP server**: Query databases
- **Web search MCP server**: Search the internet
- **Custom MCP server**: Any tools you build

Configure `mcp.servers` with one or more server URLs. The agent will discover available tools automatically.

### Configure ChatResponse for Tool Calls

| Setting | Value | Notes |
|---------|-------|-------|
| `streaming` | `true` | Stream responses |
| `include.tool.calls` | `true` | Show tool invocations in the response |

### Test Interactive Agent Conversations

1. Start the pipeline
2. Open the Chat Drawer or navigate to `/pipelines/{id}/chat`
3. Ask the agent to perform a task: "List the files in the current directory"
4. Watch the agent reason, call tools, and produce a response

---

[Back to Unit 17](../README.md)

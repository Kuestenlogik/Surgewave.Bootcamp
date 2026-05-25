# Lesson 23.3: MCP Tools Implementation

## Overview

Learn how to implement comprehensive MCP tools for Surgewave operations. Build tools for producing messages, consuming messages, managing topics, and querying cluster metrics.

## Learning Objectives

- Implement a produce tool with full message options
- Create a consume tool with subscription management
- Build topic management tools (create, delete, list, describe)
- Add metrics and monitoring tools
- Handle errors gracefully in tool handlers

## Surgewave MCP Tool Categories

| Category | Tool | Description |
|----------|------|-------------|
| **Producer** | `surgewave_produce` | Send a message to a topic |
| | `surgewave_produce_batch` | Send multiple messages atomically |
| **Consumer** | `surgewave_consume` | Read messages from a topic |
| | `surgewave_seek` | Seek to a specific offset |
| | `surgewave_commit` | Commit consumer offsets |
| **Topic Mgmt** | `surgewave_create_topic` | Create a new topic |
| | `surgewave_delete_topic` | Delete a topic |
| | `surgewave_list_topics` | List all topics |
| | `surgewave_describe_topic` | Get topic configuration and partitions |
| **Metrics** | `surgewave_cluster_info` | Get cluster metadata |
| | `surgewave_consumer_lag` | Check consumer group lag |
| | `surgewave_topic_offsets` | Get topic partition offsets |

## Tool Implementation Pattern

Every tool handler follows three steps:

1. **Validate input** — check required fields and types; return a JSON-RPC error if invalid
2. **Execute Surgewave operation** — call the broker inside a try/catch; map `SurgewaveException` to an error response
3. **Format response** — return `{ content: [{ type: "text", text: "..." }] }`

## Tool Schemas

### surgewave_produce

```json
{
    "type": "object",
    "properties": {
        "topic": {
            "type": "string",
            "description": "Target topic name"
        },
        "key": {
            "type": "string",
            "description": "Message key (optional)"
        },
        "value": {
            "type": "string",
            "description": "Message value"
        },
        "headers": {
            "type": "object",
            "description": "Optional message headers"
        }
    },
    "required": ["topic", "value"]
}
```

### surgewave_consume

```json
{
    "type": "object",
    "properties": {
        "topic": {
            "type": "string",
            "description": "Topic to consume from"
        },
        "count": {
            "type": "integer",
            "description": "Number of messages to consume",
            "default": 1
        },
        "timeout_ms": {
            "type": "integer",
            "description": "Timeout in milliseconds",
            "default": 5000
        },
        "from_beginning": {
            "type": "boolean",
            "description": "Start from the beginning of the topic",
            "default": false
        }
    },
    "required": ["topic"]
}
```

### surgewave_list_topics

```json
{
    "type": "object",
    "properties": {
        "pattern": {
            "type": "string",
            "description": "Optional regex pattern to filter topics"
        }
    },
    "required": []
}
```

## Error Handling

| Error Type | JSON-RPC Code | When to Use |
|------------|---------------|-------------|
| Invalid params | -32602 | Missing required field, invalid type |
| Tool error | -32603 | Surgewave operation failed |
| Timeout | -32603 | Operation exceeded timeout |
| Not found | -32603 | Topic or consumer group doesn't exist |

## Exercise

In this exercise, you will:

1. Implement a surgewave_produce tool
2. Implement a surgewave_consume tool
3. Implement topic management tools
4. Add input validation and error handling
5. Test all tools end-to-end

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)

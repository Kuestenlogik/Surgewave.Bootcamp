# Lesson 23.5: Building Custom AI Agents

## Overview

Learn how to build autonomous AI agents that use Surgewave for event-driven workflows. Create agents that maintain conversation state, respond to events, and coordinate multi-step tasks using Surgewave topics.

## Learning Objectives

- Design autonomous agent architectures with Surgewave
- Implement conversation state persistence via Surgewave topics
- Build event-driven agent workflows
- Create agents that monitor and respond to Surgewave events
- Implement agent coordination patterns

## AI Agent Architecture

```mermaid
graph TD
    subgraph Agent["AI Agent"]
        Core["Agent Core\n(LLM Integration, Tool Execution, Decision Making)"]
        State["State Manager\n(agent-state, agent-tasks, agent-results topics)"]
        Events["Event Handlers\n(consume business topics, trigger actions, produce responses)"]
        Core --> State --> Events
    end
    Events --> Cluster["Surgewave Cluster\n[agent-state] [agent-tasks] [agent-results] [orders] [alerts]"]
```

## Agent Patterns

### Pattern 1: Conversation State via Surgewave

```mermaid
graph LR
    UserMsg["User Message"] --> Agent["Agent"]
    Agent <-->|save/load history\nKey: session-123\nValue: {history}| StateTopic["Agent State Topic"]
    Agent -->|with context from history| Response["Response to User"]
```

### Pattern 2: Event-Driven Agent

```mermaid
graph TD
    Evt["Business Event\n(e.g., Order Created)"] --> Consumer["Agent Consumer\n(listening to 'orders' topic)"]
    Consumer --> Logic["Agent Logic\n(analyze order, decide on action)"]
    Logic --> Tools["Call tools\n(check inventory, validate customer)"]
    Logic --> Producer["Agent Producer\n(publish to 'order-decisions' topic)"]
```

### Pattern 3: Multi-Agent Coordination

```mermaid
graph LR
    A["Agent A\n(Order Processor)"] -->|publish| ReqTopic["Surgewave Topic\n(inventory-requests)"]
    ReqTopic -->|consume| B["Agent B\n(Inventory Manager)"]
    B -->|publish| RespTopic["Surgewave Topic\n(inventory-responses)"]
    RespTopic -->|consume| A
```

## Agent State Management

| State Type | Surgewave Topic | Key Strategy | Value |
|------------|-------------|--------------|-------|
| Conversation | agent-conversations | session-id | Message history JSON |
| Tasks | agent-tasks | task-id | Task definition and status |
| Results | agent-results | task-id | Execution results |
| Memory | agent-memory | entity-id | Long-term knowledge |

## Exercise

In this exercise, you will:

1. Build an autonomous order processing agent
2. Implement conversation state via Surgewave topics
3. Create event handlers for incoming orders
4. Add decision-making logic
5. Coordinate with other services via Surgewave

---

**Instructions:** [INSTRUCTIONS.md](INSTRUCTIONS.md)

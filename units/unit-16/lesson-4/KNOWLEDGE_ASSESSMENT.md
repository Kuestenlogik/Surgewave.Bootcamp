# Knowledge Assessment: Lesson 25.4 - DAG Patterns

## Multiple Choice

### 1. Which DAG pattern distributes a single input to multiple parallel processing paths?
- [ ] A) Fan-In
- [ ] B) Fan-Out
- [ ] C) Diamond
- [ ] D) Conditional Branch

<details>
<summary>Answer</summary>
**B)** Fan-Out distributes a single input message to multiple downstream processors that operate in parallel. Each downstream path receives a copy of the original message and processes it independently.
</details>

### 2. What is the Diamond pattern?
- [ ] A) A pattern that routes messages by content
- [ ] B) A pattern combining Fan-Out followed by Fan-In
- [ ] C) A pattern for sequential processing
- [ ] D) A pattern that duplicates messages

<details>
<summary>Answer</summary>
**B)** The Diamond pattern combines Fan-Out (splitting to parallel paths) and Fan-In (merging results back together). It creates a diamond shape in the DAG where data splits, gets processed in parallel, and then converges at a merge point.
</details>

### 3. When should you use Conditional Branching over Fan-Out?
- [ ] A) When you want all paths to process every message
- [ ] B) When messages should be routed to exactly one path based on their content
- [ ] C) When you need to merge results
- [ ] D) When processing order doesn't matter

<details>
<summary>Answer</summary>
**B)** Conditional Branching routes each message to exactly one downstream path based on a routing condition (e.g., priority level, message type). Fan-Out sends every message to all downstream paths. Use branching when different messages require different processing.
</details>

### 4. What challenge does the Diamond pattern introduce?
- [ ] A) Messages cannot be split
- [ ] B) The merge node must handle messages arriving at different times from parallel paths
- [ ] C) Only two parallel paths are supported
- [ ] D) Compression is not possible

<details>
<summary>Answer</summary>
**B)** In the Diamond pattern, the merge node receives results from parallel paths that may complete at different times. The merger must handle timing differences, potentially correlating results from the same original message, and dealing with cases where one path fails while another succeeds.
</details>

### 5. Which pattern is best for aggregating data from multiple geographic regions?
- [ ] A) Fan-Out
- [ ] B) Diamond
- [ ] C) Fan-In
- [ ] D) Conditional Branch

<details>
<summary>Answer</summary>
**C)** Fan-In is ideal for aggregating data from multiple sources (like different geographic regions) into a single unified stream. Each region produces data independently, and the merge node combines them into a single global view.
</details>

## True/False

### 6. In a Fan-Out pattern, each downstream processor receives a different subset of messages.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - In Fan-Out, each downstream processor receives ALL messages (a copy of every input). This is different from partitioning, where messages are distributed. If you want different messages to go to different processors, use Conditional Branching.
</details>

### 7. A DAG must not contain cycles to prevent infinite processing loops.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - DAG stands for Directed Acyclic Graph. The "Acyclic" property means there are no cycles, preventing infinite loops where messages would circulate endlessly through the pipeline. If feedback loops are needed, they should be implemented through separate pipelines.
</details>

### 8. Parallel processing in a DAG guarantees that downstream paths complete in the same order as input messages.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Parallel processing paths operate independently and may complete in any order. If ordering is important, you need explicit synchronization at the merge point, or you should process sequentially instead of in parallel.
</details>

## Code Challenge

### 9. What's wrong with this DAG definition?

```csharp
var pattern = new DagPattern(
    Name: "Feedback Loop",
    Type: DagPatternType.Diamond,
    Nodes: new List<DagNode>
    {
        new SourceNode("source", "Input", "input"),
        new TransformNode("process", "Processor", msg => msg),
        new TransformNode("validate", "Validator", msg => msg),
        new SinkNode("sink", "Output", "output")
    },
    Edges: new List<DagEdge>
    {
        new("source", "process"),
        new("process", "validate"),
        new("validate", "process"),  // Cycle!
        new("validate", "sink")
    }
);
```

<details>
<summary>Answer</summary>
The edge from "validate" back to "process" creates a **cycle**, violating the DAG (Directed Acyclic Graph) constraint. This would cause infinite processing loops.

Corrected version:
```csharp
var pattern = new DagPattern(
    Name: "Validation Pipeline",
    Type: DagPatternType.Diamond,
    Nodes: new List<DagNode>
    {
        new SourceNode("source", "Input", "input"),
        new TransformNode("process", "Processor", msg => msg),
        new TransformNode("validate", "Validator", msg => msg),
        new SinkNode("sink", "Output", "output"),
        new SinkNode("error-sink", "Errors", "errors")
    },
    Edges: new List<DagEdge>
    {
        new("source", "process"),
        new("process", "validate"),
        new("validate", "sink"),       // Valid messages
        new("validate", "error-sink")  // Invalid messages go to error topic
    }
);
```

If retry logic is needed, messages should be sent to a separate retry topic and consumed by a new pipeline instance, not looped back in the same DAG.
</details>

### 10. Design a DAG pattern for the following scenario:

Orders arrive from a single source. They need to be:
1. Validated (filter out invalid orders)
2. Split into domestic and international
3. Domestic orders go to a fast-track processor
4. International orders go through customs checking then fraud detection (sequentially)
5. Both paths merge and write to a completed-orders topic

<details>
<summary>Answer</summary>

```csharp
var pattern = new DagPattern(
    Name: "Order Processing Pipeline",
    Type: DagPatternType.ConditionalBranch,
    Nodes: new List<DagNode>
    {
        new SourceNode("source", "All Orders", "orders"),
        new TransformNode("validate", "Validator",
            msg => msg.Contains("\"valid\":true") ? msg : null),
        new BranchNode("router", "Domestic/International",
            msg => msg.Contains("\"country\":\"US\"") ? "domestic" : "international"),
        new TransformNode("fast-track", "Fast Track", msg => $"[fast] {msg}"),
        new TransformNode("customs", "Customs Check", msg => $"[customs] {msg}"),
        new TransformNode("fraud", "Fraud Detection", msg => $"[fraud-checked] {msg}"),
        new MergeNode("merge", "Combine All"),
        new SinkNode("sink", "Completed Orders", "completed-orders")
    },
    Edges: new List<DagEdge>
    {
        new("source", "validate"),
        new("validate", "router"),
        new("router", "fast-track", "domestic"),
        new("router", "customs", "international"),
        new("customs", "fraud"),            // Sequential: customs then fraud
        new("fast-track", "merge"),
        new("fraud", "merge"),
        new("merge", "sink")
    }
);
```

Key design decisions:
- Validation is a filter (returns null for invalid)
- Branch node routes based on country
- International path is sequential (customs -> fraud)
- Both paths merge before the final sink
- This combines Conditional Branch + Diamond patterns
</details>

## Score
- 10/10: Excellent! Ready for pipeline monitoring
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson

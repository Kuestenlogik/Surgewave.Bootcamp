# Lesson 17.9: Debugging Pipelines

### Pipeline Status Monitoring

The pipeline list view shows real-time status for all pipelines:

| # | Name | Status | Nodes | Msgs/s |
|---|------|--------|-------|--------|
| 1 | My ETL Pipeline | Running | 3 | 1,250 |
| 2 | RAG Chatbot | Running | 7 | 42 |
| 3 | Content Moderator | Stopped | 5 | 0 |
| 4 | Failed Import | Failed | 2 | 0 |

Click a pipeline to see per-node metrics: messages in, messages out, errors, latency.

### Live Data Preview

Inspect data flowing through a running pipeline without stopping it:

1. Click on any **connection line** between two nodes
2. The **Data Preview** panel opens at the bottom
3. See sample messages in real-time (key, value, headers)
4. Messages are sampled (not captured) to avoid performance impact

### Flow Debugger

Set breakpoints on nodes to pause and inspect data at any point:

1. Right-click a node > **Set Breakpoint**
2. A red dot appears on the node
3. When a record reaches the breakpoint, execution pauses
4. Inspect the record's key, value, headers, and metadata
5. Click **Continue** to process the current record and pause on the next
6. Click **Step** to advance one record at a time
7. Click **Remove Breakpoint** to resume normal flow

### Step-Through Execution

For detailed debugging of multi-node pipelines:

1. Set breakpoints on multiple nodes
2. Start the pipeline
3. Click **Step Through** to advance one node at a time
4. Watch how the record transforms as it passes through each node
5. Compare input vs. output at each step

### Node Error Inspection

When a node encounters an error:

1. The node displays a red error indicator
2. Click the node to see the error details
3. The **Error** tab shows:
   - Error message and stack trace
   - The record that caused the error
   - Timestamp and iteration count
4. If the connection type is `Error`, failed records route to the configured error handler

### Pipeline Execution History

View historical pipeline runs and their outcomes:

1. Open a pipeline
2. Click **History** tab
3. See all start/stop events with timestamps
4. View error logs for failed runs
5. Compare performance metrics across runs

---

[Back to Unit 17](../README.md)

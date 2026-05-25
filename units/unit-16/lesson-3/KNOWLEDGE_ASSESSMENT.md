# Knowledge Assessment: Lesson 25.3 - Connector Nodes and Configuration

## Multiple Choice

### 1. Which source connector configuration option determines where to start reading messages?
- [ ] A) format
- [ ] B) startOffset
- [ ] C) consumerGroup
- [ ] D) pollInterval

<details>
<summary>Answer</summary>
**B)** startOffset determines whether to read from "earliest", "latest", or a specific "timestamp". This controls where message consumption begins.
</details>

### 2. What is the purpose of the dropOnError option in a Filter transform?
- [ ] A) To delete the original message from the source
- [ ] B) To control behavior when the filter expression fails to evaluate
- [ ] C) To drop messages that pass the filter
- [ ] D) To stop the pipeline on errors

<details>
<summary>Answer</summary>
**B)** dropOnError controls what happens when the filter expression cannot be evaluated (e.g., missing field). If true, the record is dropped; if false, an error is raised.
</details>

### 3. Which sink configuration is best for ensuring messages with the same key go to the same partition?
- [ ] A) partitioner: "roundRobin"
- [ ] B) partitioner: "byKey"
- [ ] C) partitioner: "random"
- [ ] D) partitioner: "broadcast"

<details>
<summary>Answer</summary>
**B)** partitioner: "byKey" ensures that messages with the same key are sent to the same partition, maintaining ordering for related messages.
</details>

### 4. What is the purpose of connector templates?
- [ ] A) To generate documentation
- [ ] B) To provide pre-configured, reusable connector settings
- [ ] C) To compress pipeline definitions
- [ ] D) To automatically scale pipelines

<details>
<summary>Answer</summary>
**B)** Connector templates provide pre-configured settings that can be reused across pipelines, reducing boilerplate and ensuring consistency.
</details>

### 5. Which transform type would you use to split a single order into multiple item records?
- [ ] A) Filter
- [ ] B) Map
- [ ] C) FlatMap
- [ ] D) Aggregate

<details>
<summary>Answer</summary>
**C)** FlatMap performs one-to-many transformations, perfect for splitting a single record (like an order) into multiple records (like individual items).
</details>

## True/False

### 6. A Topic Source must specify a consumer group to read messages.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Consumer group is optional. If not specified, a default group can be used. However, specifying a consumer group is important for tracking offsets and load balancing across multiple consumers.
</details>

### 7. Configuration validation should be performed before the pipeline is deployed.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Validating configurations before deployment catches errors early, preventing runtime failures and making debugging easier.
</details>

### 8. An HTTP Sink should always retry failed requests indefinitely.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Indefinite retries can cause message queues to grow unbounded and hide persistent errors. A reasonable maxRetries limit with exponential backoff is preferred, along with dead-letter handling for failed messages.
</details>

## Code Challenge

### 9. What's wrong with this connector configuration?

```csharp
var sourceConfig = new TopicSourceConfig(
    Topic: "",  // Empty topic
    Format: "xml",  // Unsupported format
    StartOffset: "middle"  // Invalid offset
);
```

<details>
<summary>Answer</summary>
There are three validation errors:

1. **Empty Topic**: Topic is required and cannot be empty
2. **Invalid Format**: "xml" is not a supported format (valid: json, avro, protobuf, raw)
3. **Invalid StartOffset**: "middle" is not valid (valid: earliest, latest, timestamp)

Corrected configuration:
```csharp
var sourceConfig = new TopicSourceConfig(
    Topic: "my-events",
    Format: "json",
    StartOffset: "earliest"
);
```
</details>

### 10. Write a validation function for AggregateConfig.

<details>
<summary>Answer</summary>

```csharp
public static List<string> ValidateAggregateConfig(AggregateConfig config)
{
    var errors = new List<string>();

    // GroupBy is required
    if (string.IsNullOrWhiteSpace(config.GroupBy))
        errors.Add("GroupBy expression is required");

    // Aggregation is required
    if (string.IsNullOrWhiteSpace(config.Aggregation))
        errors.Add("Aggregation expression is required");

    // Validate aggregation function
    var validAggregations = new[] { "sum", "count", "avg", "min", "max" };
    var hasValidAggregation = validAggregations.Any(agg =>
        config.Aggregation.ToLower().Contains(agg + "("));
    if (!hasValidAggregation)
        errors.Add($"Aggregation must use a valid function: {string.Join(", ", validAggregations)}");

    // Validate window type
    var validWindowTypes = new[] { "tumbling", "sliding", "session" };
    if (!validWindowTypes.Contains(config.WindowType.ToLower()))
        errors.Add($"Invalid window type: {config.WindowType}. Valid: {string.Join(", ", validWindowTypes)}");

    // Window size must be positive
    if (config.WindowSize <= TimeSpan.Zero)
        errors.Add("WindowSize must be greater than zero");

    return errors;
}

// Usage:
var config = new AggregateConfig(
    GroupBy: "order.customerId",
    Aggregation: "sum(order.total)",
    WindowType: "tumbling",
    WindowSize: TimeSpan.FromMinutes(5)
);

var errors = ValidateAggregateConfig(config);
Console.WriteLine(errors.Count == 0 ? "Valid" : string.Join("\n", errors));
```
</details>

## Score
- 10/10: Excellent!
- 8-9/10: Good understanding
- 6-7/10: Review the lesson
- <6/10: Re-do the lesson

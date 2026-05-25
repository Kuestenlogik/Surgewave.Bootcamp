# Knowledge Assessment: Lesson 8.4 - Change Data Capture

## Multiple Choice

### 1. What does CDC stand for in the context of data integration?
- [ ] A) Continuous Data Connection
- [ ] B) Change Data Capture
- [ ] C) Centralized Data Collection
- [ ] D) Column Data Conversion

<details>
<summary>Answer</summary>
**B)** CDC stands for Change Data Capture, a pattern that captures row-level changes from a database and streams them as events.
</details>

### 2. In a CDC event, what does the "Before" field contain for an INSERT operation?
- [ ] A) The new row data
- [ ] B) The old row data
- [ ] C) null
- [ ] D) An empty dictionary

<details>
<summary>Answer</summary>
**C)** For an INSERT operation, the "Before" field is null because there was no previous state - it's a brand new row. The "After" field contains the new row data.
</details>

### 3. What is a key advantage of CDC over polling-based synchronization?
- [ ] A) CDC uses less CPU
- [ ] B) CDC can capture DELETE operations that polling would miss
- [ ] C) CDC doesn't require database access
- [ ] D) CDC works without network connectivity

<details>
<summary>Answer</summary>
**B)** Polling-based approaches query for current data, making it difficult to detect deleted rows (the row simply disappears). CDC captures all operations including DELETEs because it reads from the database transaction log.
</details>

## True/False

### 4. CDC events contain both the previous state (Before) and current state (After) for UPDATE operations.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - UPDATE events include Before (the previous row values) and After (the new row values), enabling consumers to understand exactly what changed.
</details>

### 5. CDC captures changes at a lower latency than polling because it reads from the database transaction log.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - CDC is log-based and captures changes in real-time as they are committed to the database, while polling only detects changes at configured intervals.
</details>

## Code Challenge

### 6. What CDC operation does this event represent, and what happened?

```csharp
var cdcEvent = new CdcEvent
{
    Operation = "UPDATE",
    Table = "products",
    Before = new Dictionary<string, object> { ["id"] = 1, ["price"] = 99.99, ["stock"] = 50 },
    After = new Dictionary<string, object> { ["id"] = 1, ["price"] = 79.99, ["stock"] = 50 },
    Timestamp = 1705000000,
    Position = 12345
};
```

<details>
<summary>Answer</summary>
This is an **UPDATE** operation on the products table. The Before and After dictionaries show that:
- Product ID 1 had its **price reduced from $99.99 to $79.99**
- The stock quantity (50) remained unchanged

The Position field (12345) represents the log position for tracking and resumability.
</details>

### 7. Fix this CDC consumer that fails to properly materialize a view from events:

```csharp
var materializedView = new Dictionary<int, Product>();

await foreach (var cdcEvent in cdcStream)
{
    if (cdcEvent.Operation == "INSERT")
    {
        var product = MapToProduct(cdcEvent.After!);
        materializedView[product.Id] = product;
    }
    else if (cdcEvent.Operation == "UPDATE")
    {
        var product = MapToProduct(cdcEvent.After!);
        materializedView[product.Id] = product;
    }
    // Missing something here!
}
```

<details>
<summary>Answer</summary>
The code is missing handling for **DELETE** operations. Without this, deleted records remain in the materialized view forever.

Fix:
```csharp
var materializedView = new Dictionary<int, Product>();

await foreach (var cdcEvent in cdcStream)
{
    if (cdcEvent.Operation == "INSERT")
    {
        var product = MapToProduct(cdcEvent.After!);
        materializedView[product.Id] = product;
    }
    else if (cdcEvent.Operation == "UPDATE")
    {
        var product = MapToProduct(cdcEvent.After!);
        materializedView[product.Id] = product;
    }
    else if (cdcEvent.Operation == "DELETE")
    {
        var product = MapToProduct(cdcEvent.Before!);  // Use Before for DELETE
        materializedView.Remove(product.Id);
    }
}
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson

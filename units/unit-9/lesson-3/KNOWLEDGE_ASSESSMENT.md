# Knowledge Assessment: Lesson 8.3 - Database Connectors

## Multiple Choice

### 1. What is the difference between Bulk Load and Incremental Load patterns?
- [ ] A) Bulk loads are faster, incremental loads are slower
- [ ] B) Bulk loads export full tables once, incremental loads track changes over time
- [ ] C) Bulk loads use SQL, incremental loads use NoSQL
- [ ] D) Bulk loads are for small tables, incremental loads are for large tables

<details>
<summary>Answer</summary>
**B)** Bulk Load performs a one-time full table export (ideal for initial data migration), while Incremental Load tracks new or changed rows over time for ongoing synchronization.
</details>

### 2. Which column type is typically used for ID-based incremental loading?
- [ ] A) VARCHAR primary key
- [ ] B) Auto-increment integer column
- [ ] C) Foreign key column
- [ ] D) Boolean flag column

<details>
<summary>Answer</summary>
**B)** An auto-increment integer column is ideal for ID-based incremental loading because it guarantees sequential values, making it easy to track which rows have been processed.
</details>

### 3. What write operation combines INSERT and UPDATE semantics?
- [ ] A) MERGE
- [ ] B) REPLACE
- [ ] C) UPSERT
- [ ] D) OVERWRITE

<details>
<summary>Answer</summary>
**C)** UPSERT (Update or Insert) attempts to update existing rows and inserts new ones if no matching row exists. This enables idempotent write operations.
</details>

## True/False

### 4. Database sink connectors should use batching to improve write performance.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Batching multiple records into a single database operation significantly reduces network round-trips and improves throughput compared to inserting records one at a time.
</details>

### 5. Time-based incremental loading queries rows by their auto-increment ID.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Time-based incremental loading queries rows by a timestamp column (e.g., created_at, updated_at). ID-based loading uses an auto-increment ID column.
</details>

## Code Challenge

### 6. What's the issue with this incremental load approach?

```csharp
var lastProcessedId = 0;
while (true)
{
    var rows = Query($"SELECT * FROM orders WHERE id > {lastProcessedId}");
    foreach (var row in rows)
    {
        await producer.ProduceAsync("orders", row);
        // Note: lastProcessedId not updated here
    }
    await Task.Delay(5000);
}
```

<details>
<summary>Answer</summary>
The issue is that **lastProcessedId is never updated** inside the loop. This means every iteration will re-query and re-produce all the same rows, causing duplicates and wasting resources.

Fix:
```csharp
var lastProcessedId = 0;
while (true)
{
    var rows = Query($"SELECT * FROM orders WHERE id > {lastProcessedId} ORDER BY id");
    foreach (var row in rows)
    {
        await producer.ProduceAsync("orders", row);
        lastProcessedId = row.Id;  // Update the offset!
    }
    await Task.Delay(5000);
}
```
</details>

### 7. Why might this sink connector configuration cause problems in production?

```csharp
dbSink.Configure(new Dictionary<string, string>
{
    ["connection.string"] = "Server=localhost;Database=mydb",
    ["table"] = "events",
    ["write.mode"] = "insert"
});
```

<details>
<summary>Answer</summary>
Using "insert" write mode will fail if the same record is processed twice (e.g., during retry after a transient failure), causing duplicate key violations. Additionally, it doesn't handle updates to existing records.

For production, use "upsert" mode for idempotent operations:
```csharp
dbSink.Configure(new Dictionary<string, string>
{
    ["connection.string"] = "Server=localhost;Database=mydb",
    ["table"] = "events",
    ["write.mode"] = "upsert",  // Insert or update
    ["key.column"] = "event_id"  // Column to match for upsert
});
```
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson

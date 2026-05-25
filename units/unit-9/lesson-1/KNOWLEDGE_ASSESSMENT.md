# Knowledge Assessment: Lesson 8.1 - Connect Concepts

## Multiple Choice

### 1. What is the primary function of a Source Connector?
- [ ] A) Read from Surgewave topics and write to external systems
- [ ] B) Read from external systems and write to Surgewave topics
- [ ] C) Transform data within Surgewave topics
- [ ] D) Monitor Surgewave cluster health

<details>
<summary>Answer</summary>
**B)** Source connectors read data from external systems (like files, databases, APIs) and produce records to Surgewave topics. They are the entry point for external data into Surgewave.
</details>

### 2. Which interface method is responsible for initializing a source connector with settings?
- [ ] A) StartAsync()
- [ ] B) StopAsync()
- [ ] C) Configure()
- [ ] D) ProcessAsync()

<details>
<summary>Answer</summary>
**C)** The Configure() method accepts a Dictionary<string, string> of configuration settings that define how the connector should behave (e.g., connection strings, topic names, credentials).
</details>

### 3. What are the three main components of a connector architecture?
- [ ] A) Producer, Consumer, Topic
- [ ] B) Config, Tasks, Transforms
- [ ] C) Source, Sink, Router
- [ ] D) Reader, Writer, Buffer

<details>
<summary>Answer</summary>
**B)** A connector consists of Config (settings), Tasks (workers that do the actual data movement), and optionally Transforms (data modifications).
</details>

## True/False

### 4. Sink connectors produce data to Surgewave topics.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Sink connectors consume data FROM Surgewave topics and write to external systems. Source connectors produce TO Surgewave topics.
</details>

### 5. Connectors help decouple data integration logic from business logic.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Connectors provide reusable, configurable components that handle the complexities of data movement, allowing business logic to focus on processing the data rather than acquiring or exporting it.
</details>

## Code Challenge

### 6. What's wrong with this connector interface implementation?

```csharp
public interface ISourceConnector
{
    void Configure(string connectionString);
    void Start();
    void Stop();
}
```

<details>
<summary>Answer</summary>
The issue is that Configure() only accepts a single string parameter instead of a Dictionary<string, string>. This limits flexibility since connectors typically need multiple configuration options (topic, credentials, format, etc.). Additionally, Start() and Stop() should be async (StartAsync/StopAsync) to handle I/O operations properly without blocking.

Fix:
```csharp
public interface ISourceConnector
{
    void Configure(Dictionary<string, string> config);
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
}
```
</details>

### 7. Given this configuration, what type of connector is being set up?

```csharp
connector.Configure(new Dictionary<string, string>
{
    ["table"] = "customers",
    ["topic"] = "customer-events",
    ["poll.interval.ms"] = "5000"
});
```

<details>
<summary>Answer</summary>
This is a **Source Connector** configuration. The presence of "table" suggests reading from a database table, and "topic" indicates where the data will be written (Surgewave topic). The "poll.interval.ms" suggests periodic polling of the source. A sink connector would read FROM a topic and write TO an external system.
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson

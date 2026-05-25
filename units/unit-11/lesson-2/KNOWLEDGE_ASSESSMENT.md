# Knowledge Assessment: Lesson 5.2 - API Wrapper Migration

## Multiple Choice

### 1. What is the API wrapper approach for Kafka migration?
- [ ] A) Running Kafka alongside Surgewave
- [ ] B) Replacing the Confluent.Kafka NuGet package with Kuestenlogik.Surgewave.Kafka.Compat
- [ ] C) Rewriting all code to use native Surgewave APIs
- [ ] D) Using a protocol translator

<details>
<summary>Answer</summary>
**B)** The API wrapper approach involves replacing the NuGet package reference from Confluent.Kafka to Kuestenlogik.Surgewave.Kafka.Compat. The code stays the same - only the package reference changes.
</details>

### 2. What is the key benefit of the API wrapper approach?
- [ ] A) Better performance than native Surgewave
- [ ] B) Zero code changes required
- [ ] C) Automatic data migration
- [ ] D) Free monitoring

<details>
<summary>Answer</summary>
**B)** Zero code changes is the key benefit. Your existing code continues to use the same Confluent.Kafka APIs, but the implementation is provided by Surgewave's compatible wrapper.
</details>

### 3. After migrating with the API wrapper, what namespace does the code use?
- [ ] A) Kuestenlogik.Surgewave.Kafka
- [ ] B) Surgewave.Client
- [ ] C) Confluent.Kafka
- [ ] D) Apache.Kafka

<details>
<summary>Answer</summary>
**C)** The code continues to use `using Confluent.Kafka;` - this namespace is provided by Surgewave.Kafka.Compat. This is what enables zero code changes.
</details>

## True/False

### 4. The API wrapper approach requires rewriting producer and consumer code.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - The API wrapper approach specifically avoids code changes. Only the NuGet package reference changes from Confluent.Kafka to Kuestenlogik.Surgewave.Kafka.Compat.
</details>

### 5. With the API wrapper, you can easily switch back to Kafka if needed.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The fallback option is a key benefit. Since the API is identical, you can switch back to the original Confluent.Kafka package if needed, providing a safe migration path.
</details>

## Code Challenge

### 6. What changes are needed to migrate this code using the API wrapper approach?

```csharp
// File: MyProject.csproj
<PackageReference Include="Confluent.Kafka" Version="2.3.0" />

// File: Producer.cs
using Confluent.Kafka;

var config = new ProducerConfig { BootstrapServers = "kafka:9092" };
using var producer = new ProducerBuilder<string, string>(config).Build();
await producer.ProduceAsync("topic", new Message<string, string> { Value = "test" });
```

<details>
<summary>Answer</summary>
Only the NuGet package reference needs to change:

```xml
// File: MyProject.csproj
<PackageReference Include="Kuestenlogik.Surgewave.Kafka.Compat" Version="1.0.0" />
```

The C# code stays exactly the same:
```csharp
// File: Producer.cs
using Confluent.Kafka;  // Same namespace, provided by Surgewave.Kafka.Compat

var config = new ProducerConfig { BootstrapServers = "surgewave:9092" };  // Update server address
using var producer = new ProducerBuilder<string, string>(config).Build();
await producer.ProduceAsync("topic", new Message<string, string> { Value = "test" });
```

The only change beyond the package reference is updating the BootstrapServers to point to Surgewave (which you'd do with either migration approach).
</details>

### 7. Compare the three migration approaches and their trade-offs:

| Approach | Code Changes | Best For |
|----------|--------------|----------|
| Protocol Compatibility | ??? | ??? |
| API Wrapper | ??? | ??? |
| Native Surgewave Client | ??? | ??? |

<details>
<summary>Answer</summary>

| Approach | Code Changes | Best For |
|----------|--------------|----------|
| Protocol Compatibility | None (just change bootstrap servers) | Quick testing, minimal risk migration |
| API Wrapper | None (change NuGet package) | Zero-code migration with easy fallback |
| Native Surgewave Client | Full rewrite | Maximum performance, new projects |

Detailed comparison:

1. **Protocol Compatibility**:
   - Fastest to try
   - No build changes
   - Still depends on Kafka library
   - Good for validation phase

2. **API Wrapper**:
   - Same API, Surgewave implementation
   - Easy to switch back if needed
   - Good balance of safety and commitment

3. **Native Surgewave Client**:
   - Best performance
   - Access to Surgewave-specific features
   - Requires code investment
   - Best for new projects or performance-critical migrations
</details>

## Score
- 7/7: Excellent! You understand API wrapper migration
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson

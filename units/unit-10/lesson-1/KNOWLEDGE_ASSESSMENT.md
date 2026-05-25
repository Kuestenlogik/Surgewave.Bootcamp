# Quiz: Lesson 10.1 - Schema Basics

## Multiple Choice

### 1. What is the primary purpose of a Schema Registry?
- [ ] A) To store message data
- [ ] B) To centralize schema management and validation
- [ ] C) To compress messages
- [ ] D) To encrypt messages

<details>
<summary>Answer</summary>
**B)** A Schema Registry centralizes schema management by storing schemas, assigning unique IDs, validating messages against registered schemas, and enforcing compatibility rules.
</details>

### 2. What is a "subject" in schema registry terminology?
- [ ] A) A consumer group
- [ ] B) A topic-schema binding identifier (typically the topic name)
- [ ] C) A schema version
- [ ] D) A validation rule

<details>
<summary>Answer</summary>
**B)** A subject is the identifier used to bind schemas to topics, typically using the topic name (e.g., "orders-value" for the value schema of the "orders" topic).
</details>

### 3. What problem does a schema registry solve for implicit contracts?
- [ ] A) Network latency
- [ ] B) Storage costs
- [ ] C) Runtime errors from mismatched data structures
- [ ] D) Message ordering

<details>
<summary>Answer</summary>
**C)** Without a schema registry, producers and consumers have implicit contracts that can lead to runtime errors when data structures don't match (e.g., producer sends age as string "X", consumer expects integer).
</details>

## True/False

### 4. Each schema version registered in the schema registry gets a unique ID.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Every schema version is assigned a globally unique ID by the registry. This ID is used to efficiently reference schemas in messages without embedding the full schema.
</details>

### 5. Schema validation happens after messages are sent to Surgewave topics.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Schema validation should happen BEFORE messages are produced to catch errors early. The producer validates against the registered schema before sending, preventing invalid data from entering the system.
</details>

## Code Challenge

### 6. What happens in this scenario without a schema registry?

```csharp
// Producer sends:
await producer.ProduceAsync("users", new { name = "John", age = 30 });

// Later, producer is updated to send:
await producer.ProduceAsync("users", new { name = "Jane", age = "thirty" });

// Consumer expects:
public record User(string Name, int Age);
```

<details>
<summary>Answer</summary>
The consumer will experience a **runtime deserialization error** when processing the second message because:
1. The producer changed `age` from int (30) to string ("thirty")
2. The consumer expects `Age` as an `int`
3. JSON deserialization will fail with a type mismatch error

With a schema registry, this would be caught at produce time - the validation would reject `age = "thirty"` because the schema defines age as an integer.
</details>

### 7. What are the four main components shown in the Schema Registry architecture?

| Schema Registry |
|-----------------|
| ? |
| ? |
| ? |
| ? |

<details>
<summary>Answer</summary>
The four main components are:

1. **Schemas** - The actual schema definitions (JSON Schema, etc.)
2. **Versions** - Version history for each schema (v1, v2, v3...)
3. **Compatibility** - Rules for schema evolution (backward, forward, full)
4. **Subjects** - Topic-schema bindings that map topics to their schemas

| Schema Registry Component | Description |
|--------------------------|-------------|
| **Schemas** | Subject → Schema mappings |
| **Versions** | Schema version history |
| **Compatibility** | Evolution rules |
| **Subjects** | Topic-schema bindings |
</details>

## Score
- 7/7: Excellent!
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson

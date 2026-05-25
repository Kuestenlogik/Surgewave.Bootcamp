# Quiz: Lesson 1.2 - Producing Messages

## Multiple Choice

### 1. What does the generic type `SurgewaveProducer<TKey, TValue>` represent?
- [ ] A) TKey is the topic name, TValue is the message content
- [ ] B) TKey is the message key type, TValue is the message value type
- [ ] C) TKey is the partition number, TValue is the offset
- [ ] D) TKey is the serializer, TValue is the deserializer

<details>
<summary>Answer</summary>
**B)** TKey is the message key type, TValue is the message value type. Surgewave uses strongly-typed producers where you specify the types for both the key and value.
</details>

### 2. Which serializer does Surgewave use by default for complex types (like custom classes)?
- [ ] A) Binary serializer
- [ ] B) XML serializer
- [ ] C) JSON serializer
- [ ] D) Protobuf serializer

<details>
<summary>Answer</summary>
**C)** JSON serializer. Surgewave automatically uses JSON serialization for complex types, while primitive types like string and int have dedicated serializers.
</details>

### 3. What information does ProduceResult contain after a successful produce?
- [ ] A) Only the offset
- [ ] B) Topic, Partition, Offset, and Timestamp
- [ ] C) Topic and message ID
- [ ] D) Acknowledgment status only

<details>
<summary>Answer</summary>
**B)** Topic, Partition, Offset, and Timestamp. The ProduceResult provides complete information about where and when the message was stored.
</details>

### 4. How do you override the default serializer for values?
- [ ] A) `options.Serializer = Serializers.Json<MyClass>();`
- [ ] B) `options.ValueSerializer = Serializers.Json<MyClass>();`
- [ ] C) `producer.SetSerializer(Serializers.Json<MyClass>());`
- [ ] D) `new SurgewaveProducer<string, MyClass>(Serializers.Json<MyClass>());`

<details>
<summary>Answer</summary>
**B)** `options.ValueSerializer = Serializers.Json<MyClass>();`. The ValueSerializer property in options allows you to specify a custom serializer.
</details>

## True/False

### 5. Surgewave automatically selects the UTF-8 string serializer when you use `SurgewaveProducer<string, string>`.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Surgewave has built-in serializers for common types. For `string`, it uses the UTF-8 string serializer automatically.
</details>

### 6. The `byte[]` type requires a custom serializer because Surgewave doesn't know how to handle raw bytes.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - For `byte[]`, Surgewave uses a pass-through serializer that performs no transformation. The bytes are stored exactly as provided.
</details>

## Code Challenge

### 7. What's wrong with this producer code?

```csharp
var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";
});

await producer.ProduceAsync("orders", "order-1", "New order received");
await producer.ProduceAsync("orders", "order-2", "Another order");

// Application ends here
```

<details>
<summary>Answer</summary>
The issue is that the **producer is not being disposed**. The producer holds resources and connections that need to be cleaned up.

Fix:
```csharp
await using var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = "localhost:9092";
});

await producer.ProduceAsync("orders", "order-1", "New order received");
await producer.ProduceAsync("orders", "order-2", "Another order");

// Producer is automatically disposed when scope ends
```

Use `await using` to ensure proper cleanup of the producer's resources.
</details>

## Score
- 7/7: Excellent! You understand producing messages
- 5-6/7: Good understanding of producer basics
- 3-4/7: Review serialization and producer options
- <3/7: Re-do the lesson and practice producing messages

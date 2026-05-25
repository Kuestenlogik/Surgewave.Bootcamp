# Lesson 10.2: JSON Schema and Avro Serialization

> **Difficulty:** Intermediate | **Duration:** 45 min | **Prerequisites:** Lesson 10.1

Define and validate messages using JSON Schema, and serialize with Apache Avro.

## Objectives

- Understand JSON Schema syntax
- Define schemas for common data patterns
- Validate messages against schemas
- Handle validation errors gracefully
- Use Avro serialization with schema registry integration

## Concepts

### What is JSON Schema?

JSON Schema is a vocabulary for annotating and validating JSON documents:

```json
{
    "type": "object",
    "properties": {
        "name": { "type": "string" },
        "age": { "type": "integer", "minimum": 0 }
    },
    "required": ["name"]
}
```

### Schema Types

| Type | Description | Example |
|------|-------------|---------|
| string | Text values | `"hello"` |
| integer | Whole numbers | `42` |
| number | Any numeric value | `3.14` |
| boolean | True/false | `true` |
| array | Ordered list | `[1, 2, 3]` |
| object | Key-value pairs | `{"key": "value"}` |
| null | Null value | `null` |

### Validation Keywords

```json
{
    "type": "object",
    "properties": {
        "email": {
            "type": "string",
            "format": "email",
            "maxLength": 255
        },
        "score": {
            "type": "integer",
            "minimum": 0,
            "maximum": 100
        },
        "tags": {
            "type": "array",
            "items": { "type": "string" },
            "minItems": 1,
            "uniqueItems": true
        }
    },
    "required": ["email"],
    "additionalProperties": false
}
```

## Apache Avro Serialization

### What is Avro?

Apache Avro is a compact binary serialization format that:
- Provides rich schema support with evolution capabilities
- Uses JSON for schema definitions
- Stores schema with data or in a schema registry
- Offers excellent performance and compression

### Avro Schema Example

```json
{
    "type": "record",
    "name": "OrderEvent",
    "namespace": "com.example.events",
    "fields": [
        { "name": "orderId", "type": "string" },
        { "name": "amount", "type": "double" },
        { "name": "currency", "type": "string", "default": "USD" },
        { "name": "timestamp", "type": "long" }
    ]
}
```

### Using Avro with Surgewave

Surgewave provides built-in Avro serialization with schema registry integration:

```csharp
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.SchemaRegistry.Avro;

// Create Avro serializer config
var avroConfig = new AvroSerializerConfig(schemaRegistryClient)
{
    AutoRegisterSchemas = true,
    SubjectNameStrategy = TopicNameStrategy.Instance
};

// Create producer with Avro serialization
await using var producer = new SurgewaveProducer<string, OrderEvent>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.AsyncValueSerializer = new SchemaRegistryAvroSerializer<OrderEvent>(avroConfig);
});

// Produce Avro-encoded messages
var orderEvent = new OrderEvent
{
    OrderId = "ORD-001",
    Amount = 99.99,
    Currency = "USD",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

await producer.ProduceAsync("orders", "order-key", orderEvent);
```

### Consuming Avro Messages

```csharp
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.SchemaRegistry.Avro;

// Create Avro deserializer
var avroDeserializerConfig = new AvroSerializerConfig(schemaRegistryClient);

await using var consumer = new SurgewaveConsumer<string, OrderEvent>(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.GroupId = "order-processors";
    options.AsyncValueDeserializer = new SchemaRegistryAvroDeserializer<OrderEvent>(avroDeserializerConfig);
});

consumer.Subscribe("orders");

var result = await consumer.ConsumeAsync();
Console.WriteLine($"Order: {result?.Value.OrderId}, Amount: {result?.Value.Amount}");
```

### Wire Format

Surgewave uses the Confluent-compatible wire format for Avro messages:

| Byte(s) | Field | Value |
|---------|-------|-------|
| 1 byte | Magic byte | `0x00` |
| 4 bytes | Schema ID | Big-endian integer |
| variable | Avro Payload | Binary or JSON Avro |

## Exercise

In this lesson, you'll:
1. Define JSON schemas for various message types
2. Implement a schema validator
3. Validate messages before producing
4. Handle validation errors
5. Use Avro serialization with schema registry

## Key Takeaways

- JSON Schema provides declarative validation
- Use `required` for mandatory fields
- Use `additionalProperties: false` for strict validation
- Validate at produce time to catch errors early
- Avro provides compact binary serialization with schema evolution
- Use `SchemaRegistryAvroSerializer` for automatic schema registration
- The wire format is compatible with Confluent Schema Registry

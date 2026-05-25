# Lesson 3.1: Custom Serialization

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Unit 2 completion

## Overview

Learn how to serialize complex data types for Surgewave messages using JSON, Protobuf, and custom serializers.

## Learning Objectives

- Understand serialization in message streaming
- Implement JSON serialization with System.Text.Json
- Create custom serializers for domain objects
- Choose the right serialization format for your use case

## Why Custom Serialization?

Surgewave's default serializers handle strings and byte arrays. For complex types, you need custom serialization:

| Format | Use Case | Pros | Cons |
|--------|----------|------|------|
| **JSON** | APIs, debugging | Human-readable, flexible | Larger size, slower |
| **Protobuf** | High throughput | Compact, fast, typed | Requires schema |
| **Custom** | Legacy systems | Full control | More maintenance |

## Key Concepts

### ISerializer / IDeserializer Interfaces

Surgewave separates serialization and deserialization into two interfaces. Both accept a `topic` parameter for context:

```csharp
public interface ISerializer<in T>
{
    byte[]? Serialize(T? data, string topic);
}

public interface IDeserializer<out T>
{
    T Deserialize(ReadOnlySpan<byte> data, string topic);
}
```

### JSON Serialization

A single class can implement both interfaces:

```csharp
public class JsonSerializer<T> : ISerializer<T>, IDeserializer<T>
{
    public byte[]? Serialize(T? data, string topic) =>
        data is null ? null : System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data);

    public T Deserialize(ReadOnlySpan<byte> data, string topic) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(data)!;
}
```

## Exercise

In this exercise, you'll:

1. Create a `JsonSerializer<T>` for domain objects
2. Configure a producer with custom serialization
3. Send and receive complex objects

## Files

- `start/Program.cs` - Starter code with TODOs
- `completed/Program.cs` - Working solution

---

**Instructions:** → [INSTRUCTIONS.md](INSTRUCTIONS.md)

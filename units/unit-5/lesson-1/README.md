# Lesson 7.1: Streams Basics

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Unit 6 completion

## Learning Objectives

- Understand the Surgewave Streams programming model
- Create a StreamsBuilder and define a topology
- Work with Stream for record processing
- Write processed data to output topics

## Key Concepts

### StreamsBuilder

The entry point for building stream processing applications:

```csharp
var builder = new StreamsBuilder("my-app");
var stream = builder.Stream<string, string>("input-topic");
stream.To("output-topic");
var app = builder.Build();
app.Start();
```

### Stream

A Stream represents an unbounded stream of records:
- Each record is an independent event
- Records are processed in order per partition
- Stateless operations don't maintain any state

### Topology

The processing graph built from your stream operations:
- Source nodes read from topics
- Processor nodes transform data
- Sink nodes write to topics

## Exercise

Build a simple word count pipeline that:
1. Reads text messages from an input topic
2. Filters out empty messages
3. Converts to uppercase
4. Writes to an output topic

## Time Estimate

~30 minutes

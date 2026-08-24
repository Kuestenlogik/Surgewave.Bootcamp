# Lesson 0.2: Project Setup

> **Difficulty:** Beginner | **Duration:** 30 min | **Prerequisites:** Lesson 0.1

## Overview

Learn how to set up a new Surgewave project using NuGet packages or the Visual Studio template.

## Learning Objectives

- Install Surgewave via NuGet
- Create a project from template
- Configure your development environment
- Run your first Surgewave application

## Prerequisites

- .NET 10 SDK installed
- A code editor (VS Code, Visual Studio, Rider)

## Installation Options

### Option 1: NuGet Packages (Recommended)

Add Surgewave packages to an existing project:

```bash
# Client library (producer/consumer)
dotnet add package Kuestenlogik.Surgewave.Client

# Embedded broker (for development/testing)
dotnet add package Kuestenlogik.Surgewave.Runtime

# Broker library (for building custom brokers)
dotnet add package Kuestenlogik.Surgewave.Broker
```

### Option 2: Visual Studio Template

Install the project template:

```bash
dotnet new install Kuestenlogik.Surgewave.Templates
```

Create a new project:

```bash
dotnet new surgewave-console -n MySurgewaveApp
```

### Option 3: Standalone Broker

Download the standalone broker from GitHub Releases:

```bash
# Download (example for Windows)
curl -LO https://github.com/Kuestenlogik/Surgewave/releases/latest/download/surgewave-win-x64.zip
unzip surgewave-win-x64.zip

# Run broker
./surgewave --port 9092
```

## Package Reference Guide

| Package | Purpose | When to Use |
|---------|---------|-------------|
| `Kuestenlogik.Surgewave.Client` | Producer/Consumer APIs | All applications |
| `Kuestenlogik.Surgewave.Runtime` | Embedded broker | Dev, testing, single-process apps |
| `Kuestenlogik.Surgewave.Broker` | Broker components | Building custom brokers |
| `Kuestenlogik.Surgewave.Kafka.Compat` | Kafka API compatibility | Migrating from Kafka |

## Quick Start Project

Create a minimal Surgewave application:

### 1. Create Project

```bash
mkdir MySurgewaveApp
cd MySurgewaveApp
dotnet new console
dotnet add package Kuestenlogik.Surgewave.Client
dotnet add package Kuestenlogik.Surgewave.Runtime
```

### 2. Write Code

```csharp
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker running on port {runtime.Port}");

// Create producer
await using var producer = new SurgewaveProducer<string, string>(opt =>
    opt.BootstrapServers = $"localhost:{runtime.Port}");

// Send a message
await producer.ProduceAsync("my-topic", "key", "Hello, Surgewave!");
Console.WriteLine("Message sent!");

// Create consumer
await using var consumer = new SurgewaveConsumer<string, string>(opt =>
{
    opt.BootstrapServers = $"localhost:{runtime.Port}";
    opt.GroupId = "my-group";
    opt.AutoOffsetReset = AutoOffsetReset.Earliest;
});

consumer.Subscribe("my-topic");

// Receive the message
var result = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
Console.WriteLine($"Received: {result?.Value}");
```

### 3. Run

```bash
dotnet run
```

## Project Templates

### surgewave-console

Basic console application with embedded broker:

```bash
dotnet new surgewave-console -n MyApp
```

### surgewave-worker

Background service for message processing:

```bash
dotnet new surgewave-worker -n MyWorker
```

### surgewave-api

ASP.NET Core API with Surgewave integration:

```bash
dotnet new surgewave-api -n MyApi
```

## IDE Setup

### Visual Studio

1. Install the Surgewave extension from the Marketplace
2. Use **File > New Project > Surgewave Application**

### VS Code

1. Install the C# Dev Kit extension
2. Open terminal: `dotnet new surgewave-console -n MyApp`

### JetBrains Rider

1. Create new .NET project
2. Add NuGet packages via NuGet tool window

## Configuration

### appsettings.json

```json
{
  "Surgewave": {
    "Client": {
      "BootstrapServers": "localhost:9092",
      "GroupId": "my-service"
    },
    "Broker": {
      "Port": 9092,
      "StorageMode": "Memory",
      "AutoCreateTopics": true
    }
  }
}
```

### Environment Variables

```bash
export Surgewave__CLIENT__BOOTSTRAPSERVERS=localhost:9092
export Surgewave__CLIENT__GROUPID=my-service
```

## Troubleshooting

### "Package not found"

Ensure you have the NuGet source configured:

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json
```

### "Port already in use"

Another process is using port 9092. Either:
- Stop the other process
- Use a different port: `.WithPort(0)` for auto-assign

### "Connection refused"

The broker isn't running.
Ensure:
- Embedded broker is started before creating clients
or
- Standalone broker is running if not using embedded

## Next Steps

You're ready to start building! Continue to:

- [Unit 1: Surgewave Fundamentals](../../unit-1/README.md) - Hands-on coding
- [API Documentation](https://github.com/Kuestenlogik/Surgewave/blob/main/docs/) - Reference guide
- [Samples](https://github.com/Kuestenlogik/Surgewave.Samples) - Example applications

---

**Next:** → [Unit 1: Surgewave Fundamentals](../../unit-1/README.md)

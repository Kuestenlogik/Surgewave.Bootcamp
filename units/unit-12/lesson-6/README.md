# Lesson 12.6: Migrating from RabbitMQ

Migrating from RabbitMQ to Surgewave can be as simple as changing a hostname and a port. This lesson
covers which RabbitMQ features map directly to Surgewave, which require a workaround, and a step-by-step
guide for moving a real application.

## Step-by-Step Migration Guide

### Step 1: Start Surgewave with AMQP and QueueView enabled

```bash
dotnet run --project src/Kuestenlogik.Surgewave.Broker -- \
  --Surgewave:QueueView:Enabled=true \
  --Surgewave:QueueView:MaxDeliveryCount=5 \
  --Surgewave:Amqp:Enabled=true \
  --Surgewave:Amqp:Port=5672
```

Or with Docker:

```yaml
# docker-compose.yml
services:
  surgewave:
    image: surgewave:latest
    ports:
      - "9092:9092"   # Kafka protocol (new consumers, tooling)
      - "5672:5672"   # AMQP 0.9.1 (existing RabbitMQ clients)
      - "5050:5050"   # Control UI
    environment:
      Surgewave__QueueView__Enabled: "true"
      Surgewave__QueueView__VisibilityTimeout: "00:00:30"
      Surgewave__QueueView__MaxDeliveryCount: "5"
      Surgewave__QueueView__DlqTopicSuffix: ".dlq"
      Surgewave__Amqp__Enabled: "true"
      Surgewave__Amqp__Port: "5672"
```

### Step 2: Update the connection factory

This is typically the only code change needed for a basic migration:

**Before (RabbitMQ):**

```csharp
var factory = new ConnectionFactory
{
    HostName = "rabbitmq.internal",
    Port = 5672,
    UserName = "app-user",
    Password = "secret",
    VirtualHost = "production"
};
```

**After (Surgewave):**

```csharp
var factory = new ConnectionFactory
{
    HostName = "surgewave.internal",   // <-- changed
    Port = 5672,                   // same port
    UserName = "guest",            // Surgewave default credentials
    Password = "guest",
    VirtualHost = "/"              // Surgewave uses "/" only
};
```

### Step 3: Verify queue declarations

Surgewave accepts `QueueDeclare`, `ExchangeDeclare`, and `QueueBind` — these will succeed without error.
Check that your queue names match your Surgewave topic names:

```csharp
// This works on Surgewave without changes
await channel.QueueDeclareAsync(
    queue: "orders",
    durable: true,
    exclusive: false,
    autoDelete: false);
```

### Step 4: Test ack/nack flow

Run your consumer against Surgewave and verify the ack/nack behavior:

```csharp
consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        await ProcessMessageAsync(ea.Body.ToArray());
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch (TransientException)
    {
        // Requeue — Surgewave QueueView re-delivers after visibility timeout
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
    }
    catch (PoisonMessageException)
    {
        // Reject — routed to orders.dlq in Surgewave
        await channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
    }
};
```

### Step 5: Check the DLQ

Surgewave auto-creates `{topic}.dlq` when a message is rejected or exceeds `MaxDeliveryCount`.
Subscribe to it with a separate consumer or inspect it via kcat:

```bash
kcat -b localhost:9092 -t orders.dlq -C -o beginning
```

### Step 6: Optionally add Kafka clients for new features

Once migrated, you can add Kafka-compatible consumers alongside your AMQP consumers — no data
duplication needed:

```csharp
// Kafka-style consumer reading the same 'orders' topic
var consumer = new SurgewaveConsumer<string, string>(new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "analytics-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
});
consumer.Subscribe("orders");
```

## RabbitMQ Feature Mapping

### What Works Directly in Surgewave

| RabbitMQ Feature | Surgewave Equivalent | Notes |
|-----------------|-----------------|-------|
| `Basic.Ack` | `IQueueView.Ack` | Full support |
| `Basic.Nack(requeue=true)` | `IQueueView.Nack(requeue: true)` | Immediate redelivery |
| `Basic.Nack(requeue=false)` | `IQueueView.Nack(requeue: false)` | Drop silently |
| `Basic.Reject(requeue=false)` | `IQueueView.RejectAsync` | Routes to DLQ |
| Dead-letter exchanges (DLQ) | Auto-created `{topic}.dlq` topic | Same behavior |
| Durable queues | All Surgewave topics are durable | Always on |
| Message persistence | Always persistent in Surgewave log | Always on |
| Delivery count | `InFlightMessage.DeliveryCount` | Full support |
| `redelivered` flag | Set when `DeliveryCount > 1` | Full support |
| Multiple consumers on one queue | Multiple AMQP consumers per topic | Via consumer groups |

### What Requires a Workaround

| RabbitMQ Feature | Surgewave Behavior | Workaround |
|-----------------|---------------|------------|
| Per-message TTL | Not supported | Use `MaxDeliveryCount` + DLQ as an expiry mechanism |
| Exchange routing (fanout, topic, headers) | Name-based only | Use separate Surgewave topics per logical exchange |
| Queue TTL / auto-delete | Not supported | Use Surgewave topic retention (`cleanup.policy=delete`) |
| Publisher confirms (`Basic.Confirm`) | Not supported | Rely on Surgewave's synchronous `ProduceAsync` return |
| Multiple queues on one channel | One consumer tag per channel | Open multiple channels, one per queue |
| Transaction semantics (`Tx.*`) | Not supported | Use idempotent consumer patterns |
| Virtual hosts | Only `"/"` is supported | Use topic naming conventions to separate tenants |
| Per-consumer prefetch (`Basic.Qos`) | Soft cap via `MaxInFlightPerConsumer` | Adjust `MaxInFlightPerConsumer` in config |

## Before/After Code Comparison

### Before: RabbitMQ producer and consumer

```csharp
// Producer — RabbitMQ
var factory = new ConnectionFactory { HostName = "rabbitmq.internal" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync("orders", ExchangeType.Direct, durable: true);
await channel.QueueDeclareAsync("orders.processing", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync("orders.processing", "orders", "process");

var props = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };
await channel.BasicPublishAsync("orders", "process", props, Encoding.UTF8.GetBytes(orderJson));

// Consumer — RabbitMQ
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, ea) =>
{
    var order = JsonSerializer.Deserialize<Order>(ea.Body.Span);
    await orderService.ProcessAsync(order!);
    await channel.BasicAckAsync(ea.DeliveryTag, false);
};
await channel.BasicConsumeAsync("orders.processing", autoAck: false, consumer: consumer);
```

### After: Same code, Surgewave as the broker

```csharp
// Producer — Surgewave (AMQP) — only HostName changed
var factory = new ConnectionFactory { HostName = "surgewave.internal" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// These calls succeed — Surgewave accepts them and maps to topics
await channel.ExchangeDeclareAsync("orders", ExchangeType.Direct, durable: true);
await channel.QueueDeclareAsync("orders.processing", durable: true, exclusive: false, autoDelete: false);
await channel.QueueBindAsync("orders.processing", "orders", "process");

var props = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };
await channel.BasicPublishAsync("orders", "process", props, Encoding.UTF8.GetBytes(orderJson));

// Consumer — Surgewave (AMQP) — identical code, QueueView handles ack/nack
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, ea) =>
{
    var order = JsonSerializer.Deserialize<Order>(ea.Body.Span);
    await orderService.ProcessAsync(order!);
    await channel.BasicAckAsync(ea.DeliveryTag, false);
};
await channel.BasicConsumeAsync("orders.processing", autoAck: false, consumer: consumer);
```

The code is identical. Only the broker hostname changes.

## Key Benefits After Migration

| Before (RabbitMQ) | After (Surgewave) |
|------------------|--------------|
| Separate Kafka cluster for analytics | Same topic, Kafka consumer group reads it |
| Message gone after ack | Log retained by policy — replay anytime |
| No built-in log replay | `--from-beginning` supported out of the box |
| Separate DLQ broker config | Auto-created `{topic}.dlq` topic |
| Memory-based queue pressure | Append-only log, storage-efficient |
| No native .NET 10 support | Built on .NET 10, idiomatic C# |

## Quick Reference

```bash
# Start Surgewave with AMQP + QueueView
dotnet run --project src/Kuestenlogik.Surgewave.Broker -- \
  --Surgewave:QueueView:Enabled=true \
  --Surgewave:Amqp:Enabled=true

# Connect your RabbitMQ app — change hostname only
RABBITMQ_HOST=localhost dotnet run --project MyApp

# Check in-flight state
curl http://localhost:5050/api/queue/orders/status

# Inspect DLQ
kcat -b localhost:9092 -t orders.dlq -C -o beginning

# Purge stuck in-flight state (safe — log untouched)
curl -X POST http://localhost:5050/api/queue/orders/purge
```

---

[Back to Unit 12](../README.md)

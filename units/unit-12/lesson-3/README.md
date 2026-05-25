# Lesson 12.3: AMQP Protocol Adapter

AMQP 0.9.1 is the wire protocol that RabbitMQ speaks. Surgewave implements the same protocol so that
any existing RabbitMQ client library — in any language — can connect to Surgewave without modification.
You simply point the connection factory at Surgewave's AMQP port instead of RabbitMQ's.

## What is AMQP 0.9.1?

AMQP (Advanced Message Queuing Protocol) 0.9.1 is an open standard binary protocol for message
brokers. It defines:

- **Connections** — persistent TCP sessions between client and broker
- **Channels** — lightweight virtual connections multiplexed over a single TCP connection
- **Exchanges** — routing endpoints where producers publish messages
- **Queues** — delivery endpoints where consumers receive messages
- **Bindings** — rules that connect exchanges to queues
- **Methods** — binary-encoded RPC-style commands (`Basic.Publish`, `Basic.Consume`, `Basic.Ack`, etc.)

RabbitMQ uses AMQP 0.9.1 as its primary protocol. The Surgewave AMQP adapter (`Kuestenlogik.Surgewave.Protocol.Amqp`)
implements the same framing and method set so that RabbitMQ client libraries see Surgewave as a valid
AMQP 0.9.1 broker.

## Surgewave as an AMQP Server

Surgewave listens for AMQP connections on port **5672** by default (same as RabbitMQ). Enable it via:

```json
{
  "Surgewave": {
    "Amqp": {
      "Enabled": true,
      "Port": 5672
    }
  }
}
```

Or via the command line:

```bash
dotnet run --project src/Kuestenlogik.Surgewave.Broker -- \
  --Surgewave:Amqp:Enabled=true \
  --Surgewave:Amqp:Port=5672 \
  --Surgewave:QueueView:Enabled=true
```

> **Important:** Enable QueueView alongside AMQP to get full ack/nack/requeue support.
> Without QueueView, the AMQP adapter falls back to log-based offset tracking and requeue is a no-op.

## Exchange Types and Surgewave Topic Mapping

AMQP producers publish to an **exchange**. Surgewave maps exchanges to Surgewave topics by name:

| AMQP Exchange | Surgewave Topic |
|---------------|-------------|
| `""` (default exchange) | Message routing key used as topic name |
| `"orders"` | `orders` |
| `"tenant.payments"` | `tenant/payments` (`.` → `/`) |

Exchange types (`direct`, `fanout`, `topic`, `headers`) are accepted by the AMQP adapter but full
routing-key pattern matching is not implemented. Messages are routed to Surgewave topics by name only.

### Declaring an Exchange

```csharp
// Declare an exchange — Surgewave accepts and maps it to a Surgewave topic
await channel.ExchangeDeclareAsync(
    exchange: "orders",
    type: ExchangeType.Direct,
    durable: true,
    autoDelete: false);
```

## Queue to Consumer Group Mapping

AMQP consumers bind to **queues**. Surgewave maps AMQP queues to Surgewave consumer groups:

| AMQP Queue Declaration | Surgewave Consumer Group |
|----------------------|---------------------|
| `QueueDeclare("orders")` | Consumer group `"orders"` on topic `"orders"` |
| `QueueBind("orders", "payments", "")` | Consumer group `"orders"` reading from topic `"payments"` |

When `Basic.Consume` is called, the AMQP adapter:
1. Resolves the Surgewave topic from the queue binding
2. Calls `QueueViewManager.GetOrCreate(topic, log)` to enrol the topic
3. Starts delivering messages from QueueView with visibility timeout tracking

## Connecting a RabbitMQ .NET Client to Surgewave

Any RabbitMQ 6.x or 7.x .NET client works without code changes. Change the hostname and port only:

```csharp
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// Before: points to RabbitMQ
// After: points to Surgewave — no other changes needed
var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,          // Surgewave AMQP port (same as RabbitMQ default)
    UserName = "guest",
    Password = "guest",
    VirtualHost = "/"
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// Declare the Surgewave topic as an AMQP queue
await channel.QueueDeclareAsync(
    queue: "orders",
    durable: true,
    exclusive: false,
    autoDelete: false);

// Consumer with explicit acknowledgement
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"Received: {message} (redelivered: {ea.Redelivered})");

        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Processing failed: {ex.Message}");
        // Requeue — Surgewave QueueView will re-deliver after visibility timeout
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
    }
};

await channel.BasicConsumeAsync("orders", autoAck: false, consumer: consumer);
```

## AMQP Producer

```csharp
// Publish to Surgewave via AMQP — same as publishing to RabbitMQ
var body = Encoding.UTF8.GetBytes("""{"orderId": "123", "amount": 99.95}""");

var props = new BasicProperties
{
    ContentType = "application/json",
    DeliveryMode = DeliveryModes.Persistent,
    MessageId = Guid.NewGuid().ToString()
};

await channel.BasicPublishAsync(
    exchange: "",
    routingKey: "orders",   // Routes to Surgewave topic "orders"
    mandatory: false,
    basicProperties: props,
    body: body);
```

## Docker Example: Connecting to Surgewave

```yaml
# docker-compose.yml
services:
  surgewave:
    image: surgewave:latest
    ports:
      - "9092:9092"   # Kafka protocol
      - "5672:5672"   # AMQP protocol
      - "5050:5050"   # Control UI
    environment:
      Surgewave__QueueView__Enabled: "true"
      Surgewave__QueueView__MaxDeliveryCount: "5"
      Surgewave__Amqp__Enabled: "true"
      Surgewave__Amqp__Port: "5672"

  my-app:
    image: my-rabbitmq-app:latest
    environment:
      RABBITMQ_HOST: surgewave       # Point app at Surgewave instead of RabbitMQ
      RABBITMQ_PORT: "5672"
    depends_on:
      - surgewave
```

## AMQP Method Support Matrix

| AMQP Method | Supported | Notes |
|-------------|-----------|-------|
| `Connection.Start/Tune/Open/Close` | Yes | Full handshake |
| `Channel.Open/Close` | Yes | Multi-channel per connection |
| `Exchange.Declare/Delete` | Yes | Mapped to Surgewave topic names |
| `Queue.Declare/Bind/Purge/Delete` | Yes | Mapped to Surgewave consumer groups |
| `Basic.Publish` | Yes | Appends to Surgewave partition log |
| `Basic.Consume/Cancel` | Yes | Triggers QueueView enrollment |
| `Basic.Ack` (single + multiple) | Yes | Advances committed offset |
| `Basic.Nack` (requeue=true/false) | Yes | Requeue or drop |
| `Basic.Reject` (requeue=true/false) | Yes | Requeue or route to DLQ |
| `Basic.Qos` | Partial | Accepted, `MaxInFlightPerConsumer` is a soft cap |
| `Basic.Confirm` (publisher confirms) | No | Not implemented |
| `Tx.*` (transactions) | No | Not implemented |

---

[Back to Unit 12](../README.md)

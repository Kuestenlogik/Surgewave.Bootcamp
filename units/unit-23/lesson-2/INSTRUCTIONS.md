# Lesson 14.2: Instructions

Follow these step-by-step instructions to orchestrate Surgewave with Docker Compose.

## Step 1: Understand Service Discovery

In Docker Compose, services can reach each other by name:

```csharp
// Instead of IP addresses, use service names
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "surgewave-broker:9092";  // Docker DNS resolves this
```

## Step 2: Handle Multiple Brokers

Configure your client to work with a broker cluster:

```csharp
// Comma-separated list of brokers
var bootstrapServers = "surgewave-broker-1:9092,surgewave-broker-2:9092,surgewave-broker-3:9092";
```

## Step 3: Wait for Dependencies

Implement retry logic since `depends_on` only waits for container start, not readiness:

```csharp
async Task<bool> WaitForBrokersAsync(string bootstrapServers, CancellationToken ct)
{
    var maxRetries = 30;
    var retryDelay = TimeSpan.FromSeconds(2);

    for (int i = 0; i < maxRetries && !ct.IsCancellationRequested; i++)
    {
        try
        {
            // Try to connect
            using var testProducer = new SurgewaveProducer<string, string>(options =>
            {
                options.BootstrapServers = bootstrapServers;
            });
            Console.WriteLine("Successfully connected to brokers!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Waiting for brokers... attempt {i + 1}/{maxRetries}");
            await Task.Delay(retryDelay, ct);
        }
    }

    return false;
}
```

## Step 4: Create docker-compose.yml

Create a Docker Compose file for a multi-broker setup:

```yaml
version: "3.8"

services:
  # Surgewave Broker 1
  surgewave-broker-1:
    image: surgewave-broker:latest
    hostname: surgewave-broker-1
    ports:
      - "9092:9092"
    environment:
      - Surgewave_BROKER_ID=1
      - Surgewave_LISTENERS=PLAINTEXT://0.0.0.0:9092
      - Surgewave_ADVERTISED_LISTENERS=PLAINTEXT://surgewave-broker-1:9092
      - Surgewave_DATA_DIR=/var/surgewave/data
    volumes:
      - broker1-data:/var/surgewave/data
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    networks:
      - surgewave-network

  # Surgewave Broker 2
  surgewave-broker-2:
    image: surgewave-broker:latest
    hostname: surgewave-broker-2
    ports:
      - "9093:9092"
    environment:
      - Surgewave_BROKER_ID=2
      - Surgewave_LISTENERS=PLAINTEXT://0.0.0.0:9092
      - Surgewave_ADVERTISED_LISTENERS=PLAINTEXT://surgewave-broker-2:9092
      - Surgewave_DATA_DIR=/var/surgewave/data
    volumes:
      - broker2-data:/var/surgewave/data
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    networks:
      - surgewave-network

  # Producer Application
  surgewave-producer:
    build:
      context: ./producer
      dockerfile: Dockerfile
    environment:
      - Surgewave_BOOTSTRAP_SERVERS=surgewave-broker-1:9092,surgewave-broker-2:9092
      - Surgewave_TOPIC=compose-test
    depends_on:
      surgewave-broker-1:
        condition: service_healthy
      surgewave-broker-2:
        condition: service_healthy
    networks:
      - surgewave-network

  # Consumer Application
  surgewave-consumer:
    build:
      context: ./consumer
      dockerfile: Dockerfile
    environment:
      - Surgewave_BOOTSTRAP_SERVERS=surgewave-broker-1:9092,surgewave-broker-2:9092
      - Surgewave_TOPIC=compose-test
      - Surgewave_GROUP_ID=compose-consumers
    depends_on:
      surgewave-broker-1:
        condition: service_healthy
      surgewave-broker-2:
        condition: service_healthy
    networks:
      - surgewave-network

volumes:
  broker1-data:
  broker2-data:

networks:
  surgewave-network:
    driver: bridge
```

## Step 5: Run with Docker Compose

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f surgewave-producer

# Scale consumers
docker compose up -d --scale surgewave-consumer=3

# Stop all services
docker compose down

# Stop and remove volumes
docker compose down -v
```

## Step 6: Debug Networking

```bash
# Check if services can reach each other
docker compose exec surgewave-producer ping surgewave-broker-1

# View network details
docker network inspect <project>_surgewave-network

# Check service health
docker compose ps
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- Multi-broker configuration
- Startup retry logic
- Health check awareness
- Graceful shutdown

## Sample docker-compose.yml for Testing

```yaml
version: "3.8"

services:
  app:
    build: .
    environment:
      - Surgewave_BOOTSTRAP_SERVERS=surgewave-broker:9092
      - Surgewave_TOPIC=test-topic
    depends_on:
      - surgewave-broker
    networks:
      - surgewave-net

  surgewave-broker:
    image: surgewave-broker:latest
    ports:
      - "9092:9092"
    networks:
      - surgewave-net

networks:
  surgewave-net:
```

## Knowledge Check

1. What's the difference between `depends_on` and `condition: service_healthy`?
2. Why use named volumes instead of bind mounts in production?
3. How does Docker DNS resolve service names?
4. What happens if a health check fails?
5. How do you scale a service to multiple replicas?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 14.2 - Docker Compose ===

Configuration:
  Bootstrap Servers: surgewave-broker-1:9092,surgewave-broker-2:9092
  Topic: compose-test
  Retry Count: 30
  Retry Delay: 2000ms

Waiting for brokers...
  Attempt 1/30: Connecting...
  Attempt 2/30: Connecting...
Successfully connected to brokers!

Starting message producer...
Sent message 1 to compose-test
Sent message 2 to compose-test
^C
Shutting down gracefully...

Lesson complete!
```

---

**Next:** [Lesson 14.3: Kubernetes Fundamentals](../lesson-3/README.md)

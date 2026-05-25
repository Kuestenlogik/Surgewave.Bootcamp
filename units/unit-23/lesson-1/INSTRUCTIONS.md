# Lesson 14.1: Instructions

Follow these step-by-step instructions to containerize a Surgewave application.

## Step 1: Understand Environment Configuration

Surgewave applications should read configuration from environment variables to work well in containers:

```csharp
var bootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
    ?? "localhost:9092";

var topic = Environment.GetEnvironmentVariable("Surgewave_TOPIC")
    ?? "default-topic";
```

This pattern allows the same image to work in different environments by changing environment variables.

## Step 2: Implement Graceful Shutdown

Containers receive SIGTERM when stopping. Handle it gracefully:

```csharp
var cts = new CancellationTokenSource();

// Handle Ctrl+C
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("Received Ctrl+C, shutting down...");
    e.Cancel = true;
    cts.Cancel();
};

// Handle SIGTERM (docker stop)
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Console.WriteLine("Received SIGTERM, shutting down...");
    cts.Cancel();
};
```

## Step 3: Create the Dockerfile

Create a multi-stage Dockerfile for your Surgewave application:

```dockerfile
# Build stage - uses full SDK for compilation
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore (cached if unchanged)
COPY *.csproj .
RUN dotnet restore

# Copy source and publish
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Runtime stage - minimal image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser
USER appuser

# Copy published app from build stage
COPY --from=build /app .

# Set default environment variables
ENV Surgewave_BOOTSTRAP_SERVERS=localhost:9092
ENV Surgewave_TOPIC=docker-test

ENTRYPOINT ["dotnet", "Lesson14_1.dll"]
```

## Step 4: Create .dockerignore

Create a `.dockerignore` file to exclude unnecessary files:

```
bin/
obj/
.git/
.vs/
*.user
*.suo
Dockerfile
.dockerignore
```

## Step 5: Build the Docker Image

Build your image with a tag:

```bash
docker build -t surgewave-lesson14-1:latest .
```

## Step 6: Run with Environment Variables

Test with different configurations:

```bash
# Run with default settings
docker run --rm surgewave-lesson14-1:latest

# Run with custom broker
docker run --rm \
  -e Surgewave_BOOTSTRAP_SERVERS=my-broker:9092 \
  -e Surgewave_TOPIC=my-topic \
  surgewave-lesson14-1:latest

# Run interactively with graceful shutdown test
docker run --rm -it surgewave-lesson14-1:latest
# Press Ctrl+C to test graceful shutdown
```

## Step 7: Verify Graceful Shutdown

When running interactively, the container should:

1. Print startup configuration
2. Handle Ctrl+C or `docker stop` gracefully
3. Clean up resources before exiting
4. Exit with code 0

```bash
# In another terminal, test docker stop
docker run --name test-surgewave surgewave-lesson14-1:latest
docker stop test-surgewave  # Should trigger graceful shutdown
```

## Complete Solution

See `completed/Program.cs` for the full implementation with:

- Environment variable configuration
- Graceful shutdown handling
- Configuration logging
- Proper async/await patterns

## Sample Dockerfile for Completed Solution

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Lesson14_1.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENV Surgewave_BOOTSTRAP_SERVERS=localhost:9092
ENV Surgewave_TOPIC=docker-test
ENTRYPOINT ["dotnet", "Lesson14_1.dll"]
```

## Knowledge Check

1. Why use multi-stage builds instead of a single FROM statement?
2. What's the difference between ENTRYPOINT and CMD?
3. Why handle SIGTERM in addition to Console.CancelKeyPress?
4. What's the purpose of .dockerignore?
5. Why copy .csproj before the rest of the source code?

## Expected Output

```
=== Surgewave Bootcamp: Lesson 14.1 - Docker Basics ===

Configuration:
  Bootstrap Servers: localhost:9092
  Topic: docker-test

Sending test messages (press Ctrl+C to stop)...
Sent message 1 to docker-test
Sent message 2 to docker-test
^C
Received shutdown signal, cleaning up...
Producer disposed, exiting gracefully.

Lesson complete!
```

---

**Next:** [Lesson 14.2: Docker Compose](../lesson-2/README.md)

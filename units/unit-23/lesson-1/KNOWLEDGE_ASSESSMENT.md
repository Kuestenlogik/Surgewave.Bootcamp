# Knowledge Assessment: Lesson 14.1 - Docker Basics

## Multiple Choice

### 1. What is the main advantage of using multi-stage Docker builds for .NET applications?
- [ ] A) Faster compilation times
- [ ] B) Smaller final images by excluding SDK and source code from runtime image
- [ ] C) Better security through encryption
- [ ] D) Automatic load balancing

<details>
<summary>Answer</summary>
**B)** Multi-stage builds allow you to use the full SDK image for building (~800MB) but copy only the published output to a smaller runtime image (~100MB). This results in significantly smaller, more secure production images.
</details>

### 2. In a Dockerfile, what is the purpose of copying the .csproj file and running `dotnet restore` before copying the rest of the source code?
- [ ] A) To reduce image size
- [ ] B) To enable better Docker layer caching for dependencies
- [ ] C) To validate the project file
- [ ] D) To encrypt the dependencies

<details>
<summary>Answer</summary>
**B)** By copying and restoring dependencies separately, Docker can cache that layer. If only source code changes (not dependencies), the restore layer is reused, significantly speeding up subsequent builds.
</details>

### 3. What is the correct configuration priority order (highest to lowest) for Surgewave applications in containers?
- [ ] A) Code defaults, appsettings.json, Environment variables, Command-line
- [ ] B) Command-line, Environment variables, appsettings.json, Code defaults
- [ ] C) appsettings.json, Code defaults, Environment variables, Command-line
- [ ] D) Environment variables, Command-line, Code defaults, appsettings.json

<details>
<summary>Answer</summary>
**B)** Configuration priority from highest to lowest: Command-line arguments > Environment variables > ConfigMap/Secrets > appsettings.json > Code defaults. Higher priority sources override lower ones.
</details>

## True/False

### 4. The ENTRYPOINT instruction in a Dockerfile specifies the command that runs when the container starts.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - ENTRYPOINT defines the executable that runs when the container starts. For .NET apps, this is typically `["dotnet", "MyApp.dll"]`. CMD can provide default arguments that can be overridden.
</details>

### 5. Docker containers should always run as the root user for maximum compatibility.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Running as root is a security risk. Best practice is to create and use a non-root user in the Dockerfile. Many base images include a non-root user option (e.g., `USER app` in .NET images).
</details>

## Code Challenge

### 6. What issues exist in this Dockerfile?

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /out
ENTRYPOINT ["dotnet", "/out/MyApp.dll"]
```

<details>
<summary>Answer</summary>
Issues:

1. **Single-stage build**: Final image includes SDK (~800MB) when only runtime is needed
2. **Poor layer caching**: Copying everything before restore means dependency cache is invalidated on any source change
3. **No .dockerignore**: Likely copies unnecessary files (bin, obj, .git)

Improved version:
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore (cached layer)
COPY *.csproj .
RUN dotnet restore

# Copy source and publish
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .

# Use non-root user
USER app

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

Also create `.dockerignore`:
```
bin/
obj/
.git/
*.md
```
</details>

### 7. How would you configure a Surgewave application to read bootstrap servers from an environment variable with a fallback to a default value?

```csharp
// Implement configuration reading
var producer = new SurgewaveProducer<string, string>(options =>
{
    // Configure BootstrapServers from environment with fallback
});
```

<details>
<summary>Answer</summary>
```csharp
var producer = new SurgewaveProducer<string, string>(options =>
{
    options.BootstrapServers = Environment.GetEnvironmentVariable("Surgewave_BOOTSTRAP_SERVERS")
        ?? "localhost:9092";

    // Additional configuration from environment
    var clientId = Environment.GetEnvironmentVariable("Surgewave_CLIENT_ID");
    if (!string.IsNullOrEmpty(clientId))
    {
        options.ClientId = clientId;
    }
});

// Docker run command:
// docker run -e Surgewave_BOOTSTRAP_SERVERS=broker:9092 -e Surgewave_CLIENT_ID=producer-1 myapp

// Docker Compose:
// environment:
//   - Surgewave_BOOTSTRAP_SERVERS=broker:9092
//   - Surgewave_CLIENT_ID=producer-1
```

For more complex configuration, use IConfiguration:
```csharp
var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("Surgewave_")
    .Build();

var config = builder.GetSection("Surgewave").Get<SurgewaveConfiguration>()
    ?? new SurgewaveConfiguration { BootstrapServers = "localhost:9092" };
```
</details>

## Score
- 7/7: Excellent! You understand Docker basics for Surgewave applications
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding

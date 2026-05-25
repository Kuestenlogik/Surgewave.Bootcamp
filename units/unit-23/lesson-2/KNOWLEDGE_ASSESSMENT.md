# Knowledge Assessment: Lesson 14.2 - Docker Compose

## Multiple Choice

### 1. What is the purpose of `depends_on` with `condition: service_healthy` in Docker Compose?
- [ ] A) To start services alphabetically
- [ ] B) To ensure a service only starts after its dependency passes health checks
- [ ] C) To share volumes between services
- [ ] D) To limit CPU usage

<details>
<summary>Answer</summary>
**B)** Using `depends_on` with `condition: service_healthy` ensures the dependent service waits until the dependency's health check passes before starting. This prevents race conditions where services try to connect before dependencies are ready.
</details>

### 2. How do containers in the same Docker Compose network communicate with each other?
- [ ] A) Using localhost
- [ ] B) Using the container ID
- [ ] C) Using the service name as the hostname
- [ ] D) Using IP addresses only

<details>
<summary>Answer</summary>
**C)** Docker Compose creates a default network where services can communicate using their service names as hostnames. For example, a producer can connect to `surgewave-broker:9092` where `surgewave-broker` is the service name.
</details>

### 3. What is the difference between named volumes and bind mounts in Docker Compose?
- [ ] A) Named volumes are faster
- [ ] B) Named volumes are managed by Docker; bind mounts map to host directories
- [ ] C) Bind mounts are more secure
- [ ] D) There is no difference

<details>
<summary>Answer</summary>
**B)** Named volumes are managed by Docker (stored in Docker's storage area, portable across hosts). Bind mounts map a specific host directory into the container (useful for development, accessing host files).
</details>

## True/False

### 4. Docker Compose health check `start_period` allows time for the container to initialize before health checks start counting failures.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - The `start_period` gives containers time to initialize. During this period, failed health checks don't count toward the retry limit. This is useful for applications that need time to start up before they can respond to health checks.
</details>

### 5. Data in named volumes is automatically deleted when you run `docker compose down`.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Named volumes persist by default after `docker compose down`. To remove volumes, you must explicitly use `docker compose down -v` (volumes flag). This ensures data isn't accidentally lost.
</details>

## Code Challenge

### 6. What issues exist in this docker-compose.yml?

```yaml
version: "3.8"

services:
  broker:
    image: surgewave-broker:latest
    ports:
      - "9092:9092"

  producer:
    image: surgewave-producer:latest
    environment:
      - BOOTSTRAP_SERVERS=localhost:9092
    depends_on:
      - broker
```

<details>
<summary>Answer</summary>
Issues:

1. **Wrong hostname**: `localhost:9092` won't work; should use service name `broker:9092`
2. **No health check dependency**: `depends_on` without condition only waits for container start, not readiness
3. **No health check on broker**: Producer might fail if broker isn't ready
4. **No volumes**: Broker data will be lost on restart
5. **No network explicitly defined**: Works but explicit is better

Fixed version:
```yaml
version: "3.8"

services:
  broker:
    image: surgewave-broker:latest
    ports:
      - "9092:9092"
    volumes:
      - broker-data:/data
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    networks:
      - surgewave-network

  producer:
    image: surgewave-producer:latest
    environment:
      - BOOTSTRAP_SERVERS=broker:9092
    depends_on:
      broker:
        condition: service_healthy
    networks:
      - surgewave-network

volumes:
  broker-data:

networks:
  surgewave-network:
    driver: bridge
```
</details>

### 7. Write a Docker Compose health check for a Surgewave broker that verifies the service is ready:

```yaml
services:
  surgewave-broker:
    image: surgewave-broker:latest
    # Add health check configuration
```

<details>
<summary>Answer</summary>
```yaml
services:
  surgewave-broker:
    image: surgewave-broker:latest
    ports:
      - "9092:9092"
      - "8080:8080"  # Management/health endpoint
    environment:
      - Surgewave_BROKER_ID=1
      - Surgewave_DATA_DIR=/data
    volumes:
      - broker-data:/data
    healthcheck:
      # Option 1: HTTP health endpoint
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      # Option 2: TCP port check (if no HTTP endpoint)
      # test: ["CMD-SHELL", "nc -z localhost 9092 || exit 1"]
      # Option 3: Custom health script
      # test: ["CMD", "/app/health-check.sh"]
      interval: 10s        # Check every 10 seconds
      timeout: 5s          # Wait 5 seconds for response
      retries: 3           # Fail after 3 consecutive failures
      start_period: 30s    # Give 30s for initial startup
    restart: unless-stopped
    networks:
      - surgewave-network

volumes:
  broker-data:
    driver: local

networks:
  surgewave-network:
    driver: bridge
```

Key points:
- `interval`: How often to run the check
- `timeout`: Max time to wait for check to complete
- `retries`: Failures before marking unhealthy
- `start_period`: Grace period during startup
- `test`: The actual check command (exits 0 = healthy, 1 = unhealthy)
</details>

## Score
- 7/7: Excellent! You understand Docker Compose for Surgewave clusters
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding

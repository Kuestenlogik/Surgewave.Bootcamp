# Knowledge Assessment: Lesson 13.3 - Health Checks

## Multiple Choice

### 1. What are the three possible health status values in ASP.NET Core health checks?
- [ ] A) Good, Bad, Unknown
- [ ] B) Healthy, Degraded, Unhealthy
- [ ] C) Running, Stopped, Error
- [ ] D) Up, Down, Pending

<details>
<summary>Answer</summary>
**B)** The three HealthStatus values are Healthy (everything is working), Degraded (working but with issues like high latency or lag), and Unhealthy (not working, requires attention).
</details>

### 2. How is consumer lag calculated?
- [ ] A) Current time minus message timestamp
- [ ] B) End offset minus consumer position offset
- [ ] C) Total messages divided by processing rate
- [ ] D) Broker count minus consumer count

<details>
<summary>Answer</summary>
**B)** Consumer lag is calculated as End Offset - Consumer Position. For example, if the topic has messages up to offset 8 and the consumer is at offset 4, the lag is 4 messages.
</details>

### 3. What is the correct way to register a custom health check?
- [ ] A) services.AddSingleton<IHealthCheck, MyHealthCheck>();
- [ ] B) services.AddHealthChecks().AddCheck<MyHealthCheck>("my-check");
- [ ] C) services.AddScoped<MyHealthCheck>();
- [ ] D) app.UseHealthCheck<MyHealthCheck>();

<details>
<summary>Answer</summary>
**B)** Custom health checks are registered using `services.AddHealthChecks().AddCheck<MyHealthCheck>("my-check")`. This integrates them into the health check system with a named identifier.
</details>

## True/False

### 4. A health check should return Degraded when the broker responds slowly but is still operational.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Degraded is appropriate for situations where the service is working but not optimally. Slow broker responses, high consumer lag within acceptable limits, or elevated error rates that don't prevent operation are good examples of Degraded status.
</details>

### 5. Health check endpoints should always be authenticated to prevent unauthorized access.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Health check endpoints, especially liveness probes, typically should NOT require authentication. Kubernetes and load balancers need to access these endpoints to determine pod/instance health. However, detailed health information endpoints may warrant authentication.
</details>

## Code Challenge

### 6. What is wrong with this health check implementation?

```csharp
public class SurgewaveBrokerHealthCheck : IHealthCheck
{
    private readonly SurgewaveClient _client;

    public SurgewaveBrokerHealthCheck(SurgewaveClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isConnected = await _client.IsConnectedAsync();
        if (isConnected)
            return HealthCheckResult.Healthy();
        else
            throw new Exception("Broker not connected");
    }
}
```

<details>
<summary>Answer</summary>
Issues:

1. **Throwing exception instead of returning Unhealthy**: Health checks should return HealthCheckResult.Unhealthy instead of throwing exceptions
2. **No timeout handling**: Long-running checks could delay health endpoint responses
3. **No cancellation token usage**: The cancellation token is ignored
4. **Missing description**: Results should include descriptive messages

Fixed version:
```csharp
public class SurgewaveBrokerHealthCheck : IHealthCheck
{
    private readonly SurgewaveClient _client;

    public SurgewaveBrokerHealthCheck(SurgewaveClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // Timeout

            var isConnected = await _client.IsConnectedAsync(cts.Token);

            if (isConnected)
            {
                return HealthCheckResult.Healthy("Surgewave broker is reachable");
            }
            else
            {
                return HealthCheckResult.Unhealthy("Surgewave broker is not connected");
            }
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Health check timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}");
        }
    }
}
```
</details>

### 7. Implement a consumer lag health check that returns Healthy (lag < 100), Degraded (100-1000), or Unhealthy (>1000):

```csharp
public class ConsumerLagHealthCheck : IHealthCheck
{
    // Implement CheckHealthAsync
}
```

<details>
<summary>Answer</summary>
```csharp
public class ConsumerLagHealthCheck : IHealthCheck
{
    private readonly SurgewaveConsumerMetrics _metrics;
    private const int HealthyThreshold = 100;
    private const int DegradedThreshold = 1000;

    public ConsumerLagHealthCheck(SurgewaveConsumerMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lag = await _metrics.GetTotalLagAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "lag", lag },
                { "timestamp", DateTime.UtcNow }
            };

            if (lag < HealthyThreshold)
            {
                return HealthCheckResult.Healthy(
                    $"Consumer lag is {lag} messages",
                    data);
            }
            else if (lag < DegradedThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"Consumer lag is elevated: {lag} messages",
                    data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy(
                    $"Consumer lag is critical: {lag} messages",
                    data: data);
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Failed to check consumer lag: {ex.Message}");
        }
    }
}
```
</details>

## Score
- 7/7: Excellent! You understand health checks for Surgewave
- 5-6/7: Good understanding, review the concepts you missed
- 3-4/7: Review the lesson material
- <3/7: Re-do the lesson before proceeding

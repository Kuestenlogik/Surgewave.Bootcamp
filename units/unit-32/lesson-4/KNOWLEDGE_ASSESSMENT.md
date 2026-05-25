# Knowledge Assessment: Lesson 27.4 - Geo-Routing and Latency Optimization

## Multiple Choice

### 1. What is the main advantage of latency-based routing over geolocation-based routing?
- [ ] A) It's simpler to implement
- [ ] B) It adapts to actual network conditions dynamically
- [ ] C) It's more accurate for compliance
- [ ] D) It requires no client-side changes

<details>
<summary>Answer</summary>
**B)** Latency-based routing measures actual network conditions and adapts dynamically. A user in Spain might have better latency to a US region than EU due to network topology. Geolocation assumes geographic proximity equals network proximity, which isn't always true.
</details>

### 2. What is the primary benefit of rack awareness in cloud deployments?
- [ ] A) Better security
- [ ] B) Simplified configuration
- [ ] C) Reduced cross-AZ data transfer costs and latency
- [ ] D) Automatic failover

<details>
<summary>Answer</summary>
**C)** Rack awareness allows consumers to prefer reading from brokers in the same availability zone (AZ). This reduces cross-AZ data transfer charges (which can be significant) and provides lower latency since data doesn't cross datacenter boundaries.
</details>

### 3. Why is P99 latency often more important than average latency?
- [ ] A) It's easier to calculate
- [ ] B) It shows the experience of worst-affected users
- [ ] C) It's always higher than P50
- [ ] D) It ignores outliers

<details>
<summary>Answer</summary>
**B)** P99 (99th percentile) shows the latency experienced by the slowest 1% of requests. This matters because: (1) those users have a poor experience, (2) system issues often manifest in tail latency first, and (3) average can hide significant issues affecting many users.
</details>

### 4. When should follower fetching be enabled?
- [ ] A) Always, for all workloads
- [ ] B) When read latency is more important than read-your-writes consistency
- [ ] C) Only for producers
- [ ] D) Only during failover

<details>
<summary>Answer</summary>
**B)** Follower fetching allows consumers to read from follower replicas instead of only the leader. This is beneficial when low read latency is critical (reading from a nearby replica) and strict read-your-writes consistency isn't required. Note: data on followers may be slightly behind the leader.
</details>

### 5. What trade-off does increasing batch size introduce?
- [ ] A) Higher throughput but higher latency
- [ ] B) Lower throughput but lower latency
- [ ] C) No trade-off, always better
- [ ] D) Higher memory but lower CPU

<details>
<summary>Answer</summary>
**A)** Larger batches mean more messages per network request (higher throughput) but messages wait longer to fill the batch (higher latency). For low-latency requirements, use smaller batches or shorter linger times.
</details>

## True/False

### 6. Anycast routing requires changes to client application code.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Anycast uses BGP to route traffic to the nearest server announcing the same IP address. Clients connect to a single IP; the network handles routing. No client-side changes are needed, making it the simplest approach for clients.
</details>

### 7. Geographic distance always correlates with network latency.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Network topology, peering agreements, and routing paths can result in geographically distant endpoints having lower latency than closer ones. For example, a user in Spain might have better latency to US servers due to direct peering, versus EU servers routing through multiple hops.
</details>

## Code Challenge

### 8. What's wrong with this latency measurement implementation?

```csharp
public class LatencyTracker
{
    private long _totalLatency;
    private int _count;

    public void Record(long latencyMs)
    {
        _totalLatency += latencyMs;
        _count++;
    }

    public double GetAverageLatency() => _totalLatency / _count;
    public double GetP99Latency() => _totalLatency * 0.99; // Wrong!
}
```

<details>
<summary>Answer</summary>
Multiple issues:

1. **P99 calculation is completely wrong** - P99 is the 99th percentile, not 99% of the total. You need to store individual samples and sort them.
2. **Integer division** - `_totalLatency / _count` loses precision
3. **No thread safety** - Concurrent access could cause incorrect values
4. **No protection against division by zero** - When `_count` is 0

Correct implementation:

```csharp
public class LatencyTracker
{
    private readonly List<long> _samples = new();
    private readonly int _maxSamples;
    private readonly object _lock = new();

    public LatencyTracker(int maxSamples = 10000)
    {
        _maxSamples = maxSamples;
    }

    public void Record(long latencyMs)
    {
        lock (_lock)
        {
            _samples.Add(latencyMs);
            // Keep bounded to avoid memory issues
            if (_samples.Count > _maxSamples)
                _samples.RemoveAt(0);
        }
    }

    public double GetAverageLatency()
    {
        lock (_lock)
        {
            return _samples.Count > 0 ? _samples.Average() : 0;
        }
    }

    public long GetP99Latency()
    {
        lock (_lock)
        {
            if (_samples.Count == 0) return 0;

            var sorted = _samples.OrderBy(x => x).ToList();
            var index = (int)(sorted.Count * 0.99);
            return sorted[Math.Min(index, sorted.Count - 1)];
        }
    }

    public LatencyStats GetStats()
    {
        lock (_lock)
        {
            if (_samples.Count == 0)
                return new LatencyStats(0, 0, 0, 0, 0, 0);

            var sorted = _samples.OrderBy(x => x).ToList();
            var count = sorted.Count;

            return new LatencyStats(
                Count: count,
                Min: sorted.First(),
                Max: sorted.Last(),
                Avg: (long)sorted.Average(),
                P50: sorted[count / 2],
                P99: sorted[(int)(count * 0.99)]
            );
        }
    }
}

public record LatencyStats(int Count, long Min, long Max, long Avg, long P50, long P99);
```
</details>

## Scenario Challenge

### 9. Design a geo-routing strategy for a real-time gaming platform with these requirements:
- Players worldwide need < 50ms latency for competitive fairness
- Game state must be consistent (no split-brain)
- Support for 1M concurrent players
- Cost optimization is important

What routing and optimization strategies would you recommend?

<details>
<summary>Answer</summary>
**Recommended Architecture: Regional Sharding with Latency-Based Routing**

**Strategy:**

1. **Regional Game Servers with Latency Limits**
```
Regions:
- us-east-1 (Virginia) - North/South America
- eu-west-1 (Ireland) - Europe, Africa
- ap-northeast-1 (Tokyo) - East Asia
- ap-southeast-1 (Singapore) - Southeast Asia, Oceania

Each region has:
- Surgewave cluster for event streaming
- Game servers for match hosting
- 50ms latency boundary enforcement
```

2. **Latency-Based Matchmaking**
```csharp
// Only match players who can all reach the same region under 50ms
public class MatchMaker
{
    public string SelectGameRegion(List<Player> players)
    {
        // Find region where ALL players have < 50ms latency
        foreach (var region in _regions)
        {
            if (players.All(p => p.LatencyTo(region) < 50))
                return region;
        }

        // No region works - reject match or warn players
        throw new MatchmakingException("Players too geographically dispersed");
    }
}
```

3. **Cost Optimization**
```
- Rack awareness: Consumers in same AZ as brokers (no cross-AZ charges)
- Compression: LZ4 for game events (fast, ~60% reduction)
- Batching: Small batches (5ms linger) for low latency
- Spot instances: Use for non-critical analytics processing
```

4. **Consistency Strategy**
```
- Single region owns each game match (no cross-region replication during match)
- Game state replicated AFTER match ends (for stats, leaderboards)
- Players forced to use their region's cluster (no cross-region play)
```

**Topic Design:**
```
Regional (not replicated during match):
  {region}.games.{game-id}.events   - Real-time game events
  {region}.games.{game-id}.state    - Game state updates

Global (async replication):
  global.leaderboards.updates       - Post-match stats
  global.players.profiles           - Player data
```

**Latency Budget:**
```
Total budget: 50ms
- Network to region: 20ms max
- Surgewave produce: 5ms
- Game server processing: 15ms
- Surgewave consume: 5ms
- Network to player: 5ms (via CDN)
```

**Why This Works:**
- Latency enforced through matchmaking (< 50ms guaranteed)
- No cross-region replication during gameplay (consistency)
- Cost optimized via rack awareness and compression
- Scales to 1M players (sharded by region)
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson

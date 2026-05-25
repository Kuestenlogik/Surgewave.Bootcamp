# Lesson 9.4: Common Issues

> **Difficulty:** Intermediate | **Duration:** 30 min | **Prerequisites:** Lesson 9.3

Debug consumer lag, rebalancing, and timeout issues.

## Objectives

- Diagnose and fix consumer lag
- Handle partition rebalancing gracefully
- Debug and resolve timeout issues

## Concepts

### Consumer Lag

Consumer lag is the difference between the latest produced offset and the last consumed offset:

```
Lag = Latest Offset - Committed Offset
```

| Symptom | Possible Cause | Solution |
|---------|---------------|----------|
| Increasing lag | Slow processing | Add consumers, optimize processing |
| Sudden spike | Consumer restart | Expected, will catch up |
| Constant high lag | Throughput mismatch | Increase consumer count |

### Partition Rebalancing

Rebalancing occurs when:
- Consumer joins or leaves the group
- Topics/partitions are added
- Consumer fails to heartbeat

```
Consumer A: [P0, P1]     Rebalance     Consumer A: [P0]
                          ------->
                                       Consumer B: [P1]
```

### Timeout Types

| Timeout | Default | Purpose |
|---------|---------|---------|
| Session | 45s | Detect dead consumers |
| Heartbeat | 3s | Background keep-alive |
| Max Poll Interval | 5m | Max time between polls |
| Request | 30s | Network request timeout |

## Exercise

In this lesson, you'll:
1. Simulate and monitor consumer lag
2. Handle rebalance events
3. Debug timeout issues

## Key Takeaways

- Monitor lag to detect processing issues
- Use rebalance callbacks for graceful handoff
- Tune timeouts based on workload

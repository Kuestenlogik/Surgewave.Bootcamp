# Lesson 2.3: Consumer Groups & Lag Monitoring

The Consumer Groups section provides visibility into consumer behavior, offset positions, and lag across all partitions.

## Learning Objectives

- Monitor consumer group state and member assignments
- Visualize consumer lag using bar and line charts
- Reset consumer group offsets using various strategies
- Enable real-time lag monitoring with auto-refresh

## Consumer Group States

| State | Color | Description |
|-------|-------|-------------|
| Stable | Green | All members assigned, consuming normally |
| Rebalancing | Yellow | Partition reassignment in progress |
| PreparingRebalance | Orange | Rebalance triggered, waiting for members |
| Empty | Gray | No active members |
| Dead | Red | Group has been removed or expired |

## Consumer Group Detail

Clicking a group shows member assignments and per-partition offset positions, including current offset, end offset, and lag for each partition.

## Consumer Lag Dashboard

The lag dashboard uses MudChart bar and line charts to visualize:

- **Lag by Partition**: Bar chart showing current lag per partition
- **Lag Over Time**: Line chart showing lag trend over a time window

## Offset Reset

Reset consumer group offsets when you need to reprocess messages or skip ahead:

| Reset Strategy | Description |
|----------------|-------------|
| Earliest | Reset to beginning of topic |
| Latest | Reset to end of topic (skip all existing) |
| To Offset | Reset to a specific offset number |
| To Timestamp | Reset to the offset at a given timestamp |
| Shift By | Move offset forward or backward by N |

The UI requires the consumer group to be in `Empty` state (no active members) before allowing offset resets.

## Real-Time Lag Monitoring

Enable auto-refresh to monitor lag in real-time. The dashboard polls the broker at configurable intervals (default: 5 seconds) and updates the charts dynamically.

---

[Back to Unit 2](../README.md)

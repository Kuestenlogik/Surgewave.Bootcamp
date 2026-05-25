# Lesson 2.1: Dashboard & Navigation

The home dashboard is the central hub of the Surgewave Control UI. It displays 14 configurable widgets that provide a real-time overview of your cluster health, performance, and activity.

## Learning Objectives

- Navigate the Surgewave Control dashboard
- Customize widget visibility and layout
- Configure theme preferences (Light, Dark, System)
- Understand dashboard persistence via LocalStorage

## Dashboard Widgets

| Widget | Description |
|--------|-------------|
| Cluster Status | Overall health indicator (Healthy, Degraded, Critical) |
| Throughput | Messages per second across all topics |
| Latency P50 | Median end-to-end latency |
| Latency P90 | 90th percentile latency |
| Latency P99 | 99th percentile latency |
| Brokers | Active broker count and status |
| Topics | Total topic count |
| Consumer Groups | Active consumer group count |
| Partitions | Total partition count across topics |
| Errors | Recent error count with severity |
| Pipelines | Active pipeline count and status |
| Connectors | Running connector count and health |
| Cluster Info | Broker version, uptime, cluster ID |
| Topics Table | Sortable table of topics with message rates |
| Consumer Groups Table | Table of consumer groups with lag indicators |
| Recent Pipelines | Recently modified pipelines with quick actions |

## Widget Visibility Toggle

The settings menu (gear icon in the top-right) lets you show or hide individual widgets. Changes are persisted to the browser's LocalStorage.

## Drag-and-Drop Widget Reordering

Widgets can be reordered by dragging them to new positions. The layout is persisted in the browser's LocalStorage, so your custom arrangement is remembered across sessions and browser restarts.

```csharp
// Layout persistence model
public record DashboardLayout
{
    public List<WidgetPosition> Widgets { get; init; } = [];
    public Dictionary<string, bool> WidgetVisibility { get; init; } = new();
    public string Theme { get; init; } = "system";
}

// Saved to LocalStorage under key "surgewave-dashboard-layout"
await LocalStorage.SetItemAsync("surgewave-dashboard-layout", layout);
```

## Theme Toggle

The Control UI supports Dark and Light themes with automatic system preference detection:

| Mode | Description |
|------|-------------|
| Light | White background, dark text, MudBlazor default palette |
| Dark | Dark background, light text, reduced eye strain |
| System | Follows OS-level dark/light preference via `prefers-color-scheme` |

Theme selection is persisted in LocalStorage alongside the dashboard layout.

## Exercise

1. Open the Surgewave Control UI at `http://localhost:5050`
2. Open the settings menu and hide the "Cluster Info" and "Errors" widgets
3. Rearrange the remaining widgets: place throughput and latency widgets at the top
4. Switch to Dark theme
5. Close and reopen the browser -- verify your layout persists

---

[Back to Unit 2](../README.md)

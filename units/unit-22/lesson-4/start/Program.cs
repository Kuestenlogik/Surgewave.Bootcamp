// Surgewave Bootcamp - Unit 9, Lesson 4: Common Issues
//
// In this lesson, you'll learn to:
// - Monitor and diagnose consumer lag
// - Handle partition rebalancing
// - Debug and fix timeout issues
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.4 - Common Issues ===");
Console.WriteLine();

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// TODO: Create a LagMonitor to track consumer lag
// Hint: var lagMonitor = new LagMonitor();

// TODO: Produce messages to create a backlog
// Hint: for (var i = 0; i < 100; i++)
//       {
//           await producer.ProduceAsync(...);
//           lagMonitor.RecordProduce(i);
//       }

// TODO: Create a slow consumer to generate lag
// Hint: Consume messages with Task.Delay to simulate slow processing

// TODO: Monitor lag and print warnings when high
// Hint: if (lagMonitor.CurrentLag > threshold)
//       {
//           Console.WriteLine($"WARNING: High lag: {lagMonitor.CurrentLag}");
//       }

// TODO: Implement rebalance handling
// Hint: Create a class that tracks partition assignments

// TODO: Simulate timeout scenarios
// Hint: Create scenarios that would cause different timeout types

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Helper Classes ==============

// TODO: Implement LagMonitor
// Hint: public class LagMonitor
//       {
//           private long _latestOffset;
//           private long _committedOffset;
//           public long CurrentLag => _latestOffset - _committedOffset;
//           ...
//       }

// TODO: Implement RebalanceTracker
// Hint: public class RebalanceTracker
//       {
//           public void OnPartitionsAssigned(int[] partitions) { ... }
//           public void OnPartitionsRevoked(int[] partitions) { ... }
//       }

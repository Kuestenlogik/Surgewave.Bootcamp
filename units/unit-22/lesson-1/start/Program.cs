// Surgewave Bootcamp - Unit 9, Lesson 1: Logging & Diagnostics
//
// In this lesson, you'll learn to:
// - Configure structured logging with ILogger
// - Subscribe to diagnostic events
// - Implement log filtering and scopes
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Kuestenlogik.Surgewave.Broker;
using Kuestenlogik.Surgewave.Core.Storage;
using Kuestenlogik.Surgewave.Runtime;
using Kuestenlogik.Surgewave.Client;
using Kuestenlogik.Surgewave.Client.Consumer;

Console.WriteLine("=== Surgewave Bootcamp: Lesson 9.1 - Logging & Diagnostics ===");
Console.WriteLine();

// TODO: Create a LoggerFactory with custom configuration
// Hint: using var loggerFactory = LoggerFactory.Create(builder =>
//       {
//           builder.SetMinimumLevel(LogLevel.Debug)
//                  .AddSimpleConsole(options =>
//                  {
//                      options.TimestampFormat = "[HH:mm:ss] ";
//                      options.SingleLine = true;
//                  });
//       });

// TODO: Create a logger instance
// Hint: var logger = loggerFactory.CreateLogger("SurgewaveApp");

// TODO: Subscribe to diagnostic events
// Hint: DiagnosticListener.AllListeners.Subscribe(new SurgewaveDiagnosticObserver(logger));

// Start embedded broker
await using var runtime = await SurgewaveRuntime.CreateBuilder()
    .WithPort(9092)
    .WithStorageEngine(StorageEngines.Memory)
    .WithAutoCreateTopics(true)
    .Build()
    .StartAsync();

Console.WriteLine($"Broker started on port {runtime.Port}");
Console.WriteLine();

// TODO: Log at different levels
// Hint: logger.LogDebug("Debug message");
//       logger.LogInformation("Info message");
//       logger.LogWarning("Warning message");

// TODO: Use structured logging with parameters
// Hint: logger.LogInformation("Connected to broker at {Host}:{Port}", "localhost", runtime.Port);

// TODO: Use logging scopes
// Hint: using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = Guid.NewGuid() }))
//       {
//           logger.LogInformation("Processing request");
//       }

// TODO: Produce and consume with logging
// Hint: Create producer/consumer and log each operation

Console.WriteLine();
Console.WriteLine("Lesson complete!");

// ============== Diagnostic Observer Classes ==============

// TODO: Implement SurgewaveDiagnosticObserver
// Hint: public class SurgewaveDiagnosticObserver : IObserver<DiagnosticListener>
//       {
//           private readonly ILogger _logger;
//           public SurgewaveDiagnosticObserver(ILogger logger) => _logger = logger;
//           public void OnNext(DiagnosticListener listener) { ... }
//           public void OnError(Exception error) { }
//           public void OnCompleted() { }
//       }

// TODO: Implement EventObserver
// Hint: public class EventObserver : IObserver<KeyValuePair<string, object?>>
//       {
//           private readonly ILogger _logger;
//           public EventObserver(ILogger logger) => _logger = logger;
//           public void OnNext(KeyValuePair<string, object?> pair) { ... }
//           public void OnError(Exception error) { }
//           public void OnCompleted() { }
//       }

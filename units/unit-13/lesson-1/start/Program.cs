// Surgewave Bootcamp - Unit 13, Lesson 1: Hosted Services
//
// In this lesson, you'll learn to:
// - Create a BackgroundService that consumes Surgewave messages
// - Implement IHostedService for custom lifecycle control
// - Handle graceful shutdown with CancellationToken
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements for Surgewave and hosting
// Hint: Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

var builder = WebApplication.CreateBuilder(args);

// TODO: Register the Surgewave broker service as a singleton and hosted service
// Hint: Use AddSingleton<SurgewaveBrokerService>() and AddHostedService()

// TODO: Register the message producer service as a hosted service
// Hint: Use AddHostedService<MessageProducerService>()

// TODO: Register the message consumer service as a hosted service
// Hint: Use AddHostedService<MessageConsumerService>()

var app = builder.Build();

app.MapGet("/", () => "Surgewave Consumer Service Running");

app.Run();

// TODO: Implement SurgewaveBrokerService as IHostedService
// This service should:
// - Start an embedded Surgewave broker in StartAsync
// - Expose the Port property for other services to use
// - Clean up the broker in StopAsync
//
// Hint: Use SurgewaveRuntime.CreateBuilder() with WithPort(0), WithStorageMode(StorageMode.Memory)

// TODO: Implement MessageConsumerService as a BackgroundService
// This service should:
// - Create a SurgewaveConsumer in ExecuteAsync
// - Subscribe to "demo-messages" topic
// - Consume and log messages in a loop
// - Handle graceful shutdown via stoppingToken
//
// Hint: Use new SurgewaveConsumer<string, string>(options => { ... })

// TODO: Implement MessageProducerService as a BackgroundService
// This service should:
// - Create a SurgewaveProducer in ExecuteAsync
// - Produce test messages every 2 seconds
// - Handle graceful shutdown via stoppingToken
//
// Hint: Use new SurgewaveProducer<string, string>(options => { ... })

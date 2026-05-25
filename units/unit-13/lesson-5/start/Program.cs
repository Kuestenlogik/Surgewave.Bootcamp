// Surgewave Bootcamp - Unit 13, Lesson 5: Background Processing
//
// In this lesson, you'll learn to:
// - Implement producer pools for high-throughput
// - Create multiple consumer workers
// - Handle graceful shutdown
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: Kuestenlogik.Surgewave.*, System.Collections.Concurrent

var builder = WebApplication.CreateBuilder(args);

// TODO: Configure shutdown timeout
// Hint: builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = ...)

// TODO: Register Surgewave broker service

// TODO: Register producer pool service

// TODO: Register consumer worker service with 3 workers

// TODO: Register message generator service

var app = builder.Build();

// TODO: Add root endpoint at "/"

// TODO: Add stats endpoint at "/stats"

app.Run();

// TODO: Create SurgewaveBrokerService class (reuse from previous lessons)

// TODO: Create ProducerPool<TKey, TValue> class
// - Constructor takes poolSize, bootstrapServers, logger
// - Creates array of producers
// - ProduceAsync uses round-robin selection (Interlocked.Increment)
// - DisposeAsync disposes all producers

// TODO: Create ProducerPoolService : IHostedService
// - Creates ProducerPool in StartAsync
// - Exposes Pool property
// - Disposes pool in StopAsync

// TODO: Create ConsumerWorkerService : BackgroundService
// - Constructor takes broker, logger, workerCount, topic, groupId
// - ExecuteAsync starts multiple RunWorkerAsync tasks
// - Each worker creates its own consumer
// - Workers process messages until cancellation

// TODO: Create MessageGeneratorService : BackgroundService
// - Uses producer pool to generate test messages
// - Generates message every 500ms

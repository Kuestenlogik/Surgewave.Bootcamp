// Surgewave Bootcamp - Unit 13, Lesson 2: Dependency Injection
//
// In this lesson, you'll learn to:
// - Register Surgewave clients with IServiceCollection
// - Create extension methods for clean registration
// - Use appropriate service lifetimes
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Microsoft.Extensions.Options

var builder = WebApplication.CreateBuilder(args);

// TODO: Configure Surgewave options from configuration
// Hint: Create an AddSurgewaveConfiguration extension method

// TODO: Register Surgewave broker service as singleton and hosted service
// Hint: Similar to Lesson 13.1

// TODO: Register Surgewave producer using DI
// Hint: Use AddSingleton with a factory that gets the broker port

var app = builder.Build();

// TODO: Add GET endpoint at "/" that returns a welcome message

// TODO: Add POST endpoint at "/messages" that:
// - Accepts MessageRequest (Key, Value)
// - Uses injected SurgewaveProducer to send the message
// - Returns MessageResponse (Success, Message)

app.Run();

// TODO: Create SurgewaveConfiguration class
// Properties: BootstrapServers (string), DefaultGroupId (string?)

// TODO: Create MessageRequest record
// Properties: Key (string), Value (string)

// TODO: Create MessageResponse record
// Properties: Success (bool), Message (string)

// TODO: Create SurgewaveServiceCollectionExtensions static class
// Methods:
// - AddSurgewaveConfiguration(IServiceCollection, IConfiguration)
// - AddSurgewaveProducer<TKey, TValue>(IServiceCollection, Action<SurgewaveProducerOptions>?)

// TODO: Create SurgewaveBrokerService class (reuse from Lesson 13.1)

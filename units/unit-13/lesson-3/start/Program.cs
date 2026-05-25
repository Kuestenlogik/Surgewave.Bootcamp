// Surgewave Bootcamp - Unit 13, Lesson 3: Health Checks
//
// In this lesson, you'll learn to:
// - Implement IHealthCheck for Surgewave broker connectivity
// - Create producer health checks
// - Configure health check endpoints
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: Kuestenlogik.Surgewave.*, Microsoft.Extensions.Diagnostics.HealthChecks, System.Text.Json

var builder = WebApplication.CreateBuilder(args);

// TODO: Register Surgewave broker service
// Hint: AddSingleton<SurgewaveBrokerService>() and AddHostedService()

// TODO: Register Surgewave producer
// Hint: AddSingleton with factory using broker.Port

// TODO: Add health checks
// Hint: builder.Services.AddHealthChecks()
//       .AddCheck<SurgewaveBrokerHealthCheck>("surgewave-broker", tags: new[] { "surgewave" })
//       .AddCheck<SurgewaveProducerHealthCheck>("surgewave-producer", tags: new[] { "surgewave" });

var app = builder.Build();

// TODO: Map basic health check endpoint at "/health"
// Hint: app.MapHealthChecks("/health");

// TODO: Map detailed health check endpoint at "/health/detail"
// Hint: Use HealthCheckOptions with custom ResponseWriter

// TODO: Add root endpoint
// Hint: app.MapGet("/", () => "Health Check Demo");

app.Run();

// TODO: Create WriteHealthCheckResponse static method
// This should write a JSON response with status, duration, and check details

// TODO: Create SurgewaveBrokerService class (reuse from previous lessons)

// TODO: Create SurgewaveBrokerHealthCheck class implementing IHealthCheck
// - Inject SurgewaveBrokerService
// - Check if Port > 0
// - Return Healthy if broker is running, Unhealthy otherwise

// TODO: Create SurgewaveProducerHealthCheck class implementing IHealthCheck
// - Inject SurgewaveProducer<string, string>
// - Try to send a test message to "__health_check" topic
// - Return Healthy if send succeeds, Unhealthy otherwise

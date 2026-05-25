// Surgewave Bootcamp - Unit 13, Lesson 4: Middleware & Endpoints
//
// In this lesson, you'll learn to:
// - Create Minimal API endpoints for producing messages
// - Implement correlation ID middleware
// - Pass correlation context through message headers
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: Kuestenlogik.Surgewave.*, System.Text.Json

var builder = WebApplication.CreateBuilder(args);

// TODO: Register Surgewave broker service
// Hint: AddSingleton<SurgewaveBrokerService>() and AddHostedService()

// TODO: Register Surgewave producer
// Hint: AddSingleton with factory using broker.Port

var app = builder.Build();

// TODO: Add correlation middleware
// Hint: app.UseCorrelation();

// TODO: Add root endpoint at "/"
// Hint: app.MapGet("/", () => "Middleware & Endpoints Demo");

// TODO: Add POST endpoint at "/api/topics/{topic}/messages"
// This endpoint should:
// - Accept ProduceRequest (Key, Value)
// - Extract correlation ID from HttpContext.Items
// - Produce message using injected producer
// - Return ProduceResponse with Results.Created

// TODO: Add POST endpoint at "/api/topics/{topic}/messages/batch"
// This endpoint should:
// - Accept List<ProduceRequest>
// - Produce each message
// - Return list of ProduceResponse

app.Run();

// TODO: Create ProduceRequest record (Key, Value)

// TODO: Create ProduceResponse record (Success, CorrelationId, Topic, Key, Error?)

// TODO: Create CorrelationMiddleware class
// - Extract X-Correlation-ID from request header
// - Generate new GUID if not present
// - Store in HttpContext.Items
// - Add to response header

// TODO: Create CorrelationMiddlewareExtensions class with UseCorrelation method

// TODO: Create SurgewaveBrokerService class (reuse from previous lessons)

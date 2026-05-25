// Surgewave Bootcamp - Unit 17, Lesson 1: Message Size Limits
//
// In this lesson, you'll learn to:
// - Understand why message size limits exist
// - Configure producer and broker message size limits
// - Handle message size validation errors gracefully
// - Implement size checking before sending messages
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: System.Text, Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("Message Size Limits Demo");
Console.WriteLine("========================");
Console.WriteLine();

// TODO: Start embedded Surgewave broker
// Hint: Use SurgewaveRuntime.CreateBuilder() with WithPort(0), WithStorageMode(StorageMode.Memory)

// TODO: Create producer and consumer
// Hint: Use new SurgewaveProducer<string, string>(options => { ... })

// TODO: Test 1 - Send a small message (100 bytes)
// - Generate a 100 byte payload using MessageSizeValidator.GeneratePayload(100)
// - Validate the size using MessageSizeValidator.Validate()
// - If valid, send the message

// TODO: Test 2 - Send a medium message (500 KB)
// - Generate a 500 KB payload
// - Validate and send if within limits

// TODO: Test 3 - Attempt to send a large message (2 MB)
// - Generate a 2 MB payload
// - Validate - this should FAIL with default 1 MB limit
// - Print that the message was blocked

// TODO: Test 4 - Increase the limit and retry
// - Create a new limit of 5 MB
// - Re-validate the 2 MB message with new limit
// - Send successfully

// TODO: Consume all sent messages and print count

// TODO: Implement MessageSizeValidator helper class
// - DefaultMaxMessageSize constant (1 MB)
// - Validate(key, value, maxSize) method
// - GeneratePayload(sizeInBytes) method

// TODO: Implement ValidationResult record
// - IsValid, TotalSize, MaxSize, KeySize, ValueSize properties
// - ToString() method for nice formatting

// Surgewave Bootcamp - Unit 17, Lesson 4: Claim-Check Pattern
//
// In this lesson, you'll learn to:
// - Understand the claim-check pattern and when to use it
// - Store large payloads in external storage (simulated)
// - Send lightweight reference messages through Surgewave
// - Retrieve payloads using claim-check references
// - Handle cleanup of external storage
//
// Follow the instructions in INSTRUCTIONS.md to complete the TODOs below.

// TODO: Add using statements
// Hint: System.Security.Cryptography, System.Text, System.Text.Json
// Hint: Kuestenlogik.Surgewave.Broker, Kuestenlogik.Surgewave.Runtime, Kuestenlogik.Surgewave.Client, Kuestenlogik.Surgewave.Client.Consumer

Console.WriteLine("Claim-Check Pattern Demo");
Console.WriteLine("========================");
Console.WriteLine();

// TODO: Start embedded Surgewave broker
// Hint: Use SurgewaveRuntime.CreateBuilder()

// TODO: Initialize external storage (local file system)
// Hint: var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "claim-check-demo"));

// TODO: Create a large payload (10 MB of random data)
// Hint: Use Random.Shared.NextBytes()

// TODO: Store payload in external storage
// - Call storage.StoreAsync(largePayload)
// - Get back a ClaimCheck reference
// - Create a ClaimCheckMessage with the reference and metadata

// TODO: Print storage info
// - Location where payload was stored
// - Size of the claim check reference (should be small!)
// - Percentage size reduction

// TODO: Send claim check reference to Surgewave
// - Create producer with string key and string value
// - Serialize the ClaimCheckMessage
// - Send to topic "claim-checks"

// TODO: Consume the claim check reference
// - Create consumer
// - Deserialize the ClaimCheckMessage

// TODO: Fetch payload from external storage
// - Use storage.RetrieveAsync(claimCheck)
// - Verify content matches original

// TODO: Cleanup external storage
// - Use storage.DeleteAsync(claimCheck)

// TODO: Implement StorageType enum
// LocalFile, S3, AzureBlob

// TODO: Implement ClaimCheck record
// - Id: Guid
// - StorageType: StorageType
// - Location: string (URL/path to stored data)
// - Size: long
// - ContentType: string
// - Checksum: string
// - CreatedAt: DateTime
// - ExpiresAt: DateTime

// TODO: Implement ClaimCheckMessage record
// - ClaimCheck: ClaimCheck
// - Metadata: Dictionary<string, string>
// - Serialize() method
// - Deserialize(string) static method

// TODO: Implement IExternalStorage interface
// - StoreAsync(byte[] data, string contentType) -> ClaimCheck
// - RetrieveAsync(ClaimCheck claimCheck) -> byte[]
// - DeleteAsync(ClaimCheck claimCheck) -> Task

// TODO: Implement LocalFileStorage class
// - Constructor takes basePath and optional expiration
// - Implement IExternalStorage
// - Store files with GUID names
// - Compute and verify checksums

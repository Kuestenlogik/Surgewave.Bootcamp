# Lesson 19.4: Instructions

Follow these step-by-step instructions to learn protocol debugging techniques.

## Step 1: Create a Hex Dump Utility

First, build a utility that formats bytes as readable hex dumps:

```csharp
public static class HexDump
{
    public static string Format(byte[] data, int bytesPerLine = 16)
    {
        var sb = new StringBuilder();

        for (int offset = 0; offset < data.Length; offset += bytesPerLine)
        {
            // Offset column
            sb.Append($"{offset:X8}  ");

            // Hex bytes column
            for (int i = 0; i < bytesPerLine; i++)
            {
                if (offset + i < data.Length)
                    sb.Append($"{data[offset + i]:X2} ");
                else
                    sb.Append("   ");

                // Add extra space in middle
                if (i == 7)
                    sb.Append(" ");
            }

            sb.Append(" |");

            // ASCII column
            for (int i = 0; i < bytesPerLine && offset + i < data.Length; i++)
            {
                byte b = data[offset + i];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }

            sb.AppendLine("|");
        }

        return sb.ToString();
    }
}
```

## Step 2: Create an Error Code Dictionary

```csharp
public static class KafkaErrors
{
    private static readonly Dictionary<short, (string Name, string Description)> Errors = new()
    {
        { 0, ("NONE", "Success") },
        { -1, ("UNKNOWN_SERVER_ERROR", "An unexpected server error occurred") },
        { 1, ("OFFSET_OUT_OF_RANGE", "The requested offset is out of range") },
        { 2, ("CORRUPT_MESSAGE", "Message CRC check failed") },
        { 3, ("UNKNOWN_TOPIC_OR_PARTITION", "Topic or partition does not exist") },
        { 4, ("INVALID_FETCH_SIZE", "Invalid fetch size") },
        { 5, ("LEADER_NOT_AVAILABLE", "No leader for partition") },
        { 6, ("NOT_LEADER_OR_FOLLOWER", "Broker is not the partition leader") },
        { 7, ("REQUEST_TIMED_OUT", "Request timed out") },
        { 8, ("BROKER_NOT_AVAILABLE", "Broker not available") },
        { 9, ("REPLICA_NOT_AVAILABLE", "Replica not available") },
        { 10, ("MESSAGE_TOO_LARGE", "Message exceeds max size") },
        { 14, ("OFFSET_METADATA_TOO_LARGE", "Offset metadata too large") },
        { 15, ("NETWORK_EXCEPTION", "Network error") },
        { 16, ("COORDINATOR_LOAD_IN_PROGRESS", "Coordinator loading") },
        { 17, ("COORDINATOR_NOT_AVAILABLE", "Coordinator not available") },
        { 35, ("UNSUPPORTED_VERSION", "API version not supported") },
        { 36, ("TOPIC_ALREADY_EXISTS", "Topic already exists") },
        { 37, ("INVALID_PARTITIONS", "Invalid partition count") },
    };

    public static string Describe(short errorCode)
    {
        if (Errors.TryGetValue(errorCode, out var error))
            return $"{error.Name}: {error.Description}";
        return $"UNKNOWN_ERROR({errorCode})";
    }

    public static bool IsError(short errorCode) => errorCode != 0;
}
```

## Step 3: Build a Protocol Tracer

```csharp
public class ProtocolTracer
{
    private readonly NetworkStream _stream;
    private readonly bool _verbose;

    public ProtocolTracer(NetworkStream stream, bool verbose = true)
    {
        _stream = stream;
        _verbose = verbose;
    }

    public async Task<byte[]> SendRequestAsync(byte[] request, string description)
    {
        var timestamp = DateTime.Now;

        // Parse request header for logging
        var apiKey = BinaryPrimitives.ReadInt16BigEndian(request.AsSpan(4, 2));
        var apiVersion = BinaryPrimitives.ReadInt16BigEndian(request.AsSpan(6, 2));
        var correlationId = BinaryPrimitives.ReadInt32BigEndian(request.AsSpan(8, 4));

        Console.WriteLine($"\n[{timestamp:HH:mm:ss.fff}] -> REQUEST: {description}");
        Console.WriteLine($"  API: {GetApiName(apiKey)} ({apiKey}) v{apiVersion}");
        Console.WriteLine($"  Correlation ID: {correlationId}");
        Console.WriteLine($"  Size: {request.Length} bytes");

        if (_verbose)
        {
            Console.WriteLine("  Hex dump:");
            Console.WriteLine(Indent(HexDump.Format(request), "    "));
        }

        // Send request
        var sendStart = DateTime.Now;
        await _stream.WriteAsync(request);

        // Read response
        var lengthBuffer = new byte[4];
        await _stream.ReadExactlyAsync(lengthBuffer);
        var responseLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
        var response = new byte[responseLength];
        await _stream.ReadExactlyAsync(response);
        var latency = (DateTime.Now - sendStart).TotalMilliseconds;

        // Parse response
        var responseCorrelationId = BinaryPrimitives.ReadInt32BigEndian(response.AsSpan(0, 4));

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] <- RESPONSE");
        Console.WriteLine($"  Correlation ID: {responseCorrelationId}");
        Console.WriteLine($"  Size: {response.Length} bytes");
        Console.WriteLine($"  Latency: {latency:F1}ms");

        if (responseCorrelationId != correlationId)
        {
            Console.WriteLine($"  WARNING: Correlation ID mismatch! Expected {correlationId}");
        }

        if (_verbose)
        {
            Console.WriteLine("  Hex dump:");
            Console.WriteLine(Indent(HexDump.Format(response), "    "));
        }

        return response;
    }

    private static string Indent(string text, string prefix)
    {
        return string.Join("\n", text.Split('\n').Select(line => prefix + line));
    }

    private static string GetApiName(short apiKey) => apiKey switch
    {
        0 => "Produce",
        1 => "Fetch",
        2 => "ListOffsets",
        3 => "Metadata",
        // ... add more as needed
        _ => "Unknown"
    };
}
```

## Step 4: Analyze Protocol Messages

```csharp
public static class ProtocolAnalyzer
{
    public static void AnalyzeRequest(byte[] data)
    {
        Console.WriteLine("\n=== Request Analysis ===");

        // Check minimum size
        if (data.Length < 12)
        {
            Console.WriteLine("ERROR: Request too short (< 12 bytes)");
            return;
        }

        var length = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
        var apiKey = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(4, 2));
        var apiVersion = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(6, 2));
        var correlationId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(8, 4));

        Console.WriteLine($"Length: {length} (payload: {data.Length - 4})");

        if (length != data.Length - 4)
        {
            Console.WriteLine($"WARNING: Length mismatch! Header says {length}, actual {data.Length - 4}");
        }

        Console.WriteLine($"API Key: {apiKey} ({GetApiName(apiKey)})");

        if (apiKey < 0 || apiKey > 75)
        {
            Console.WriteLine($"WARNING: Unknown API key {apiKey}");
        }

        Console.WriteLine($"API Version: {apiVersion}");
        Console.WriteLine($"Correlation ID: {correlationId}");

        // Parse client ID
        if (data.Length >= 14)
        {
            var clientIdLength = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(12, 2));
            if (clientIdLength >= 0 && data.Length >= 14 + clientIdLength)
            {
                var clientId = Encoding.UTF8.GetString(data, 14, clientIdLength);
                Console.WriteLine($"Client ID: \"{clientId}\"");
            }
            else if (clientIdLength == -1)
            {
                Console.WriteLine("Client ID: null");
            }
        }
    }

    public static void AnalyzeResponse(byte[] data)
    {
        Console.WriteLine("\n=== Response Analysis ===");

        if (data.Length < 4)
        {
            Console.WriteLine("ERROR: Response too short");
            return;
        }

        var correlationId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
        Console.WriteLine($"Correlation ID: {correlationId}");
        Console.WriteLine($"Payload size: {data.Length - 4} bytes");

        // Try to find error codes
        if (data.Length >= 6)
        {
            var possibleError = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(4, 2));
            if (possibleError != 0 && possibleError < 100 && possibleError > -10)
            {
                Console.WriteLine($"Possible error code at offset 4: {possibleError}");
                Console.WriteLine($"  -> {KafkaErrors.Describe(possibleError)}");
            }
        }
    }
}
```

## Step 5: Debug a Broken Message

Practice fixing common protocol issues:

```csharp
public static void DebugExercise()
{
    Console.WriteLine("\n=== Debug Exercise ===\n");

    // Example 1: Wrong byte order
    Console.WriteLine("1. Wrong byte order:");
    var wrongOrder = new byte[] { 0x2e, 0x00, 0x00, 0x00 }; // Little-endian
    var correct = new byte[] { 0x00, 0x00, 0x00, 0x2e };   // Big-endian
    Console.WriteLine($"   Wrong:   {BitConverter.ToString(wrongOrder)} = {BitConverter.ToInt32(wrongOrder)}");
    Console.WriteLine($"   Correct: {BitConverter.ToString(correct)} = {BinaryPrimitives.ReadInt32BigEndian(correct)}");

    // Example 2: Missing length prefix
    Console.WriteLine("\n2. Missing length prefix:");
    Console.WriteLine("   Client sent request body without 4-byte length prefix");
    Console.WriteLine("   Broker interprets first 4 bytes as length, gets garbage");

    // Example 3: Truncated message
    Console.WriteLine("\n3. Truncated message:");
    Console.WriteLine("   Length field says 100 bytes, but only 50 sent");
    Console.WriteLine("   Broker blocks waiting for remaining 50 bytes -> timeout");
}
```

## Complete Solution

See `completed/Program.cs` for the full implementation demonstrating:

- Hex dump formatting
- Error code lookup
- Protocol tracing
- Message analysis
- Common debugging scenarios

## Sample Output

```
=== Surgewave Bootcamp: Lesson 19.4 - Protocol Debugging ===

--- Hex Dump Demo ---
00000000  00 00 00 2e 00 03 00 00  00 00 00 01 00 0c 6d 69  |..............mi|
00000010  6e 69 6d 61 6c 2d 63 6c  69 65 6e 74 ff ff ff ff  |nimal-client....|

--- Protocol Trace ---
[10:30:45.123] -> REQUEST: Metadata
  API: Metadata (3) v0
  Correlation ID: 1
  Size: 30 bytes

[10:30:45.156] <- RESPONSE
  Correlation ID: 1
  Size: 124 bytes
  Latency: 33.2ms

--- Error Analysis ---
Error 3: UNKNOWN_TOPIC_OR_PARTITION: Topic or partition does not exist
Error 35: UNSUPPORTED_VERSION: API version not supported

--- Debug Exercise ---
1. Wrong byte order:
   Wrong:   2E-00-00-00 = 46 (incorrect)
   Correct: 00-00-00-2E = 46 (correct)

Lesson complete!
```

## Knowledge Check

1. How do you identify the API key in a hex dump of a request?
2. What does a correlation ID mismatch indicate?
3. How can you tell if a message uses wrong byte order?
4. What are the symptoms of a truncated message?
5. Why is it important to log both request and response hex dumps?

---

**Next:** [Lesson 19.5: Protocol Extensions](../lesson-5/README.md)

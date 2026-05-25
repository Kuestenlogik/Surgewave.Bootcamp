# Knowledge Assessment: Lesson 23.4 - AI Agent Integration

## Multiple Choice

### 1. Where is the Claude Desktop configuration file located on Windows?
- [ ] A) C:\Users\<user>\claude_config.json
- [ ] B) %APPDATA%\Claude\claude_desktop_config.json
- [ ] C) %PROGRAMFILES%\Claude\config.json
- [ ] D) %USERPROFILE%\.claude\config.json

<details>
<summary>Answer</summary>
**B)** On Windows, Claude Desktop reads its MCP configuration from `%APPDATA%\Claude\claude_desktop_config.json`.
</details>

### 2. How does Claude Desktop communicate with MCP servers by default?
- [ ] A) HTTP REST API
- [ ] B) WebSocket
- [ ] C) stdio (standard input/output)
- [ ] D) gRPC

<details>
<summary>Answer</summary>
**C)** Claude Desktop launches MCP servers as subprocesses and communicates via stdio - sending JSON-RPC messages over stdin and receiving responses on stdout.
</details>

### 3. What should you use to pass sensitive credentials to an MCP server?
- [ ] A) Hardcode them in the server
- [ ] B) Store them in the config file "args" array
- [ ] C) Environment variables in the "env" object
- [ ] D) Command line arguments visible in process list

<details>
<summary>Answer</summary>
**C)** Environment variables in the "env" object of the MCP server config are the safest way. They're passed to the subprocess without being visible in config files or process listings.
</details>

### 4. What happens when an MCP server process terminates unexpectedly?
- [ ] A) Claude Desktop crashes
- [ ] B) The tools become unavailable and Claude reports an error
- [ ] C) Claude Desktop automatically restarts the server
- [ ] D) Nothing, Claude continues using cached tools

<details>
<summary>Answer</summary>
**B)** When the server terminates, Claude Desktop detects the closed connection and the tools become unavailable. The user will see an error if they try to use Surgewave tools.
</details>

### 5. Why is rate limiting important for MCP servers?
- [ ] A) To reduce memory usage
- [ ] B) To prevent AI from overwhelming Surgewave with requests
- [ ] C) To improve response time
- [ ] D) It's not important for MCP servers

<details>
<summary>Answer</summary>
**B)** Rate limiting prevents the AI from making too many requests in a short time, which could overwhelm the Surgewave cluster or cause excessive resource consumption.
</details>

## True/False

### 6. Claude Desktop can connect to MCP servers running on remote machines via HTTP.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - While stdio is the default for local servers, Claude Desktop also supports HTTP/SSE transport for connecting to remote MCP servers.
</details>

### 7. You must restart Claude Desktop after changing the MCP configuration file.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - Claude Desktop reads the configuration file at startup. Changes to `claude_desktop_config.json` require restarting the application to take effect.
</details>

## Code Challenge

### 8. What's wrong with this Claude Desktop configuration?

```json
{
    "mcpServers": {
        "surgewave": {
            "command": "SurgewaveMcpServer.exe",
            "args": ["--bootstrap", "kafka.prod.company.com:9092", "--password", "secret123"]
        }
    }
}
```

<details>
<summary>Answer</summary>
Several issues:

1. **Password in args**: The password is visible in the config file and process listings. Use environment variables instead.

2. **Relative path**: The command should be an absolute path or the exe must be in PATH.

Corrected version:
```json
{
    "mcpServers": {
        "surgewave": {
            "command": "C:\\Tools\\SurgewaveMcpServer\\SurgewaveMcpServer.exe",
            "args": [],
            "env": {
                "Surgewave_BOOTSTRAP_SERVERS": "kafka.prod.company.com:9092",
                "Surgewave_PASSWORD": "secret123"
            }
        }
    }
}
```

Even better: Use a secrets manager and pass only a reference to the secret.
</details>

### 9. Complete the stdio transport implementation:

```csharp
public async Task RunAsync()
{
    using var stdin = Console.OpenStandardInput();
    using var stdout = Console.OpenStandardOutput();
    using var reader = new StreamReader(_____);
    using var writer = new StreamWriter(_____) { AutoFlush = true };

    while (true)
    {
        var line = await reader._____();
        if (line == _____) break;  // EOF condition

        var response = await HandleMessageAsync(line);
        if (response != null)
        {
            await writer._____(JsonSerializer.Serialize(response));
        }
    }
}
```

<details>
<summary>Answer</summary>

```csharp
public async Task RunAsync()
{
    using var stdin = Console.OpenStandardInput();
    using var stdout = Console.OpenStandardOutput();
    using var reader = new StreamReader(stdin);
    using var writer = new StreamWriter(stdout) { AutoFlush = true };

    while (true)
    {
        var line = await reader.ReadLineAsync();
        if (line == null) break;  // EOF condition

        var response = await HandleMessageAsync(line);
        if (response != null)
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(response));
        }
    }
}
```

Key points:
- `stdin` is passed to StreamReader
- `stdout` is passed to StreamWriter (with AutoFlush!)
- `ReadLineAsync()` reads one JSON-RPC message per line
- `null` indicates EOF (Claude closed the connection)
- `WriteLineAsync` sends the response with a newline
</details>

## Score
- 9/9: Excellent!
- 7-8/9: Good understanding
- 5-6/9: Review the lesson
- <5/9: Re-do the lesson

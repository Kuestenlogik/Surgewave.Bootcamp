# Knowledge Assessment: Lesson 0.5 - Getting Support

Test your understanding of Surgewave support resources.

## Multiple Choice

### 1. Which is the best first step when you encounter a problem with Surgewave?

- [ ] A) Open a support ticket immediately
- [ ] B) Post on Discord asking for help
- [ ] C) Check the documentation and search existing issues
- [ ] D) Email the Surgewave team

<details>
<summary>Answer</summary>

**C)** Check the documentation and search existing issues.

Self-help resources often have your answer already. Check docs, search GitHub issues/discussions, and try troubleshooting commands before asking for help.
</details>

### 2. What command shows the health status of a Surgewave cluster?

- [ ] A) `surgewave status`
- [ ] B) `surgewave cluster health`
- [ ] C) `surgewave check`
- [ ] D) `surgewave diagnose`

<details>
<summary>Answer</summary>

**B)** `surgewave cluster health`

This command provides an overview of broker health, leader elections, and cluster status.
</details>

### 3. Which support channel provides real-time community help for free?

- [ ] A) Email support
- [ ] B) Phone support
- [ ] C) Discord server
- [ ] D) On-site consulting

<details>
<summary>Answer</summary>

**C)** Discord server

Discord provides real-time chat with the community. It's free but best-effort - community members are volunteers.
</details>

### 4. When asking for help, what information should you include?

- [ ] A) Just the error message
- [ ] B) Surgewave version, .NET version, OS, config, full error, reproduction steps
- [ ] C) Only the code that's failing
- [ ] D) A brief description of the problem

<details>
<summary>Answer</summary>

**B)** Surgewave version, .NET version, OS, config, full error, reproduction steps

Detailed information helps others understand and diagnose your issue quickly. Include all context that might be relevant.
</details>

## True/False

### 5. Community support on Discord has guaranteed response times.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>

**False** - Community support is best-effort. Community members are volunteers and may not always be available. For guaranteed response times, professional support plans are required.
</details>

### 6. You can enable debug logging to help troubleshoot issues.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>

**True** - You can enable detailed logging with `.WithLogLevel(LogLevel.Debug)` or the `Surgewave_LOG_LEVEL=Debug` environment variable.
</details>

### 7. Stack Overflow can be used for Surgewave questions.

- [ ] True
- [ ] False

<details>
<summary>Answer</summary>

**True** - Use the `surgewave-streaming` tag on Stack Overflow for technical questions. This is another free community resource.
</details>

## Matching

### 8. Match the support need with the appropriate resource:

| Need | Resource |
|------|----------|
| 1. Quick CLI help | A. Paid support plan |
| 2. Community discussion | B. `surgewave --help` |
| 3. Production SLA | C. GitHub Discussions |
| 4. Bug report | D. GitHub Issues |

<details>
<summary>Answer</summary>

1-B, 2-C, 3-A, 4-D

- Quick CLI help → `surgewave --help`
- Community discussion → GitHub Discussions
- Production SLA → Paid support plan
- Bug report → GitHub Issues
</details>

## Short Answer

### 9. What metrics should you check when troubleshooting high latency?

<details>
<summary>Answer</summary>

Key metrics for latency troubleshooting:
- `surgewave_producer_latency_p99` - Producer latency percentiles
- `surgewave_consumer_lag` - Consumer lag behind producer
- Batch settings and compression configuration
- Network round-trip times
- Broker resource utilization (CPU, memory, disk I/O)
</details>

## Score

- 9/9: Excellent! You know where to get help!
- 7-8/9: Good understanding
- 5-6/9: Review support resources
- <5/9: Re-read the lesson

# Lesson 0.5: Getting Support

> **Difficulty:** Beginner | **Duration:** 15 min | **Prerequisites:** None

## Overview

Learn how to get help with Surgewave, from free community resources to professional support options. Whether you're troubleshooting an issue or planning a production deployment, there are resources available.

## Learning Objectives

- Know where to find Surgewave documentation
- Use community channels for support
- Understand professional support options
- Learn self-help troubleshooting techniques

## Documentation Resources

### Official Documentation

| Resource | URL | Description |
|----------|-----|-------------|
| **Documentation** | `docs/` folder | Comprehensive API and feature docs |
| **CLI Guide** | `docs/CLI_GUIDE.md` | All CLI commands |
| **Troubleshooting** | `docs/TROUBLESHOOTING.md` | Common issues and solutions |
| **FAQ** | `docs/FAQ.md` | Frequently asked questions |

### In-Code Help

```bash
# CLI help
surgewave --help
surgewave <command> --help

# Example
surgewave topic --help
surgewave topic create --help
```

## Community Support (Free)

### Discord Community

Join the Surgewave Discord server for real-time community support:

```
https://discord.gg/surgewave-community
```
*(Placeholder - actual link TBD)*

**Channels:**
- `#general` - General discussion
- `#help` - Ask questions, get help
- `#showcase` - Share your Surgewave projects
- `#announcements` - Release news, updates

**Discord Guidelines:**
- Search before asking
- Provide context (version, config, error messages)
- Be patient - community members are volunteers
- Help others when you can

### GitHub Discussions

For longer-form questions and discussions:

```
https://github.com/Kuestenlogik/Surgewave/discussions
```

**Categories:**
- **Q&A** - Ask technical questions
- **Ideas** - Propose new features
- **Show and Tell** - Share projects
- **General** - Open discussion

### Stack Overflow

Use the `surgewave-streaming` tag:

```
https://stackoverflow.com/questions/tagged/surgewave-streaming
```

**Good Question Template:**
```markdown
**What I'm trying to do:**
[Clear description of goal]

**What I've tried:**
[Code samples, configurations]

**What's happening:**
[Error messages, unexpected behavior]

**Environment:**
- Surgewave version: x.y.z
- .NET version: 10.0
- OS: Windows/Linux
```

## Self-Help Troubleshooting

### Diagnostic Commands

```bash
# Check broker health
surgewave cluster health

# View broker logs
surgewave logs --broker 1

# Check topic status
surgewave topic describe my-topic

# Monitor consumer lag
surgewave consumer-group describe my-group

# Test connectivity
surgewave ping localhost:9092
```

### Common Issues Checklist

| Symptom | Check |
|---------|-------|
| Connection refused | Is broker running? Correct port? |
| Consumer not receiving | Subscribed? Correct group? Offset position? |
| Producer timeout | Network issues? Broker overloaded? |
| High latency | Check batch settings, compression |
| Out of memory | Increase heap, check retention |

### Logging

Enable detailed logging for troubleshooting:

```csharp
// In your application
var builder = SurgewaveRuntime.CreateBuilder()
    .WithLogLevel(LogLevel.Debug);

// Or via environment variable
// Surgewave_LOG_LEVEL=Debug
```

### Metrics

Check built-in metrics for insights:

```bash
# Prometheus metrics endpoint
curl http://localhost:9090/metrics

# Key metrics to watch:
# - surgewave_broker_messages_in_total
# - surgewave_broker_messages_out_total
# - surgewave_consumer_lag
# - surgewave_producer_latency_p99
```

## Professional Support (Paid)

### Support Plans

*The following support plans are planned offerings - details subject to change.*

| Plan | Response Time | Channels | Best For |
|------|---------------|----------|----------|
| **Community** | Best effort | Discord, GitHub | Learning, side projects |
| **Developer** | 48 hours | Email, tickets | Small teams, non-critical |
| **Business** | 24 hours | Email, tickets, chat | Production workloads |
| **Enterprise** | 4 hours | Dedicated support, phone | Mission-critical systems |

### Support Plan Features

#### Developer Plan
- Email support (business hours)
- Access to knowledge base
- Quarterly check-in calls

#### Business Plan
- Priority email support
- Live chat support
- Architecture review (annual)
- Training credits

#### Enterprise Plan
- 24/7 support with SLA
- Dedicated support engineer
- On-site training available
- Custom development options
- Early access to features

### Consulting Services

For specialized needs:

- **Architecture Review** - Expert review of your Surgewave deployment
- **Performance Tuning** - Optimization for your workload
- **Migration Assistance** - Help migrating from Kafka
- **Custom Development** - Tailored features and connectors
- **Training** - On-site or remote team training

### Contact Sales

For pricing and custom requirements:

```
Email: sales@kuestenlogik.de
Web: https://kuestenlogik.de/surgewave/support
```
*(Placeholder - actual contact TBD)*

## Training Resources

### This Bootcamp

You're already here! Complete all units for comprehensive Surgewave knowledge.

### Video Tutorials

*(Coming soon)*

Video tutorials covering:
- Getting started
- Common use cases
- Best practices
- Advanced topics

### Webinars

Live and recorded webinars:

```
https://kuestenlogik.de/surgewave/webinars
```
*(Placeholder - TBD)*

## Getting the Most from Support

### Before Asking for Help

1. **Check documentation** - Answer might be there
2. **Search issues/discussions** - Someone may have asked
3. **Try minimal reproduction** - Isolate the problem
4. **Gather information** - Version, config, logs, error messages

### Asking Good Questions

**Include:**
- Surgewave version (`surgewave --version`)
- .NET version (`dotnet --version`)
- Operating system
- Relevant configuration
- Complete error messages
- Minimal code to reproduce

**Example Good Question:**
> I'm using Surgewave 1.2.0 on .NET 10 / Ubuntu 22.04.
>
> When I try to consume from a compacted topic with 100+ partitions, the consumer crashes with OutOfMemoryException after ~5 minutes.
>
> Config: `MaxPollRecords=500`, `FetchMaxBytes=50MB`
>
> Full stack trace: [attached]
>
> Minimal reproduction: [gist link]

## Summary

| Need | Resource |
|------|----------|
| Quick answer | Documentation, CLI help |
| Community help | Discord, GitHub Discussions |
| Bug report | GitHub Issues |
| Production support | Paid support plans |
| Training | Bootcamp, videos, webinars |
| Custom work | Consulting services |

## Key Takeaways

- Multiple free resources available: docs, Discord, GitHub
- Provide detailed information when asking for help
- Professional support available for production needs
- Self-help tools: CLI diagnostics, logs, metrics
- Good questions get faster, better answers

---

**Congratulations!** You've completed Unit 0: Introduction. You're now ready to start building with Surgewave!

**Next:** [Unit 1: Surgewave Fundamentals](../../unit-1/README.md)

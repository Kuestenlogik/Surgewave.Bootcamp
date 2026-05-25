# Unit 27 Project: Hybrid Cloud Demo

## Overview

Simulate a multi-region hybrid cloud deployment with intelligent data routing based on geographic location, data sovereignty compliance, and automatic cloud bursting. The demo brings together all concepts from Unit 27: multi-region clusters, cross-cloud replication, geo-routing, and hybrid on-premise/cloud patterns.

## Objectives

- Deploy simulated Surgewave clusters across multiple regions
- Implement geographic data routing with latency optimization
- Enforce data sovereignty rules (GDPR, CCPA compliance)
- Build store-and-forward resilience for network partitions
- Demonstrate cloud bursting under simulated load

## Requirements

### 1. Multi-Region Setup
- At least 3 regional clusters (US, EU, APAC)
- 1 on-premise cluster for sensitive data
- Each cluster with simulated latency characteristics

### 2. Geographic Routing
- Route producers to nearest region
- Latency-based cluster selection
- Fallback routing when a region is unavailable

### 3. Data Sovereignty
- Classify topics by sensitivity (Public, Internal, Confidential, Restricted)
- EU personal data stays in EU region (GDPR)
- Payment data stays on-premise (PCI DSS)
- Audit log for all cross-region data transfers

### 4. Resilience
- Store-and-forward during network partitions
- Automatic failover to secondary region
- Cloud bursting when primary capacity exceeded

### 5. Reporting
- Region-by-region message distribution
- Data sovereignty compliance report
- Latency comparison across regions
- Cloud burst activation events

## Difficulty

Advanced | Estimated Duration: 90 min

## Getting Started

1. Open `start/Program.cs`
2. Set up multi-region clusters
3. Implement geographic routing
4. Add data sovereignty controls
5. Build resilience features
6. Generate the comprehensive report
7. Compare with `completed/Program.cs`

---

**Next Unit:** [Unit 2](../../unit-2/README.md) (if available)

# Lesson 2.5: Monitoring & Alerting

The Monitoring section provides real-time metrics dashboards, configurable alert rules, and a performance advisor.

## Learning Objectives

- Navigate the metrics dashboard with auto-refresh
- Configure alert rules for consumer lag, throughput, and latency
- Set up notification channels (Email, Slack, Webhook, PagerDuty)
- Use the performance advisor for optimization recommendations
- Monitor connector health and audit logs

## Metrics Dashboard

The cluster metrics dashboard displays throughput, latency percentiles, and active connection counts with configurable auto-refresh intervals.

## Alert Rules

Surgewave supports 7 built-in alert rule types:

| Alert Type | Trigger | Example |
|------------|---------|---------|
| Consumer Lag | Lag exceeds threshold | Lag > 10,000 for 5 minutes |
| Throughput Drop | Messages/s drops below minimum | Throughput < 100/s |
| Latency Spike | P99 latency exceeds threshold | P99 > 500ms |
| Broker Down | Broker becomes unreachable | Broker-2 offline |
| Disk Usage | Storage exceeds percentage | Disk > 85% |
| Error Rate | Errors per minute exceed threshold | Errors > 50/min |
| Pipeline Failure | Pipeline enters error state | Pipeline stopped unexpectedly |

## Notification Channels

| Channel | Configuration |
|---------|---------------|
| Email | SMTP server, recipients |
| Slack | Webhook URL, channel |
| Webhook | Custom HTTP endpoint |
| PagerDuty | Integration key |

## Performance Advisor

The performance advisor analyzes cluster behavior and provides actionable recommendations with severity levels (High, Medium, Low) for partition imbalance, over-provisioned consumers, and retention optimization.

## Connector Health Dashboard

Monitor connector status with columns for name, type, state, tasks, throughput, errors, and auto-restart toggle.

## Audit Log Viewer

All administrative actions are recorded in the audit log with filterable views by action type, user, and date range.

## Exercise

1. Navigate to Monitoring > Alerts
2. Create a new alert rule for Consumer Lag > 1,000 on `order-processor`
3. Set a 2-minute duration and Webhook notification to `http://localhost:9999/alerts`
4. Produce messages faster than the consumer can process them
5. Verify the alert fires and the webhook receives the notification

---

[Back to Unit 2](../README.md)

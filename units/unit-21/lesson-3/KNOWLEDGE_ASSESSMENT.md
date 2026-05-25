# Knowledge Assessment: Lesson 4.3 - Monitoring

## Multiple Choice

### 1. What monitoring stack does Surgewave integrate with for metrics visualization?
- [ ] A) ELK Stack (Elasticsearch, Logstash, Kibana)
- [ ] B) Prometheus and Grafana
- [ ] C) Datadog only
- [ ] D) CloudWatch only

<details>
<summary>Answer</summary>
**B)** Surgewave exports metrics to Prometheus (for storage) which can then be visualized in Grafana dashboards. This is the standard monitoring stack: Surgewave (metrics) -> Prometheus (storage) -> Grafana (visualization).
</details>

### 2. Which metric would you use to detect a consumer falling behind?
- [ ] A) `surgewave_broker_messages_in_total`
- [ ] B) `surgewave_producer_latency_ms`
- [ ] C) `surgewave_consumer_lag`
- [ ] D) `surgewave_broker_partitions_count`

<details>
<summary>Answer</summary>
**C)** `surgewave_consumer_lag` measures how far behind a consumer is from the latest messages. High lag indicates the consumer cannot keep up with the producer rate.
</details>

### 3. What does `surgewave_producer_errors_total` measure?
- [ ] A) Network latency
- [ ] B) Number of send errors
- [ ] C) Message size errors
- [ ] D) Consumer connection errors

<details>
<summary>Answer</summary>
**B)** `surgewave_producer_errors_total` counts the number of send errors from producers. This is useful for alerting on producer issues.
</details>

## True/False

### 4. Without monitoring, you can still effectively plan cluster capacity.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**False** - Without monitoring, you are blind to issues and unclear about capacity. Monitoring provides the data needed for proactive operations and capacity planning.
</details>

### 5. The metric `surgewave_broker_bytes_in_total` tracks the total bytes received by the broker.
- [ ] True
- [ ] False

<details>
<summary>Answer</summary>
**True** - `surgewave_broker_bytes_in_total` is a broker metric that tracks the total bytes received. This is useful for understanding ingestion throughput.
</details>

## Code Challenge

### 6. You need to set up alerting for the following scenarios. Which metrics would you monitor?

a) Producer is experiencing high latency
b) Consumer is not processing messages fast enough
c) Broker is receiving an unusually high number of API requests

<details>
<summary>Answer</summary>
The metrics to monitor are:

a) **Producer high latency**: `surgewave_producer_latency_ms`
   - Alert when P99 latency exceeds threshold (e.g., > 100ms)

b) **Consumer processing slowly**: `surgewave_consumer_lag`
   - Alert when lag exceeds threshold (e.g., > 10000 messages)
   - Also monitor `surgewave_consumer_messages_consumed_total` rate

c) **High API request volume**: `surgewave_broker_requests_total`
   - Alert on rate increase (e.g., > 10000 req/sec)
   - Consider rate of change alerting for sudden spikes
</details>

### 7. What is the benefit of monitoring over reactive troubleshooting?

<details>
<summary>Answer</summary>
Monitoring provides several benefits over reactive troubleshooting:

| Without Monitoring | With Monitoring |
|-------------------|-----------------|
| Blind to issues | Early warning before outages |
| Reactive fixes after problems occur | Proactive operations, fix before impact |
| Unclear capacity | Data-driven capacity planning |
| Difficult root cause analysis | Historical data for troubleshooting |

Key benefits:
1. **Early warning**: Detect issues before they impact users
2. **Proactive operations**: Scale before hitting limits
3. **Capacity planning**: Make data-driven sizing decisions
4. **Faster incident response**: Historical metrics aid root cause analysis
</details>

## Score
- 7/7: Excellent! You understand monitoring fundamentals
- 5-6/7: Good understanding
- 4/7: Review the lesson
- <4/7: Re-do the lesson

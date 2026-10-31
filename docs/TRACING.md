# Distributed Tracing Runbook - ShopMicro

This document describes how distributed tracing operates across **ShopMicro**, tracing requests end-to-end from the YARP API Gateway through to asynchronous worker background processing.

---

## 1. End-to-End Tracing Flow

```
                      +-----------------------------+
                      |       Client / Browser      |
                      +--------------+--------------+
                                     |
               HTTP POST /orders     | (Optional W3C 'traceparent' header)
                                     v
                 [Span 1: YARP Gateway (Port: 5000)]
                 TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
                                     |
           Proxies to Order API      | Injects 'traceparent'
                                     v
               [Span 2: Order.Api POST /orders (Port: 5002)]
                                     |
               HTTP GET /products    | Injects 'traceparent' via HttpClient
                                     v
          [Span 3: Catalog.Api GET /products/{id} (Port: 5001)]
          [Span 4: Npgsql PostgreSQL Query execution]
                                     |
           Returns product data      |
                                     v
             [Span 5: SQL Server Order + Outbox Commit]
                                     |
           Publishes OrderCreated    | Injects TraceId into MassTransit message headers
                                     v
                         [RabbitMQ Message Broker]
                                     |
           Delivers OrderCreated     |
                                     v
     [Span 6: Notification.Worker Consumer (OrderCreatedConsumer)]
     - Checks Idempotency Store (InMemory)
     - Dispatches notification log with correlated TraceId
                                     |
                                     v
              +---------------------------------------------+
              |           Seq Centralized UI (5341)         |
              | Correlated flame-graph under single TraceId |
              +---------------------------------------------+
```

---

## 2. W3C TraceContext Standard

Distributed trace context propagation across HTTP boundaries and message brokers follows the **W3C TraceContext specification**:

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
              |  |                                |                |
        Version  32-hex Trace ID                  16-hex Span ID   Trace Flags (01 = Sampled)
```

1. **Gateway (`Yarp.ReverseProxy`):**
   - Receives the incoming HTTP request.
   - Generates or preserves `traceparent`.
   - Propagates `traceparent` downstream to `Order.Api`.

2. **Order API (`Order.Api`):**
   - Extracts `traceparent` on incoming HTTP request.
   - `AddHttpClientInstrumentation()` automatically injects `traceparent` into outgoing calls to `Catalog.Api`.
   - `AddSource("MassTransit")` attaches the trace context to outgoing RabbitMQ AMQP message headers.

3. **Catalog API (`Catalog.Api`):**
   - Extracts incoming `traceparent` from `Order.Api`.
   - Database operations executed through `Npgsql` emit child spans under the exact same `TraceId`.

4. **Notification Worker (`Notification.Worker`):**
   - MassTransit consumer automatically extracts the trace context from RabbitMQ message headers.
   - Consumer execution creates a child span linked to the originating `TraceId`.

---

## 3. How to Execute & Verify the Trace

1. Ensure the container infrastructure is running:
   ```bash
   docker compose up -d
   ```
2. Start the services locally or via IDE:
   - `Gateway` (Port 5000)
   - `Catalog.Api` (Port 5001)
   - `Order.Api` (Port 5002)
   - `Notification.Worker`
3. Execute the requests in [ShopMicro.http](file:///d:/CICD/ShopMicro.http):
   - Step 1: Verify Gateway routes.
   - Step 2: Create a product in Catalog.
   - Step 3: Post an order with `traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01`.
4. Open the **Seq Web UI** at [http://localhost:5341](http://localhost:5341):
   - Filter by:
     ```sql
     @TraceId = '4bf92f3577b34da6a3ce929d0e0e4736'
     ```
   - Notice that log entries and telemetry spans from all 4 components (`Gateway`, `Order.Api`, `Catalog.Api`, and `Notification.Worker`) are unified under the exact same Trace ID.

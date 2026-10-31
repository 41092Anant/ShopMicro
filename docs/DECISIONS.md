# Architectural Decision Records (ADR)

## ADR-001: Target Platform
* **Decision:** Use **.NET 10 LTS**.
* **Reasoning:** Keeps SDK, target framework, CI runners, and Docker base images aligned on a long-term supported version.

## ADR-002: API Style
* **Decision:** Use **ASP.NET Core Controllers** with `[ApiController]`.
* **Reasoning:** Traditional, well-structured folder organization (`Controllers/`, `Services/`, `Models/`, `Data/`) that makes boundaries and responsibility clear during learning.

## ADR-003: Database-per-Service
* **Decision:** Each microservice has an independent database and DbContext.
  * `Catalog.Api` -> PostgreSQL
  * `Order.Api` -> SQL Server
* **Reasoning:** Enforces true microservices boundary. Prevents accidental joins or shared database locks.

## ADR-004: Local Port Allocation
To avoid port conflicts during local multi-service development:
* **Gateway (YARP):** `http://localhost:5000`
* **Catalog.Api:** `http://localhost:5001`
* **Order.Api:** `http://localhost:5002`
* **PostgreSQL:** `localhost:5432`
* **SQL Server:** `localhost:1433`
* **RabbitMQ:** `localhost:5672` (Broker) / `localhost:15672` (Management UI)
* **Redis:** `localhost:6379`
* **Seq (Centralized Logging):** `http://localhost:5341` (UI & ingestion)

## ADR-005: Error Response Standard
* **Decision:** Standardize on **RFC 7807 `ProblemDetails`**.
* **Reasoning:** Provides a uniform error structure across all microservices so API consumers (and other services) can parse errors predictably.

## ADR-006: Asynchronous Messaging & Event Contracts
* **Decision:** Use **MassTransit** over **RabbitMQ** with isolated C# records in a shared `Contracts` class library.
* **Reasoning:** Provides a resilient, decoupled transport abstraction without leaking internal EF Core database entities across microservice boundaries.

## ADR-007: Idempotent Consumer Pattern
* **Decision:** Message consumers track processed message/entity keys via an `IProcessedMessageStore` before executing side effects.
* **Reasoning:** RabbitMQ guarantees at-least-once message delivery. Network retransmissions or consumer retries can cause duplicate deliveries. Deduplicating on unique keys (e.g. `order-{OrderId}`) guarantees side effects (like sending emails or triggering fulfillment) execute strictly once.

## ADR-008: Transactional Outbox Pattern
* **Decision:** Use MassTransit's Entity Framework Core Outbox in `Order.Api` (`AddEntityFrameworkOutbox<OrderDbContext>`) with `UseSqlServer()` and `UseBusOutbox()`.
* **Reasoning:** Solves the dual-write problem across distributed databases and message brokers. Saving the order and staging the outgoing event in SQL Server outbox tables within the exact same database transaction guarantees that events are only published if and only if the database transaction succeeds.

## ADR-009: Structured Logging with Serilog
* **Decision:** Standardize on **Serilog** across all microservices and workers, enriched with contextual properties (`Application`, `traceId`, LogContext).
* **Reasoning:** Replaces plain string console logs with queryable, structured JSON key-value properties. Enables centralized log ingestion (e.g. Seq) and distributed log correlation without parsing fragile unstructured log strings.




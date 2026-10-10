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

## ADR-005: Error Response Standard
* **Decision:** Standardize on **RFC 7807 `ProblemDetails`**.
* **Reasoning:** Provides a uniform error structure across all microservices so API consumers (and other services) can parse errors predictably.

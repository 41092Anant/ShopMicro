# ShopMicro: Step-by-Step Learning Roadmap & Review Policy

This document defines our step-by-step learning methodology, the **Task Review Agent** policy, and the comprehensive atomic task breakdown for building the **ShopMicro** microservices system.

---

## Part 1: The Learning Methodology & Review Policy

### 1. The Core Rule: One Atomic Task at a Time
* We do **not** jump ahead or bundle multiple tasks into a single turn.
* Every task represents one small, understandable change (e.g., creating a blank project, adding one entity, configuring one port).
* Work pauses after each task until you review and confirm your understanding.

### 2. The Task Review Agent Protocol
After completing each task, the mentor acts as a **Task Review Agent** and delivers a structured review report containing:
1. **Task Completed**: What specific action was executed.
2. **The "Why" & Concept Explanation**: Plain-English explanation of why this step is necessary in microservices.
3. **Policy & Constraint Audit**:
   * Target .NET 10 LTS verified?
   * No unnecessary layers or Clean Architecture bloat introduced?
   * Database-per-service isolation respected?
   * Solution builds with 0 errors and 0 warnings?
4. **How to Verify in Your Editor**: Exact files to look at or commands to run.
5. **Awaiting Your Sign-Off**: The mentor stops and waits for your confirmation before touching the next task.

---

## Part 2: Comprehensive Micro-Task Breakdown

### Phase 0: Environment, Decisions & Foundation
- [x] **Task 0.1**: Initialize Git repository and standard .NET `.gitignore`.
- [x] **Task 0.2**: Create root solution file `ShopMicro.sln`.
- [x] **Task 0.3**: Define team coding rules in `AGENTS.md`.
- [x] **Task 0.4**: Record initial Architectural Decision Records in `docs/DECISIONS.md`.
- [x] **Task 0.5**: Create project overview and architecture diagram in `README.md`.

---

### Phase 1: Catalog API & PostgreSQL
#### Milestone 1A: Project Scaffolding
- [x] **Task 1.1**: Scaffold a blank ASP.NET Core Web API project (`Catalog.Api`) with Controllers and add it to `ShopMicro.sln`.
- [x] **Task 1.2**: Inspect default files and remove template boilerplate (`WeatherForecast.cs` and `WeatherForecastController.cs`).
- [x] **Task 1.3**: Configure local HTTP port `5001` in `src/Catalog.Api/Properties/launchSettings.json`.

#### Milestone 1B: EF Core & Database Model
- [x] **Task 1.4**: Add NuGet packages: `Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.EntityFrameworkCore.Design`.
- [x] **Task 1.5**: Create the internal database entity `src/Catalog.Api/Models/Product.cs` (`Id`, `Name`, `Description`, `Price`, `IsActive`, `CreatedUtc`).
- [x] **Task 1.6**: Create `src/Catalog.Api/Data/CatalogDbContext.cs` and configure property constraints via Fluent API.
- [x] **Task 1.7**: Add the PostgreSQL connection string to `appsettings.json` and register `CatalogDbContext` in `Program.cs`.
- [x] **Task 1.8**: Generate the initial EF Core migration using `dotnet ef migrations add InitialCatalogCreate`.

#### Milestone 1C: API Contracts (DTOs) & Service Layer
- [x] **Task 1.9**: Create public request DTO `src/Catalog.Api/Contracts/CreateProductRequest.cs` with validation attributes.
- [x] **Task 1.10**: Create public request DTO `src/Catalog.Api/Contracts/UpdateProductRequest.cs` with validation attributes.
- [x] **Task 1.11**: Create public response DTO `src/Catalog.Api/Contracts/ProductResponse.cs`.
- [x] **Task 1.12**: Define interface `src/Catalog.Api/Services/IProductService.cs` with async operations and `CancellationToken`.
- [x] **Task 1.13**: Implement `src/Catalog.Api/Services/ProductService.cs` and register it in `Program.cs`.

#### Milestone 1D: Controllers & Error Handling
- [x] **Task 1.14**: Create `src/Catalog.Api/Controllers/ProductsController.cs` with `GET /products` (list all products).
- [x] **Task 1.15**: Add `GET /products/{id}` with `200 OK` and `404 ProblemDetails` responses.
- [x] **Task 1.16**: Add `POST /products` with `201 CreatedAtAction` and validation error handling.
- [ ] **Task 1.17**: Add `PUT /products/{id}` and `DELETE /products/{id}` endpoints.
- [ ] **Task 1.18**: Configure standardized RFC 7807 `ProblemDetails` and OpenAPI documentation in `Program.cs`.
- [ ] **Task 1.19**: Create `src/Catalog.Api/Catalog.Api.http` to test all endpoints.

---

### Phase 2: Unit Testing & CI Pipeline
- [ ] **Task 2.1**: Create xUnit test project `tests/Catalog.Tests` and add it to `ShopMicro.sln`.
- [ ] **Task 2.2**: Add project reference from `Catalog.Tests` to `Catalog.Api`.
- [ ] **Task 2.3**: Write unit tests for product creation validation (e.g., negative price rejected).
- [ ] **Task 2.4**: Write unit tests for get product by ID (found vs. not found).
- [ ] **Task 2.5**: Write unit tests for product updates and soft/hard deletes.
- [ ] **Task 2.6**: Run tests locally and verify all pass.
- [ ] **Task 2.7**: Create GitHub Actions CI workflow file `.github/workflows/ci.yml`.
- [ ] **Task 2.8**: Demonstrate a deliberately failing test to verify CI error detection.

---

### Phase 3: Docker & Local Compose
- [ ] **Task 3.1**: Create multi-stage `Dockerfile` for `Catalog.Api`.
- [ ] **Task 3.2**: Create `.dockerignore` file.
- [ ] **Task 3.3**: Create root `docker-compose.yml` with `catalog-db` (PostgreSQL) service and persistent volume.
- [ ] **Task 3.4**: Add `catalog-api` service to `docker-compose.yml` and configure internal network DNS.
- [ ] **Task 3.5**: Configure PostgreSQL health check and container startup dependencies.
- [ ] **Task 3.6**: Create `.env.example` with non-production credentials.
- [ ] **Task 3.7**: Verify container startup and data persistence across restarts.

---

### Phase 4: Order API & Synchronous Inter-Service Communication
- [ ] **Task 4.1**: Create `Order.Api` Web API project and add to `ShopMicro.sln`.
- [ ] **Task 4.2**: Configure local port `5002` in `launchSettings.json`.
- [ ] **Task 4.3**: Add EF Core SQL Server packages to `Order.Api`.
- [ ] **Task 4.4**: Create `Order` and `OrderItem` models and `OrderDbContext`.
- [ ] **Task 4.5**: Add SQL Server service to `docker-compose.yml`.
- [ ] **Task 4.6**: Create DTO contract for Catalog product lookup (no shared entities!).
- [ ] **Task 4.7**: Register typed `HttpClient` in `Order.Api` for calling `Catalog.Api`.
- [ ] **Task 4.8**: Configure resilience handler (timeouts, bounded retries).
- [ ] **Task 4.9**: Implement `POST /orders` (validates products with Catalog API, calculates totals, saves order).
- [ ] **Task 4.10**: Implement `GET /orders/{id}`.
- [ ] **Task 4.11**: Handle downstream Catalog API downtime gracefully with RFC 7807 error responses.

---

### Phase 5: YARP Gateway & Redis Caching
- [ ] **Task 5.1**: Create `Gateway` ASP.NET Core project on port `5000`.
- [ ] **Task 5.2**: Install and configure `Yarp.ReverseProxy`.
- [ ] **Task 5.3**: Configure routes for `/catalog/{**catch-all}` and `/orders/{**catch-all}`.
- [ ] **Task 5.4**: Add Redis service to `docker-compose.yml`.
- [ ] **Task 5.5**: Add `Microsoft.Extensions.Caching.StackExchangeRedis` to `Catalog.Api`.
- [ ] **Task 5.6**: Implement Cache-Aside pattern for `GET /products/{id}` with short TTL.
- [ ] **Task 5.7**: Implement cache invalidation when products are created, updated, or deleted.

---

### Phase 6: RabbitMQ, Asynchronous Events & Notification Worker
- [ ] **Task 6.1**: Add RabbitMQ service with Management UI (port 15672) to `docker-compose.yml`.
- [ ] **Task 6.2**: Define shared event contract `OrderCreatedEvent` in an isolated contracts package/project.
- [ ] **Task 6.3**: Add MassTransit to `Order.Api` for event publishing.
- [ ] **Task 6.4**: Create `Notification.Worker` worker service project.
- [ ] **Task 6.5**: Add MassTransit consumer in `Notification.Worker` for `OrderCreatedEvent`.
- [ ] **Task 6.6**: Implement idempotent message processing to handle duplicate delivery safely.
- [ ] **Task 6.7**: Implement EF Core Outbox pattern in `Order.Api` to guarantee atomic database write + message publish.

---

### Phase 7: Observability (Logs, Traces, Metrics)
- [ ] **Task 7.1**: Add Serilog structured logging across all services.
- [ ] **Task 7.2**: Add Seq logging container to `docker-compose.yml`.
- [ ] **Task 7.3**: Configure OpenTelemetry tracing in Gateway, Catalog API, Order API, and Worker.
- [ ] **Task 7.4**: Trace an end-to-end user request through Gateway -> Order API -> Catalog API -> RabbitMQ -> Worker.
- [ ] **Task 7.5**: Add health check endpoints (`/health/live`, `/health/ready`) to all services.

---

### Phase 8: Azure Cloud Deployment
- [ ] **Task 8.1**: Plan Azure resources, region, and cost budget alerts.
- [ ] **Task 8.2**: Provision Azure Resource Group with cost controls.
- [ ] **Task 8.3**: Deploy Azure Container Apps environment with private ingress for backend APIs.
- [ ] **Task 8.4**: Provision Azure Database for PostgreSQL and Azure SQL Database.
- [ ] **Task 8.5**: Set up Azure Key Vault with Managed Identity for zero-credential secrets management.
- [ ] **Task 8.6**: Run end-to-end cloud smoke test.

---

### Phase 9: Security, Rate Limiting & Portfolio Polish
- [ ] **Task 9.1**: Add API rate limiting at Gateway.
- [ ] **Task 9.2**: Add authentication & authorization checks.
- [ ] **Task 9.3**: Finalize comprehensive documentation, architecture diagrams, and runbook.

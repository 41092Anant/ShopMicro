# ShopMicro Learning Task Roadmap

This document breaks down our microservices learning into atomic, single-step tasks. We work on **one task at a time**, review it thoroughly against our project rules, and only proceed when you are confident.

---

## Phase 0: Environment & Foundation
- [x] **Task 0.1**: Initialize Git repository and standard .NET `.gitignore`.
- [x] **Task 0.2**: Create root solution `ShopMicro.sln`.
- [x] **Task 0.3**: Define project coding rules in `AGENTS.md`.
- [x] **Task 0.4**: Record architectural decisions (ADRs) in `docs/DECISIONS.md`.
- [x] **Task 0.5**: Create project overview in `README.md`.

---

## Phase 1: Catalog API & PostgreSQL (Atomic Steps)

### Milestone A: Project Scaffolding
- [ ] **Task 1.1**: Scaffold a clean ASP.NET Core Web API project (`Catalog.Api`) with Controllers and add it to `ShopMicro.sln`.
- [ ] **Task 1.2**: Review generated files and remove default boilerplate (`WeatherForecast.cs`, `WeatherForecastController.cs`).
- [ ] **Task 1.3**: Configure local port `5001` in `Properties/launchSettings.json`.

### Milestone B: Database & EF Core Setup
- [ ] **Task 1.4**: Install EF Core PostgreSQL packages (`Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`).
- [ ] **Task 1.5**: Create the internal database entity `Models/Product.cs` (Id, Name, Description, Price, IsActive, CreatedUtc).
- [ ] **Task 1.6**: Create `Data/CatalogDbContext.cs` and configure entity constraints with EF Core Fluent API.
- [ ] **Task 1.7**: Add connection string to `appsettings.json` and register `CatalogDbContext` in `Program.cs`.
- [ ] **Task 1.8**: Generate the initial database migration using `dotnet ef migrations add InitialCatalogCreate`.

### Milestone C: API Contracts & Business Logic
- [ ] **Task 1.9**: Create public request and response DTOs (`Contracts/CreateProductRequest.cs`, `Contracts/UpdateProductRequest.cs`, `Contracts/ProductResponse.cs`) with input validation.
- [ ] **Task 1.10**: Create service interface `Services/IProductService.cs` and implementation `Services/ProductService.cs` with async methods and `CancellationToken`.
- [ ] **Task 1.11**: Register `IProductService` in `Program.cs`.

### Milestone D: Controller & Error Handling
- [ ] **Task 1.12**: Create `Controllers/ProductsController.cs` with `GET /products` (list all products).
- [ ] **Task 1.13**: Add `GET /products/{id}` with 200 OK and 404 ProblemDetails.
- [ ] **Task 1.14**: Add `POST /products` with 201 CreatedAtAction and 400 validation response.
- [ ] **Task 1.15**: Add `PUT /products/{id}` and `DELETE /products/{id}`.
- [ ] **Task 1.16**: Configure centralized error handling (`ProblemDetails`) and OpenAPI documentation in `Program.cs`.
- [ ] **Task 1.17**: Create `Catalog.Api.http` file to manually execute and verify all endpoints.

---

## Phase 2: Unit Testing & CI
- Tasks will be detailed when Phase 1 is complete.

## Phase 3: Docker & PostgreSQL Container
- Tasks will be detailed when Phase 2 is complete.

## Phase 4: Order API & Synchronous Inter-Service Calls
- Tasks will be detailed when Phase 3 is complete.

## Phase 5: YARP Gateway & Redis Caching
- Tasks will be detailed when Phase 4 is complete.

## Phase 6: RabbitMQ & Asynchronous Notification Worker
- Tasks will be detailed when Phase 5 is complete.

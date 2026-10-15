# ShopMicro: Complete Architectural Journey & Progress Flow

This document provides a comprehensive visual and structural breakdown of everything built in **ShopMicro** from **Phase 0** through **Phase 3**.

> 💡 **Tip:** An interactive, fully-rendered web view of this document is available at [docs/GRAPHICAL_FLOW.html](file:///d:/CICD/docs/GRAPHICAL_FLOW.html). You can double-click it in Windows Explorer or open it in any browser to see interactive, rendered SVG diagrams!

---

## 1. System Architecture Map (Where We Are Today)

```mermaid
flowchart TB
    subgraph HostWorkstation ["💻 Local Workstation (Host: Windows)"]
        BrowserClient["🌐 Web Browser / HTTP Client\n(http://localhost:5001)"]
        HttpFile["📄 Catalog.Api.http\n(Manual API Tests)"]
        TestSuite["🧪 xUnit Catalog.Tests\n(20 Automated Unit Tests)"]
        DotnetCli["⚡ dotnet CLI / IDE\n(dotnet build / test / run)"]
    end

    subgraph DockerHost ["🐳 Docker Engine (Local Container Stack)"]
        subgraph BridgeNetwork ["Custom Bridge Network: shopmicro-network"]
            subgraph CatalogApiContainer ["Container: shopmicro-catalog-api"]
                ApiEntry["Catalog.Api.dll (.NET 10 LTS)\nNon-Root User (UID 1654)"]
                ApiPort["Port: 5001"]
            end

            subgraph PostgresContainer ["Container: shopmicro-catalog-db"]
                PgEngine["PostgreSQL 17 (Alpine)"]
                PgHealth["Health Check: pg_isready"]
                PgPort["Port: 5432"]
            end
        end

        subgraph PersistentStorage ["💾 Docker Named Volume"]
            NamedVolume["shopmicro-catalog-db-data\n(/var/lib/postgresql/data)"]
        end
    end

    subgraph GitHubCloud ["☁️ GitHub Actions CI Pipeline"]
        CiRunner["Runner: ubuntu-latest\n(.NET 10.0.x SDK)"]
        CiSteps["1. Checkout ➔ 2. Setup .NET ➔ 3. Restore ➔ 4. Build Release ➔ 5. Run Tests"]
    end

    BrowserClient -->|"HTTP: 5001"| ApiPort
    HttpFile -.->|"Executes Requests"| ApiPort
    TestSuite -.->|"Tests In-Memory"| ApiEntry
    DotnetCli -->|"Builds & Runs"| ApiEntry

    ApiEntry -->|"Internal DNS: Host=catalog-db:5432"| PgPort
    PgEngine --- NamedVolume
    PgHealth -.->|"Reports healthy"| ApiEntry

    DotnetCli -->|"git push"| CiRunner
    CiRunner --> CiSteps
```

---

## 2. Step-by-Step Implementation Timeline

Every task has been completed and verified atomically:

```mermaid
flowchart LR
    subgraph P0 ["Phase 0: Foundation"]
        T01["0.1 Git & .gitignore"] --> T02["0.2 Solution ShopMicro.sln"]
        T02 --> T03["0.3 Coding Rules (AGENTS.md)"]
        T03 --> T04["0.4 ADRs (DECISIONS.md)"]
        T04 --> T05["0.5 Architecture Overview (README.md)"]
    end

    subgraph P1 ["Phase 1: Catalog API & Postgres"]
        T11["1.1-1.3 Project Scaffold (Port 5001)"] --> T12["1.4-1.8 Models, EF Core & Migrations"]
        T12 --> T13["1.9-1.13 DTOs, Interface & Service"]
        T13 --> T14["1.14-1.17 CRUD Controller Endpoints"]
        T14 --> T15["1.18 RFC 7807 ProblemDetails"]
        T15 --> T16["1.19 Native Catalog.Api.http"]
    end

    subgraph P2 ["Phase 2: Unit Testing & CI"]
        T21["2.1-2.2 xUnit Test Project (.NET 10)"] --> T22["2.3 DTO Validation Tests"]
        T22 --> T23["2.4-2.5 Service & Controller Tests"]
        T23 --> T24["2.6 Local Verification (20/20 Pass)"]
        T24 --> T25["2.7 GitHub Actions CI Workflow"]
        T25 --> T26["2.8 Deliberate Failure Demonstration"]
    end

    subgraph P3 ["Phase 3: Docker & Compose"]
        T31["3.1 Multi-Stage Dockerfile (.NET 10)"] --> T32["3.2 Root .dockerignore"]
        T32 --> T33["3.3 Compose: PostgreSQL & Volume"]
        T33 --> T34["3.4 Compose: Catalog API & DNS Network"]
        T34 --> T35["3.5 Health Check (pg_isready)"]
        T35 --> T36["3.6 .env.example Security Template"]
        T36 --> T37["3.7 Full Container Lifecycle Verified"]
    end

    P0 --> P1 --> P2 --> P3
```

---

## 3. The API Request-Response Lifecycle

Here is how an incoming HTTP request traverses the Catalog service:

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / HTTP Runner
    participant Pipeline as ASP.NET Core Middleware
    participant Controller as ProductsController
    participant Service as ProductService
    participant Db as CatalogDbContext (EF Core)
    participant Postgres as PostgreSQL (catalogdb)

    Note over Client, Pipeline: Incoming HTTP Request (e.g. POST /products)
    Client->>Pipeline: POST /products { "name": "Desk", "price": 199.99 }
    
    alt Validation Failure (e.g. Price < 0)
        Pipeline-->>Client: 400 Bad Request (ValidationProblemDetails)
    else Valid Payload
        Pipeline->>Controller: Route to Create(request, cancellationToken)
        Controller->>Service: CreateAsync(request, cancellationToken)
        Service->>Db: AddAsync(product, cancellationToken)
        Db->>Postgres: INSERT INTO "Products" VALUES (...)
        Postgres-->>Db: Acknowledge & Persist
        Db-->>Service: SaveChangesAsync() Done
        Service-->>Controller: Return ProductResponse DTO
        Controller-->>Client: 201 Created (Location: /products/{id}, Body: ProductResponse)
    end

    Note over Client, Pipeline: Missing Item Request (e.g. GET /products/{missing-guid})
    Client->>Pipeline: GET /products/{id}
    Pipeline->>Controller: Route to GetById(id, cancellationToken)
    Controller->>Service: GetByIdAsync(id, cancellationToken)
    Service->>Db: Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
    Db->>Postgres: SELECT * FROM "Products" WHERE "Id" = id LIMIT 1
    Postgres-->>Db: NULL (Not Found)
    Db-->>Service: null
    Service-->>Controller: null
    Controller-->>Client: 404 Not Found (RFC 7807 ProblemDetails + traceId)
```

---

## 4. Multi-Stage Docker Build Flow

Our multi-stage Docker build guarantees minimal container size and non-root runtime security:

```mermaid
flowchart TD
    subgraph StageBuild ["Stage 2: Build & Restore (mcr.microsoft.com/dotnet/sdk:10.0)"]
        CopyCsproj["1. COPY Catalog.Api.csproj"]
        Restore["2. RUN dotnet restore\n(Cached Layer if dependencies don't change)"]
        CopyCode["3. COPY remaining source code"]
        Compile["4. RUN dotnet build -c Release"]
        CopyCsproj --> Restore --> CopyCode --> Compile
    end

    subgraph StagePublish ["Stage 3: Publish (Intermediate)"]
        Publish["5. RUN dotnet publish -c Release -o /app/publish"]
        Compile --> Publish
    end

    subgraph StageRuntime ["Stage 1 & 4: Final Lean Runtime (mcr.microsoft.com/dotnet/aspnet:10.0)"]
        SlimBase["Base Image (Only ASP.NET Core Runtime ~200MB)"]
        NonRoot["Set USER $APP_UID (UID 1654)"]
        CopyBinaries["6. COPY --from=publish /app/publish .\n(Only compiled DLLs, zero SDK tools)"]
        Entrypoint["7. ENTRYPOINT ['dotnet', 'Catalog.Api.dll']"]
        SlimBase --> NonRoot --> CopyBinaries --> Entrypoint
    end

    Publish -->|"Extracts compiled output"| CopyBinaries
```

---

## 5. Automated CI Pipeline Flow

Every commit or pull request against `master`/`main` executes this automated validation pipeline:

```mermaid
flowchart TD
    GitTrigger["Git Push / Pull Request to master"] --> RunnerInit["Spin up GitHub Runner (ubuntu-latest)"]

    subgraph PipelineExecution ["GitHub Actions Workflow (.github/workflows/ci.yml)"]
        S1["Step 1: Checkout Repository\n(actions/checkout@v4)"]
        S2["Step 2: Setup .NET 10 SDK\n(actions/setup-dotnet@v4 with '10.0.x')"]
        S3["Step 3: Verify SDK Version\n(dotnet --version)"]
        S4["Step 4: Restore Solution Dependencies\n(dotnet restore ShopMicro.sln)"]
        S5["Step 5: Compile Solution in Release\n(dotnet build ShopMicro.sln --no-restore -c Release)"]
        S6["Step 6: Execute 20 xUnit Tests in Release\n(dotnet test ShopMicro.sln --no-build -c Release)"]
        
        S1 --> S2 --> S3 --> S4 --> S5 --> S6
    end

    RunnerInit --> S1

    S6 --> PassBranch{"All 20 Tests Pass?"}
    PassBranch -->|"Yes (Exit Code 0)"| GreenCI["✅ Build & Tests Passed: Merge Permitted"]
    PassBranch -->|"No (Exit Code 1)"| RedCI["❌ Broken Assertion: Pipeline Halts & Merge Blocked"]
```

---

## 6. Comprehensive Phase Progress Matrix

| Phase | Description | Key Artifacts & Accomplishments | Status |
| :--- | :--- | :--- | :---: |
| **Phase 0** | **Environment & Decisions** | [ShopMicro.sln](file:///d:/CICD/ShopMicro.sln), [AGENTS.md](file:///d:/CICD/AGENTS.md), [DECISIONS.md](file:///d:/CICD/docs/DECISIONS.md) (ADR-001 through ADR-005) | **Completed** |
| **Phase 1** | **Catalog API & PostgreSQL** | [Product.cs](file:///d:/CICD/src/Catalog.Api/Models/Product.cs), [CatalogDbContext.cs](file:///d:/CICD/src/Catalog.Api/Data/CatalogDbContext.cs), [ProductService.cs](file:///d:/CICD/src/Catalog.Api/Services/ProductService.cs), [ProductsController.cs](file:///d:/CICD/src/Catalog.Api/Controllers/ProductsController.cs), [Catalog.Api.http](file:///d:/CICD/src/Catalog.Api/Catalog.Api.http) | **Completed** |
| **Phase 2** | **Unit Testing & CI Pipeline** | [Catalog.Tests.csproj](file:///d:/CICD/tests/Catalog.Tests/Catalog.Tests.csproj), 20 Unit Tests ([Validation](file:///d:/CICD/tests/Catalog.Tests/CreateProductRequestValidationTests.cs), [Service](file:///d:/CICD/tests/Catalog.Tests/ProductServiceTests.cs), [Controller](file:///d:/CICD/tests/Catalog.Tests/ProductsControllerTests.cs)), [.github/workflows/ci.yml](file:///d:/CICD/.github/workflows/ci.yml) | **Completed** |
| **Phase 3** | **Docker & Local Compose** | [Dockerfile](file:///d:/CICD/src/Catalog.Api/Dockerfile), [.dockerignore](file:///d:/CICD/.dockerignore), [docker-compose.yml](file:///d:/CICD/docker-compose.yml), [.env.example](file:///d:/CICD/.env.example) | **Completed** |
| **Phase 4** | **Order API & Inter-Service Calls** | Order microservice, SQL Server, inter-service `HttpClient` with resilience, `POST /orders` | **Next Up** |
| **Phase 5** | **YARP Gateway & Redis Caching** | Reverse proxy, cache-aside pattern, route dispatching | Upcoming |
| **Phase 6** | **RabbitMQ & Asynchronous Worker** | MassTransit, `OrderCreatedEvent`, Outbox pattern, notification worker | Upcoming |
| **Phase 7** | **Observability & Health Checks** | OpenTelemetry, Serilog, Seq container, `/health/live`, `/health/ready` | Upcoming |
| **Phase 8** | **Azure Cloud Deployment** | Azure Container Apps, Azure DB for PostgreSQL, Azure SQL, Key Vault | Upcoming |
| **Phase 9** | **Security & Polish** | Rate limiting, authentication/authorization | Upcoming |

---

## 7. Next Step: Phase 4 Milestone

We are ready to start **Phase 4: Order API & Synchronous Inter-Service Communication**.

The immediate first step will be:
* **Task 4.1:** Scaffold the `Order.Api` ASP.NET Core Web API project targeting **.NET 10 LTS** with Controllers and attach it to [ShopMicro.sln](file:///d:/CICD/ShopMicro.sln).

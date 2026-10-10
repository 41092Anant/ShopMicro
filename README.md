# ShopMicro - Modern .NET 10 Microservices

A practical, phased microservices learning project following clean, real-world industry practices.

## Architecture Overview

```
                      +-------------------+
                      |   YARP Gateway    | (Port: 5000)
                      +---------+---------+
                                |
             +------------------+------------------+
             |                                     |
             v                                     v
    +-----------------+                   +-----------------+
    |   Catalog API   | (Port: 5001)      |    Order API    | (Port: 5002)
    |  (PostgreSQL)   |                   |  (SQL Server)   |
    +-----------------+                   +--------+--------+
                                                   |
                                                   | Publishes OrderCreated
                                                   v
                                          +-----------------+
                                          |    RabbitMQ     | (Port: 5672, UI: 15672)
                                          +--------+--------+
                                                   |
                                                   v
                                          +-----------------+
                                          | Notification    |
                                          | Worker          |
                                          +-----------------+
```

## Technology Stack

* **Platform:** .NET 10 LTS
* **API Style:** ASP.NET Core Web API with Controllers (`[ApiController]`)
* **Data Access:** Entity Framework Core
* **Databases:** PostgreSQL (Catalog API), SQL Server (Order API)
* **Messaging:** RabbitMQ + MassTransit
* **Gateway:** YARP (Yet Another Reverse Proxy)
* **Caching:** Redis + `IDistributedCache`
* **Observability:** OpenTelemetry, Serilog + Seq

## Project Structure

```
d:/CICD/
├── docs/                 # Architectural decision records and runbooks
├── src/                  # Microservice projects (Catalog.Api, Order.Api, etc.)
├── tests/                # Unit and integration tests
├── AGENTS.md             # Development operating guidelines
├── README.md             # Project documentation
└── ShopMicro.sln         # Root solution file
```

## Current Status

* **Phase 0:** Environment, solution setup, and decision records (Completed)
* **Phase 1:** Catalog API and PostgreSQL (Upcoming)

<h1 align="center">
  <br>
  <a href="https://github.com/your-username/elaris-marketplace">
    <img src="https://res.cloudinary.com/dqpkxxzaf/image/upload/v1759222012/egg-logo_pflvdz.png" alt="Elaris Marketplace" width="100">
  </a>
  <br>
  Elaris Marketplace
  <br>
</h1>

<h4 align="center">
  🐾 Elaris Marketplace — A Pixel Pet Trading System built on microservice architecture for learning production-ready distributed systems (CQRS, Saga, Outbox, Observability, etc.)
</h4>

<p align="center">
  <a href="#project-overview">Overview</a> •
  <a href="#key-features">Key Features</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#tech-stack">Tech Stack</a> •
  <a href="#local-development">Local Development</a> •
  <a href="#testing--observability">Testing & Observability</a> •
  <a href="#deployment--roadmap">Deployment & Roadmap</a> •
</p>

---

## 🧱 Project Overview

**Elaris Marketplace** là một **microservice learning project** mô phỏng chợ ảo, nơi người dùng có thể mua, bán và trade thú cưng pixel.  
Mục tiêu chính: **thực hành kiến trúc microservice thực tế**, áp dụng các pattern phổ biến trong hệ thống sản xuất như CQRS, Outbox, Saga Orchestration, Observability và Resilience.

- 🎯 Học patterns sản xuất (gateway, messaging, tracing, saga)
- 🧩 Kiến trúc phân tán có thể mở rộng
- 🧠 Môi trường lý tưởng để học và demo microservices .NET hiện đại

---

## 🔑 Key Features

- 🔐 **Authentication & Authorization:** Duende IdentityServer, JWT RS256, refresh token rotation  
- 🐉 **Product Management:** CRUD với MongoDB, filter/sort/paging  
- 🛒 **Basket Service:** Redis-based, atomic operations, TTL  
- 📦 **Inventory Management:** Postgres + gRPC + reservation logic  
- 🧾 **Ordering System:** CQRS, Outbox, Read model, MassTransit Saga  
- 💳 **Payment Flow:** Simulated preauthorize & capture  
- 🪄 **Observability:** OpenTelemetry, Prometheus, Grafana, Loki  
- ⚙️ **Resilience:** Polly retry, circuit breaker  
- 🚦 **Gateway (YARP):** Rate limiting, caching, structured logging  

---

## 🏗️ Architecture

<p align="center">
  <img src="https://res.cloudinary.com/dqpkxxzaf/image/upload/v1759222500/microservice-arch_elaris.png" alt="Architecture" width="700" style="margin:8px; border-radius:8px;">
</p>

```
     [ Gateway (YARP) ]
          ↓
 ┌────────────────────────────┐
 │ Identity (Duende)          │
 │ ProductService (MongoDB)   │
 │ Basket (Redis)             │
 │ Inventory (Postgres/gRPC)  │
 │ Ordering (CQRS + Saga)     │
 │ Payment (Simulated)        │
 └────────────────────────────┘
         ↕ RabbitMQ (MassTransit)
        ↳ Observability (OTel, Grafana)
```

---

## 🧰 Tech Stack

| Layer | Technology |
|-------|-------------|
| API Gateway | YARP (.NET 8) |
| Auth | Duende IdentityServer |
| Messaging | RabbitMQ + MassTransit |
| Databases | MongoDB, PostgreSQL, Redis |
| Saga Orchestration | MassTransit Saga |
| Observability | OpenTelemetry, Prometheus, Grafana, Loki |
| CI/CD | GitHub Actions + Helm + Kubernetes |

---

## ⚙️ Local Development

### Prerequisites
- Docker & Docker Compose  
- .NET 8 SDK  

### Setup & Run

```bash
# Clone the repository
git clone https://github.com/your-username/elaris-marketplace.git
cd elaris-marketplace

# Start dependencies
docker compose up -d

# Run services
dotnet run --project Services.Identity
dotnet run --project Services.ProductService
dotnet run --project Services.InventoryService
dotnet run --project Services.OrderService
dotnet run --project Services.PaymentService
dotnet run --project Gateway

# Access URLs
Gateway API: http://localhost:8000
```

---

## 🧪 Testing & Observability

### Testing Strategy
| Type | Description |
|------|-------------|
| Unit | Business logic |
| Integration | Mongo, Postgres, RabbitMQ (TestContainers) |
| Contract | Event schema validation |
| E2E | Simulate purchase flow |
| Chaos | RabbitMQ/network failures |
| Load | k6 / Locust testing |

### Observability Stack
- **Tracing:** OpenTelemetry → Grafana Tempo / Jaeger  
- **Metrics:** Prometheus  
- **Logs:** Serilog → Loki  
- **Dashboards:** Grafana panels per service  
- **Alerts:** Prometheus Alertmanager  

---

## 🧑‍💻 Contributors

| Name | Role |
|------|------|
| Trần Văn Nhanh | Architect / Developer |
| (You?) | Join via PR or Issue! |

---


Folder Structure 28/11/2025.
└───src
    │   Elaris.sln
    │   inventory.proto
    │   InventoryGrpcService.cs
    │
    ├───ApiGateway
    │   │   ApiGateway.csproj
    │   │   ApiGateway.csproj.user
    │   │   ApiGateway.http
    │   │   appsettings.Development.json
    │   │   appsettings.json
    │   │   Dockerfile
    │   │   Program.cs
    │   │
    │   ├───Middlewares
    │   │       LoggingMiddleware.cs
    │   │       SwaggerAggregatorMiddleware.cs
    │   │
    │   └───Properties
    │           launchSettings.json
    │
    ├───BuildingBlocks
    │   └───Contracts
    │       │   BuildingBlocks.Contracts.csproj
    │       │
    │       ├───Commands
    │       │       OrderProcessingCommands.cs
    │       │
    │       ├───Events
    │       │       BasketEvent.cs
    │       │       InventoryEvent.cs
    │       │       OrderEvent.cs
    │       │       PaymentEvent.cs
    │       │       ProductEvent.cs
    │
    ├───BuildingBlocks.GrpcContracts
    │   │   BuildingBlocks.GrpcContracts.csproj
    │   │
    │   └───proto
    │           inventory.proto
    │           payment.proto
    │
    ├───BuildingBlocks.Infrastucture
    │   │   BuildingBlocks.Infrastucture.csproj
    │   │
    │   └───Authentication
    │           JwtAuthenticationHelper.cs
    │
    ├───GrpcContracts
    │       GrpcContracts.csproj
    │
    ├───Services.BasketService
    │   ├───Services.BasketService.API
    │   │   │   appsettings.Development.json
    │   │   │   appsettings.json
    │   │   │   Dockerfile
    │   │   │   Program.cs
    │   │   │   Services.BasketService.API.csproj
    │   │   │   Services.BasketService.API.csproj.user
    │   │   │   Services.BasketService.API.http
    │   │   │
    │   │   ├───Extensions
    │   │   │       HttpContextExtensions.cs
    │   │   │
    │   │   └───Properties
    │   │           launchSettings.json
    │   │
    │   ├───Services.BasketService.Application
    │   │   │   Services.BasketService.Application.csproj
    │   │   │
    │   │   ├───Interfaces
    │   │   │       IBasketRepository.cs
    │   │   │       ICatalogServiceClient.cs
    │   │   │
    │   │   ├───Models
    │   │   │       Basket.cs
    │   │   │       BasketDto.cs
    │   │   │       BasketItem.cs
    │   │   │       ProductDto.cs
    │   │   │
    │   │   └───Validators
    │   │           BasketItemValidator.cs
    │   │
    │   └───Services.BasketService.Infrastructure
    │       │   Services.BasketService.Infrastructure.csproj
    │       │
    │       ├───Monitoring
    │       │       RedisMetrics.cs
    │       │
    │       ├───Repositories
    │       │       BasketRepository.cs
    │       │
    │       └───Services
    │               CatalogServiceClient.cs
    │
    ├───Services.CatalogService
    │   │   appsettings.Development.json
    │   │   appsettings.json
    │   │   Dockerfile
    │   │   Program.cs
    │   │   Services.CatalogService.csproj
    │   │   Services.CatalogService.csproj.user
    │   │   Services.CatalogService.http
    │   │
    │   ├───Config
    │   │       MinIOOptions.cs
    │   │
    │   ├───Data
    │   │       MongoContext.cs
    │   │       SoftDeleteCollection.cs
    │   │
    │   ├───Extensions
    │   │       HttpContextExtensions.cs
    │   │
    │   ├───Features
    │   │   └───Products
    │   │       ├───ApproveProduct
    │   │       │       ApproveProductEndpoints.cs
    │   │       ├───CreateProduct
    │   │       │       CreateProductEndpoint.cs
    │   │       ├───DeleteProduct
    │   │       │       DeleteProductEndpoint.cs
    │   │       ├───GetAllProducts
    │   │       │       GetAllProductsEndpoint.cs
    │   │       ├───GetMyProducts
    │   │       │       GetMyProductsEndpoint.cs
    │   │       ├───GetPendingProducts
    │   │       │       GetPendingProductsEndpoint.cs
    │   │       ├───GetProduct
    │   │       │       GetProductEndpoint.cs
    │   │       ├───GetProducts
    │   │       │       GetProductsEndpoint.cs
    │   │       │       GetProductsQuery.cs
    │   │       ├───RejectProduct
    │   │       │       RejectProductEndpoint.cs
    │   │       └───UpdateProduct
    │   │               UpdateProductEndpoint.cs
    │   │
    │   ├───Models
    │   │       CreateProductRequest.cs
    │   │       Product.cs
    │   │       UpdateProductRequest.cs
    │   │
    │   ├───Properties
    │   │       launchSettings.json
    │   │
    │   └───Services
    │           FileStorageService.cs
    │
    ├───Services.IdentityService
    │   │   appsettings.Development.json
    │   │   appsettings.json
    │   │   Dockerfile
    │   │   IdentityServerConfig.cs
    │   │   Program.cs
    │   │   Services.IdentityService.csproj
    │   │   Services.IdentityService.csproj.user
    │   │   Services.IdentityService.http
    │   │
    │   ├───Controllers
    │   │       AuthController.cs
    │   │       UserController.cs
    │   │
    │   ├───Data
    │   │   │   AppDbContext.cs
    │   │   │   SeedData.cs
    │   │   │
    │   │   ├───Entities
    │   │   │       PayoutRequest.cs
    │   │   │
    │   │   └───Migrations
    │   │           20251021074357_InitialIdentity.cs
    │   │           20251021074357_InitialIdentity.Designer.cs
    │   │           20251022043642_AddRefreshToken.cs
    │   │           20251022043642_AddRefreshToken.Designer.cs
    │   │           20251125083847_AddUserExtendedFields.cs
    │   │           20251125083847_AddUserExtendedFields.Designer.cs
    │   │           AppDbContextModelSnapshot.cs
    │   │
    │   ├───DTOs
    │   │       UserDtos.cs
    │   │
    │   ├───Extensions
    │   │       HttpContextExtensions.cs
    │   │
    │   ├───Properties
    │   │       launchSettings.json
    │   │
    │   └───Security
    │           Argon2PasswordHasher.cs
    │           RsaKeyProvider.cs
    │
    ├───Services.InventoryService
    │   ├───Services.InventoryService.API
    │   │   │   appsettings.Development.json
    │   │   │   appsettings.json
    │   │   │   DesignTimeDbContextFactory.cs
    │   │   │   Dockerfile
    │   │   │   Program.cs
    │   │   │   Services.InventoryService.API.csproj
    │   │   │   Services.InventoryService.API.csproj.user
    │   │   │   Services.InventoryService.API.http
    │   │   │
    │   │   ├───Controllers
    │   │   │       InventoryController.cs
    │   │   │
    │   │   ├───Extensions
    │   │   │       HttpContextExtensions.cs
    │   │   │
    │   │   ├───Grpc
    │   │   │       InventoryGrpcService.cs
    │   │   │
    │   │   └───Properties
    │   │           launchSettings.json
    │   │
    │   ├───Services.InventoryService.Application
    │   │   │   AssemblyReference.cs
    │   │   │   Services.InventoryService.Application.csproj
    │   │   │
    │   │   ├───Common
    │   │   │   ├───Mappings
    │   │   │   │       InventoryMappingConfig.cs
    │   │   │   │
    │   │   │   └───Models
    │   │   │           PaginatedList.cs
    │   │   │
    │   │   ├───DTOs
    │   │   │       InventoryItemDto.cs
    │   │   │       OrderDto.cs
    │   │   │       ProductDto.cs
    │   │   │
    │   │   ├───Interfaces
    │   │   │       ICatalogServiceClient.cs
    │   │   │       IInventoryRepository.cs
    │   │   │       IInventoryService.cs
    │   │   │       IUnitOfWork.cs
    │   │   │
    │   │   ├───Inventory
    │   │   │   ├───Commands
    │   │   │   │   ├───BulkUpdateInventory
    │   │   │   │   │       BulkUpdateInventoryCommand.cs
    │   │   │   │   │       BulkUpdateInventoryCommandHandler.cs
    │   │   │   │   ├───ConfirmStockDeduction
    │   │   │   │   │       ConfirmStockDeductionCommand.cs
    │   │   │   │   │       ConfirmStockDeductionCommandHandler.cs
    │   │   │   │   ├───CreateOrUpdateInventoryItem
    │   │   │   │   │       CreateOrUpdateInventoryItemCommand.cs
    │   │   │   │   │       CreateOrUpdateInventoryItemCommandHandler.cs
    │   │   │   │   ├───DecreaseStock
    │   │   │   │   │       DecreaseStockCommand.cs
    │   │   │   │   │       DecreaseStockCommandHandler.cs
    │   │   │   │   ├───IncreaseStock
    │   │   │   │   │       IncreaseStockCommand.cs
    │   │   │   │   │       IncreaseStockCommandHandler.cs
    │   │   │   │   ├───ReleaseStock
    │   │   │   │   │       ReleaseStockCommand.cs
    │   │   │   │   │       ReleaseStockCommandHandler.cs
    │   │   │   │   ├───ReserveStock
    │   │   │   │   │       ReserveStockCommand.cs
    │   │   │   │   │       ReserveStockCommandHandler.cs
    │   │   │   │   └───SetLowStockThreshold
    │   │   │   │           SetLowStockThresholdCommand.cs
    │   │   │   │           SetLowStockThresholdCommandHandler.cs
    │   │   │   │
    │   │   │   └───Queries
    │   │   │       ├───CheckProductsAvailability
    │   │   │       │       CheckProductsAvailabilityQuery.cs
    │   │   │       │       CheckProductsAvailabilityQueryHandler.cs
    │   │   │       ├───GetInventoryByProductId
    │   │   │       │       GetInventoryByProductIdQuery.cs
    │   │   │       │       GetInventoryByProductIdQueryHandler.cs
    │   │   │       ├───GetInventoryHistory
    │   │   │       │       GetInventoryHistoryQuery.cs
    │   │   │       │       GetInventoryHistoryQueryHandler.cs
    │   │   │       ├───GetInventoryList
    │   │   │       │       GetInventoryListQuery.cs
    │   │   │       │       GetInventoryListQueryHandler.cs
    │   │   │       ├───GetInventoryStatistics
    │   │   │       │       GetInventoryStatisticsQuery.cs
    │   │   │       │       GetInventoryStatisticsQueryHandler.cs
    │   │   │       └───GetLowStockItems
    │   │   │               GetLowStockItemsQuery.cs
    │   │   │               GetLowStockItemsQueryHandler.cs
    │   │   │
    │   ├───Services.InventoryService.Domain
    │   │   │   Services.InventoryService.Domain.csproj
    │   │   │
    │   │   └───Entities
    │   │           InventoryHistory.cs
    │   │           InventoryItem.cs
    │   │           StockReservation.cs
    │   │
    │   └───Services.InventoryService.Infrastructure
    │       │   Services.InventoryService.Infrastructure.csproj
    │       │
    │       ├───BackgroundServices
    │       │       ExpiredReservationCleanupService.cs
    │       │       ReservationTimeoutService.cs
    │       │
    │       ├───Consumers
    │       │       ConfirmInventoryConsumer.cs
    │       │       ProductCreatedConsumer.cs
    │       │       ReleaseInventoryConsumer.cs
    │       │       ReserveInventoryConsumer.cs
    │       │
    │       ├───Extensions
    │       │       DependencyInjection.cs
    │       │
    │       ├───Migrations
    │       │       20251030083514_InitialInventory.cs
    │       │       20251030083514_InitialInventory.Designer.cs
    │       │       20251113045114_AddReservedQuantity.cs
    │       │       20251113045114_AddReservedQuantity.Designer.cs
    │       │       20251127102506_AddInventoryExtendedEntitiesAndFields.cs
    │       │       20251127102506_AddInventoryExtendedEntitiesAndFields.Designer.cs
    │       │       InventoryDbContextModelSnapshot.cs
    │       │
    │       ├───Persistence
    │       │       InventoryDbContext.cs
    │       │
    │       ├───Repositories
    │       │       InventoryRepository.cs
    │       │       UnitOfWork.cs
    │       │
    │       └───Services
    │               CatalogServiceClient.cs
    │               InventoryService.cs
    │
    ├───Services.InventoryService.Grpc
    │   │   appsettings.Development.json
    │   │   appsettings.json
    │   │   Program.cs
    │   │   Services.InventoryService.Grpc.csproj
    │   │   Services.InventoryService.Grpc.csproj.user
    │   │
    │   ├───Properties
    │   │       launchSettings.json
    │   │
    │   ├───Protos
    │   │       inventory.proto
    │   │
    │   └───Services
    │           InventoryGrpcService.cs
    │
    ├───Services.InventoryService.SDK
    │   │   ServiceCollectionExtension.cs
    │   │   Services.InventoryService.SDK.csproj
    │
    └───Services.OrderService
        ├───Services.OrderService.API
        │   │   appsettings.Development.json
        │   │   appsettings.json
        │   │   DesignTimeDbContextFactory.cs
        │   │   Dockerfile
        │   │   Program.cs
        │   │   Services.OrderService.API.csproj
        │   │   Services.OrderService.API.csproj.user
        │   │   Services.OrderService.API.http
        │   │
        │   ├───Controllers
        │   │       OrdersController.cs
        │   │
        │   ├───Extensions
        │   │       HttpContextExtensions.cs
        │   │
        │   ├───Middleware
        │   │       ExceptionMiddleware.cs
        │   │
        │   └───Properties
        │           launchSettings.json


## 🪪 License
MIT License © 2025 — Elaris Marketplace Team

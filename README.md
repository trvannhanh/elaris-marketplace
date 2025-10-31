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
  <a href="#ui--ux">UI / UX</a>
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
[ React UI (Pixel theme) ]
          ↓
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
| Frontend | React + Tailwind (Pixel Art UI) |
| CI/CD | GitHub Actions + Helm + Kubernetes |

---

## ⚙️ Local Development

### Prerequisites
- Docker & Docker Compose  
- .NET 8 SDK  
- Node.js (for React UI)

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
Frontend: http://localhost:3000  
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

## 🎨 UI / UX

<p align="center">
  <img src="https://res.cloudinary.com/dqpkxxzaf/image/upload/v1759222700/pixel-ui-1.png" alt="Product Grid" width="700" style="margin:6px; border-radius:8px;">
  <img src="https://res.cloudinary.com/dqpkxxzaf/image/upload/v1759222701/pixel-ui-2.png" alt="Basket" width="700" style="margin:6px; border-radius:8px;">
  <img src="https://res.cloudinary.com/dqpkxxzaf/image/upload/v1759222702/pixel-ui-3.png" alt="Order History" width="700" style="margin:6px; border-radius:8px;">
</p>

UI hướng phong cách **pixel-art 16-bit**, với:
- Grid sản phẩm có sprite & badge hiếm
- Modal giỏ hàng viền pixel
- Album “Pet Collection” dạng card retro
---

## 🧑‍💻 Contributors

| Name | Role |
|------|------|
| Trần Văn Nhanh | Architect / Developer |
| (You?) | Join via PR or Issue! |

---

## 🪪 License
MIT License © 2025 — Elaris Marketplace Team

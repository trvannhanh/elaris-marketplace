<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
  <title>Elaris Marketplace — Pixel Pet Trading System</title>
  <link href="https://cdn.jsdelivr.net/npm/tailwindcss@2.2.19/dist/tailwind.min.css" rel="stylesheet">
  <link href="https://fonts.googleapis.com/css2?family=Press+Start+2P&family=Roboto:wght@400;700&display=swap" rel="stylesheet">
  <style>
    body {
      background: #1a1a2e;
      color: #e0e7ff;
      font-family: 'Roboto', sans-serif;
      line-height: 1.7;
    }
    .pixel-font { font-family: 'Press Start 2P', cursive; }
    .pixel-border { border: 4px double #64ffda; }
    .sprite { image-rendering: pixelated; image-rendering: -moz-crisp-edges; image-rendering: crisp-edges; }
    pre { background: #16213e; padding: 1rem; border-radius: 0.5rem; overflow-x: auto; border: 1px solid #334155; }
    code { font-family: 'Courier New', monospace; color: #a5b4fc; }
    .badge { @apply inline-block px-2 py-1 text-xs font-bold rounded; }
    .badge-mvp { @apply bg-green-600 text-white; }
    .badge-future { @apply bg-purple-600 text-white; }
    .ascii { font-family: 'Courier New', monospace; background: #0f172a; padding: 1rem; border-radius: 0.5rem; color: #94a3b8; }
    .section { @apply mb-12; }
    .link { @apply text-cyan-400 hover:text-cyan-300 underline; }
  </style>
</head>
<body class="min-h-screen">

  <!-- Header -->
  <header class="bg-gradient-to-r from-indigo-900 to-purple-900 text-white py-12 text-center">
    <h1 class="pixel-font text-4xl md:text-6xl mb-4">Elaris Marketplace</h1>
    <p class="text-lg md:text-xl max-w-3xl mx-auto">
      Một chợ ảo nơi bạn có thể <strong class="text-yellow-300">mua, bán, trao đổi</strong> các “pet” và vật phẩm pixel art — được xây dựng như một <strong class="text-cyan-300">hệ thống microservices production-ready</strong> để học và thực hành kiến trúc hiện đại.
    </p>
  </header>

  <!-- Main Content -->
  <main class="container mx-auto px-6 py-12 max-w-5xl">

    <!-- Elevator Pitch -->
    <section class="section bg-gray-900 p-6 rounded-lg shadow-lg">
      <h2 class="pixel-font text-2xl mb-4 text-cyan-300">Elevator Pitch</h2>
      <p class="mb-4">
        <strong>Elaris Marketplace</strong> là một nền tảng thương mại điện tử ảo, nơi người dùng có thể <strong>sưu tầm, giao dịch và sở hữu</strong> các pet pixel art độc đáo.
      </p>
      <p>
        Hệ thống mô phỏng <strong>toàn bộ quy trình mua sắm thực tế</strong> — từ giỏ hàng, đặt hàng, thanh toán đến quản lý kho — với <strong>kiến trúc microservices đầy đủ</strong>, <strong>CQRS</strong>, <strong>Saga Orchestration</strong>, <strong>Outbox Pattern</strong>, <strong>gRPC</strong>, <strong>Event-Driven</strong>, <strong>Observability</strong>, <strong>Resilience</strong> và <strong>Security production-grade</strong>.
      </p>
      <p class="mt-4 font-bold text-yellow-300">
        Mục tiêu: Không chỉ là một app — mà là <em>một sân chơi thực tế</em> để triển khai <strong>tất cả các pattern microservice quan trọng nhất</strong> trong .NET 8.
      </p>
    </section>

    <!-- Tính năng chính -->
    <section class="section">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Tính năng chính</h2>
      <div class="grid md:grid-cols-2 gap-6">
        <div class="bg-gray-800 p-6 rounded-lg">
          <h3 class="text-xl font-bold text-green-400 mb-3">MVP (Đã hoàn thiện)</h3>
          <ul class="space-y-2 text-sm">
            <li><span class="text-green-400">✓</span> Đăng ký / Đăng nhập (Duende + JWT RS256 + Refresh Rotation)</li>
            <li><span class="text-green-400">✓</span> Quản lý sản phẩm/pet (MongoDB) — filter, sort, paging</li>
            <li><span class="text-green-400">✓</span> Giỏ hàng (Redis) — thêm, xóa, checkout</li>
            <li><span class="text-green-400">✓</span> Inventory Reservation (Postgres + gRPC)</li>
            <li><span class="text-green-400">✓</span> Ordering (CQRS + Outbox + MassTransit)</li>
            <li><span class="text-green-400">✓</span> Saga Orchestration (MassTransit State Machine)</li>
            <li><span class="text-green-400">✓</span> Payment Simulation (Pre-auth → Capture)</li>
            <li><span class="text-green-400">✓</span> Observability (OpenTelemetry + Grafana)</li>
            <li><span class="text-green-400">✓</span> API Gateway (YARP) — Rate Limiting, Caching</li>
          </ul>
        </div>
        <div class="bg-gray-800 p-6 rounded-lg">
          <h3 class="text-xl font-bold text-purple-400 mb-3">Tính năng mở rộng (Tương lai)</h3>
          <ul class="space-y-2 text-sm">
            <li><span class="text-gray-500">○</span> Marketplace: Seller listing, đấu giá, offer</li>
            <li><span class="text-gray-500">○</span> Trade trực tiếp giữa người dùng</li>
            <li><span class="text-gray-500">○</span> NFT Minting & Pixel Art Generator</li>
            <li><span class="text-gray-500">○</span> Leaderboard, Events, Promotions</li>
            <li><span class="text-gray-500">○</span> Real-time notification (WebSocket)</li>
          </ul>
        </div>
      </div>
    </section>

    <!-- Kiến trúc hệ thống -->
    <section class="section">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Kiến trúc hệ thống</h2>
      <div class="ascii text-xs md:text-sm">
<pre>
┌──────────────┐    HTTPS    ┌──────────────┐
│   Browser /  │───────────▶│  YARP Gateway│
│   React UI   │  (BFF)     │ (RateLimit,  │
└──────────────┘            │  Cache, Auth)│
        ▲                   └──────┬───────┘
        │                          │
        │ gRPC / HTTP              │ HTTP / gRPC
        │                          ▼
┌───────┴───────┐            ┌─────────────┐
│   Duende      │◀──────────▶│ Product     │
│ IdentityServer │ JWT       │ Service     │
└───────┬───────┘            │ (MongoDB)   │
        │                   └──────┬──────┘
        │                          │
        ▼                          ▼
┌──────────────┐ gRPC        ┌─────────────┐
│ Inventory    │◀───────────▶│ Basket      │
│ Service      │  Reserve    │ (Redis)     │
│ (Postgres)   │             └──────┬──────┘
└───────┬──────┘                    │
        │                           ▼
        │                     ┌─────────────┐
        │ Events              │ Ordering    │
        ▼                     │ Service     │
┌──────────────┐ RabbitMQ     │ (CQRS +     │
│   RabbitMQ   │─────────────▶│  Saga)      │
│ (MassTransit)│  Events      └──────┬──────┘
└───────┬──────┘                    │
        │                           ▼
        │ HTTP/gRPC           │ Payment     │
        └───────────────────▶│ Service     │
                              │ (Simulated) │
                              └─────────────┘
</pre>
      </div>
      <p class="mt-4 text-sm text-gray-300">
        <strong>Observability</strong>: OpenTelemetry → Grafana Tempo / Prometheus / Loki
      </p>
    </section>

    <!-- Công nghệ & Pattern -->
    <section class="section">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Công nghệ & Pattern</h2>
      <div class="overflow-x-auto">
        <table class="w-full text-sm text-left border border-gray-700">
          <thead class="bg-gray-800">
            <tr>
              <th class="px-4 py-2">Layer</th>
              <th class="px-4 py-2">Tech & Pattern</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-700">
            <tr><td class="px-4 py-2 font-semibold">Gateway</td><td class="px-4 py-2">YARP (.NET 8), Rate Limiting, Output Caching, Serilog</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Auth</td><td class="px-4 py-2">Duende IdentityServer, JWT RS256, Refresh Rotation, Argon2id</td></tr>
            <tr><td class="px-4 py-2 font-semibold">API Style</td><td class="px-4 py-2">Minimal APIs + Vertical Slice</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Data</td><td class="px-4 py-2">MongoDB (Product), Postgres (Inventory/Orders), Redis (Basket)</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Messaging</td><td class="px-4 py-2">RabbitMQ + MassTransit, Outbox + Inbox Pattern</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Sync Comm</td><td class="px-4 py-2">gRPC (Inventory), HTTP</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Orchestration</td><td class="px-4 py-2">Saga State Machine (MassTransit)</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Resilience</td><td class="px-4 py-2">Polly (Retry, Circuit Breaker, Timeout)</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Observability</td><td class="px-4 py-2">OpenTelemetry, Grafana Stack</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Testing</td><td class="px-4 py-2">xUnit, Testcontainers, MassTransit In-Memory, k6</td></tr>
            <tr><td class="px-4 py-2 font-semibold">Deploy</td><td class="px-4 py-2">Docker, Kubernetes, Helm, GitHub Actions</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Bắt đầu nhanh -->
    <section class="section bg-gray-900 p-6 rounded-lg">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Bắt đầu nhanh (Local Development)</h2>
      <div class="space-y-4">
        <div>
          <h3 class="font-bold text-yellow-300 mb-2">Yêu cầu</h3>
          <ul class="list-disc list-inside text-sm ml-4">
            <li>Docker + Docker Compose</li>
            <li>.NET 8 SDK</li>
            <li>Node.js 18+ (cho UI)</li>
          </ul>
        </div>
        <div>
          <h3 class="font-bold text-yellow-300 mb-2">1. Clone repo</h3>
          <pre><code>git clone https://github.com/your-org/elaris-marketplace.git
cd elaris-marketplace</code></pre>
        </div>
        <div>
          <h3 class="font-bold text-yellow-300 mb-2">2. Khởi động infra</h3>
          <pre><code>docker-compose up -d</code></pre>
          <p class="text-sm text-gray-300">Khởi động: Postgres, Mongo, Redis, RabbitMQ, OpenTelemetry Collector, Grafana</p>
        </div>
        <div>
          <h3 class="font-bold text-yellow-300 mb-2">3. Chạy services</h3>
          <pre><code># Gateway
dotnet run --project src/Gateway

# Identity
dotnet run --project src/Identity

# Product Service
dotnet run --project src/ProductService</code></pre>
        </div>
        <div>
          <h3 class="font-bold text-yellow-300 mb-2">4. Mở UI</h3>
          <pre><code>cd ui && npm install && npm run dev</code></pre>
          <p class="text-sm">Truy cập: <a href="http://localhost:3000" class="link">http://localhost:3000</a></p>
        </div>
      </div>
    </section>

    <!-- Demo Flow -->
    <section class="section">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Demo Flow (Acceptance Criteria)</h2>
      <ol class="list-decimal list-inside space-y-3 text-sm">
        <li><strong>Đăng ký → Đăng nhập</strong> → nhận JWT</li>
        <li><strong>Duyệt danh sách pet</strong> → filter theo giá, loại</li>
        <li><strong>Thêm vào giỏ hàng</strong> → xem giỏ</li>
        <li><strong>Checkout</strong> → hệ thống:
          <ul class="list-disc list-inside ml-6 mt-1">
            <li>Reserve inventory (gRPC)</li>
            <li>Pre-authorize payment</li>
            <li>Saga điều phối: <code>reserve → capture → complete</code></li>
            <li>Hoặc rollback nếu fail</li>
          </ul>
        </li>
        <li><strong>Xem đơn hàng</strong> + trạng thái real-time</li>
        <li><strong>Kiểm tra trace</strong> trong Grafana Tempo</li>
      </ol>
    </section>

    <!-- Observability -->
    <section class="section bg-gray-900 p-6 rounded-lg">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Observability Dashboard</h2>
      <div class="grid md:grid-cols-2 gap-4 text-sm">
        <div><strong>Grafana</strong>: <a href="http://localhost:3001" class="link">http://localhost:3001</a></div>
        <div><strong>Tempo (Traces)</strong>: <a href="http://localhost:3001/d/tempo" class="link">http://localhost:3001/d/tempo</a></div>
        <div><strong>Prometheus</strong>: <a href="http://localhost:9090" class="link">http://localhost:9090</a></div>
        <div><strong>RabbitMQ</strong>: <a href="http://localhost:15672" class="link">http://localhost:15672</a> (guest/guest)</div>
      </div>
    </section>

    <!-- Roadmap -->
    <section class="section">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Roadmap phát triển</h2>
      <div class="overflow-x-auto">
        <table class="w-full text-sm text-left border border-gray-700">
          <thead class="bg-gray-800">
            <tr>
              <th class="px-4 py-2">Phase</th>
              <th class="px-4 py-2">Thời gian</th>
              <th class="px-4 py-2">Mục tiêu</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-700">
            <tr><td class="px-4 py-2">0 - Setup</td><td class="px-4 py-2">1 tuần</td><td class="px-4 py-2">Repo, Docker Compose, CI</td></tr>
            <tr><td class="px-4 py-2">1 - Core MVP</td><td class="px-4 py-2">3-4 tuần</td><td class="px-4 py-2">Auth, Product, Basket</td></tr>
            <tr><td class="px-4 py-2">2 - Inventory & Ordering</td><td class="px-4 py-2">3-4 tuần</td><td class="px-4 py-2">gRPC, Outbox, Events</td></tr>
            <tr><td class="px-4 py-2">3 - Saga & Payment</td><td class="px-4 py-2">2-3 tuần</td><td class="px-4 py-2">Full order flow</td></tr>
            <tr><td class="px-4 py-2">4 - Observability</td><td class="px-4 py-2">1-2 tuần</td><td class="px-4 py-2">OTel, Grafana, Alerts</td></tr>
            <tr><td class="px-4 py-2">5 - Polish</td><td class="px-4 py-2">1-2 tuần</td><td class="px-4 py-2">UI, Tests, Docs</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Testing -->
    <section class="section bg-gray-900 p-6 rounded-lg">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Testing Strategy</h2>
      <pre><code># Unit tests
dotnet test

# Integration tests (Testcontainers)
dotnet test --filter "Category=Integration"

# Load test (k6)
k6 run load/checkout.js</code></pre>
    </section>

    <!-- Bảo mật -->
    <section class="section">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Bảo mật & Best Practices</h2>
      <ul class="list-disc list-inside space-y-2 text-sm">
        <li>JWT RS256 + Key Vault</li>
        <li>Refresh Token Rotation + Revocation List</li>
        <li>Argon2id password hashing</li>
        <li>mTLS internal (k8s)</li>
        <li>Idempotency-Key cho mọi mutation</li>
        <li>Rate limiting per IP/user</li>
        <li>Input validation + sanitization</li>
      </ul>
    </section>

    <!-- UI/UX -->
    <section class="section bg-gray-900 p-6 rounded-lg">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">UI / UX (Pixel Art Vibe)</h2>
      <ul class="list-disc list-inside space-y-2 text-sm">
        <li><strong>Theme</strong>: 16-bit palette (NES/SNES style)</li>
        <li><strong>Sprites</strong>: 32x32 PNG, pixel-perfect</li>
        <li><strong>Layout</strong>: Grid cards, hover effects, modal cart</li>
        <li><strong>Tech</strong>: React + Tailwind + Canvas/CSS pixel filters</li>
      </ul>
    </section>

    <!-- Đóng góp -->
    <section class="section">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">Đóng góp</h2>
      <ol class="list-decimal list-inside space-y-2 text-sm">
        <li>Fork repo</li>
        <li>Tạo branch: <code>feature/xxx</code> hoặc <code>bugfix/xxx</code></li>
        <li>Commit theo <a href="https://www.conventionalcommits.org/" class="link">Conventional Commits</a></li>
        <li>Mở PR với template</li>
      </ol>
    </section>

    <!-- License & Community -->
    <section class="section text-center">
      <h2 class="pixel-font text-2xl mb-6 text-cyan-300">License & Cộng đồng</h2>
      <p class="mb-4">
        <a href="LICENSE" class="link font-bold">MIT License</a> – Xem file <code>LICENSE</code>
      </p>
      <div class="space-x-6">
        <a href="https://discord.gg/elaris" class="link">Discord</a>
        <a href="https://github.com/your-org/elaris-marketplace/discussions" class="link">GitHub Discussions</a>
      </div>
    </section>

    <!-- Footer -->
    <footer class="text-center py-8 text-gray-400 text-sm">
      <p>
        <strong>Elaris Marketplace</strong> — <em>Không chỉ là code, mà là một hành trình kiến trúc.</em><br/>
        <strong class="text-yellow-300">Bắt đầu sưu tầm pet của bạn ngay hôm nay!</strong>
      </p>
    </footer>

  </main>
</body>
</html>

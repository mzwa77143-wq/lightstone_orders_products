# Orders & Inventory Service (.NET 8 Clean Architecture)

A production-ready, high-concurrency ASP.NET Core (.NET 8) microservice built with Clean Architecture, Entity Framework Core, Dapper, and Microsoft SQL Server.

---

## Features

- **Clean Architecture**: Clear separation across Domain, Application, Infrastructure, and API layers.
- **High-Concurrency Inventory Management**: Database-level atomic row locking (`WITH (UPDLOCK, ROWLOCK)`) and atomic SQL queries preventing overselling under intense concurrent demand.
- **Deadlock Avoidance**: Deterministic alphabetical sorting of SKUs prior to lock acquisition.
- **Idempotent Order Processing**: Strict idempotency using `ExternalOrderId` and unique indexes. Duplicate submissions return HTTP 200 OK without mutating inventory.
- **High-Performance Daily Sales Reporting**: Aggregated sales reporting powered by Dapper queries targeting optimized composite indexes on `Orders(PlacedAtUtc)` and `OrderItems`.
- **Operational Readiness**:
  - Structured JSON logging with Serilog capturing explicit outcomes (`Accepted`, `DuplicateIgnored`, `RejectedInsufficientStock`, `RejectedInvalidProduct`).
  - Liveness (`/health`) and Readiness (`/health/ready` with SQL Server connectivity check) health check endpoints.
  - Startup database migration and automated seeding (`IDataSeeder`).
- **Comprehensive Test Coverage**: Unit tests and integration tests covering concurrency, idempotency, and reporting.

---

## Solution Structure

```
OrdersAndInventoryService/
├── src/
│   ├── OrdersAndInventory.Domain/            # Entities, value objects, exceptions, enums
│   ├── OrdersAndInventory.Application/       # DTOs, interfaces, services, validators
│   ├── OrdersAndInventory.Infrastructure/    # EF Core DbContext, Dapper queries, migrations, seeder
│   └── OrdersAndInventory.Api/               # Controllers, middleware, Serilog, health checks
├── tests/
│   ├── OrdersAndInventory.UnitTests/         # Domain and validator unit tests
│   └── OrdersAndInventory.IntegrationTests/  # End-to-end API, concurrency, idempotency tests
├── media/                                    # Synthesized voice narration audio files (.wav)
├── walkthrough_player.html                   # Interactive Video & Audio Walkthrough Player
├── OrdersAndInventoryService.postman_collection.json # Postman Collection for manual testing
├── VIDEO_WALKTHROUGH_SCRIPT.md               # Word-for-word video presentation script
├── README.md                                 # Setup & usage instructions
└── SOLUTION.md                               # Architecture & design documentation
```

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer with roll-forward)
- [Microsoft SQL Server 2019+](https://www.microsoft.com/en-us/sql-server) or Docker

### Starting SQL Server with Docker (Optional)

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_password123" \
   -p 1433:1433 --name sqlserver -d \
   mcr.microsoft.com/mssql/server:2022-latest
```

---

## Configuration & Setup

### 1. Connection String

Update `src/OrdersAndInventory.Api/appsettings.json` with your SQL Server connection string if different:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=OrdersAndInventoryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 2. Database Migrations

The application automatically applies pending migrations and executes the `IDataSeeder` on startup in `Development` mode.

To manually apply the SQL schema script, run `src/OrdersAndInventory.Infrastructure/Scripts/InitialCreate.sql` against your SQL Server instance.

---

## Running the Application & Tests

### Run the API Service

```bash
dotnet run --project src/OrdersAndInventory.Api
```

The service will start on `https://localhost:7001` and `http://localhost:5000`.
Swagger UI is accessible in development at: `http://localhost:5000/swagger`

### Run All Unit & Integration Tests

```bash
dotnet test --logger "console;verbosity=normal"
```

---

## Sample cURL / HTTP Requests

### 1. Health Checks

#### Liveness Probe
```bash
curl -i http://localhost:5000/health
```
*Response: `200 OK` (`Healthy`)*

#### Readiness Probe (Checks SQL Server)
```bash
curl -i http://localhost:5000/health/ready
```
*Response: `200 OK` (`Healthy`)*

---

### 2. Products & Inventory Management

#### Create a New Product
```bash
curl -i -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "sku": "PROD-KEYBOARD-RGB",
    "name": "Mechanical Gaming Keyboard RGB",
    "price": 129.99,
    "stock": 50
  }'
```

#### Get All Products
```bash
curl -i http://localhost:5000/api/products
```

#### Get Product by SKU
```bash
curl -i http://localhost:5000/api/products/PROD-KEYBOARD-RGB
```

#### Adjust Stock (Add or Remove Stock)
```bash
curl -i -X POST http://localhost:5000/api/products/PROD-KEYBOARD-RGB/adjust-stock \
  -H "Content-Type: application/json" \
  -d '{
    "delta": 25
  }'
```

---

### 3. Order Processing & Idempotency

#### Submit an Order
```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "external_order_id": "ORD-20260820-001",
    "placed_at": "2026-08-20T14:30:00Z",
    "items": [
      {
        "sku": "PROD-KEYBOARD-RGB",
        "qty": 2,
        "unit_price": 129.99
      },
      {
        "sku": "MOUSE-002",
        "qty": 1,
        "unit_price": 49.99
      }
    ]
  }'
```
*Response (`201 Created`):*
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "external_order_id": "ORD-20260820-001",
  "placed_at": "2026-08-20T14:30:00Z",
  "total_amount": 309.97,
  "status": "Accepted",
  "items": [
    {
      "id": "e2f0a151-5e92-4f38-bc02-0e9e118eb3d4",
      "sku": "PROD-KEYBOARD-RGB",
      "qty": 2,
      "unit_price": 129.99,
      "total_price": 259.98
    },
    {
      "id": "1c7f99ba-c2b6-4dae-bc60-50d4fbb1c285",
      "sku": "MOUSE-002",
      "qty": 1,
      "unit_price": 49.99,
      "total_price": 49.99
    }
  ],
  "is_duplicate": false
}
```

#### Re-submit the Same Order (Idempotency Test)
```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "external_order_id": "ORD-20260820-001",
    "placed_at": "2026-08-20T14:30:00Z",
    "items": [
      {
        "sku": "PROD-KEYBOARD-RGB",
        "qty": 2,
        "unit_price": 129.99
      }
    ]
  }'
```
*Response (`200 OK`, no stock deducted, `is_duplicate: true`):*
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "external_order_id": "ORD-20260820-001",
  "placed_at": "2026-08-20T14:30:00Z",
  "total_amount": 309.97,
  "status": "Accepted",
  "items": [...],
  "is_duplicate": true
}
```

#### Submit Order with Insufficient Stock (Atomically Rejected)
```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "external_order_id": "ORD-EXCEED-STOCK-001",
    "placed_at": "2026-08-20T15:00:00Z",
    "items": [
      {
        "sku": "PROD-KEYBOARD-RGB",
        "qty": 9999,
        "unit_price": 129.99
      }
    ]
  }'
```
*Response (`422 Unprocessable Entity`):*
```json
{
  "status": 422,
  "title": "Insufficient Stock",
  "detail": "Insufficient stock for product 'PROD-KEYBOARD-RGB'. Requested: 9999, Available: 73.",
  "sku": "PROD-KEYBOARD-RGB",
  "requestedQuantity": 9999,
  "availableStock": 73
}
```

---

### 4. Daily Sales Summary

#### Get Aggregated Sales by Date Range
```bash
curl -i "http://localhost:5000/api/sales/daily-summary?startDate=2026-08-01&endDate=2026-08-31"
```

*Response (`200 OK`):*
```json
{
  "start_date": "2026-08-01",
  "end_date": "2026-08-31",
  "daily_summaries": [
    {
      "date": "2026-08-20",
      "total_qty_sold": 3,
      "total_gross_sales": 309.97,
      "products": [
        {
          "sku": "MOUSE-002",
          "qty_sold": 1,
          "gross_sales": 49.99
        },
        {
          "sku": "PROD-KEYBOARD-RGB",
          "qty_sold": 2,
          "gross_sales": 259.98
        }
      ]
    }
  ]
}
```

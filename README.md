# FlowOps – Event‑Driven Subscription & Billing (MVP)

This is a **learning-first**, event‑driven platform in ASP.NET Core that demonstrates subscriptions, billing, reporting and replay - currently running **in a single process** for simplicity. The architecture and contracts are prepared to split into microservices later.

---

## ✅ What’s implemented now

- **Create & cancel subscriptions** (domain aggregate with invariants)
- **Suspend / Resume** subscriptions
- **Billing** on activation (`InvoiceIssuedEvent`) + **Payments** (`InvoicePaidEvent`)
- **Reporting (read‑model)**: per‑customer `ActiveSubscriptions`, `ActiveSubscriptionIds`, `TotalInvoiced`, `TotalPaid`
- **Event bus** (`IEventBus`) with **in‑memory** implementation
- **Event recorder + replay** to rebuild read‑models
- **ProblemDetails middleware** (400/404/409/500)
- **Postman‑friendly endpoints**
- **Dockerfile / docker compose**

---

## 🧰 Requirements

- .NET SDK **9.0**
- Visual Studio 2022 (Current, 17.8+ recommended)
- Postman (or curl)
- Docker Desktop (optional but supported)

---

## 🗂️ Solution structure

### `FlowOps` (ASP.NET Core Web API)

**Contracts**
- `Contracts/CreateSubscriptionRequest.cs`
- `Contracts/PayInvoiceRequest.cs`
- `Contracts/PlanResponse.cs`
- `Contracts/SubscriptionDetailsResponse.cs`

**Domain / Application**
- `Domain/Subscriptions/Subscription.cs` – aggregate + invariants (`Activate`, `Cancel`, `Suspend`, `Resume`, `Expire`)
- `Domain/Subscriptions/SubscriptionStatus.cs`
- `Domain/Subscriptions/InMemorySubscriptionRepository.cs`
- `Application/Subscriptions/SubscriptionCommandService.cs` – orchestration (publish events)

**Events (integration)**
- `Events/SubscriptionActivatedEvent.cs`
- `Events/SubscriptionCancelledEvent.cs`
- `Events/SubscriptionSuspendedEvent.cs`
- `Events/SubscriptionResumedEvent.cs`
- `Events/InvoicePaidEvent.cs`

**Billing**
- `Pricing/IPlanPricing.cs`
- `Pricing/InMemoryPlanPricing.cs`
- `Services/Billing/IBillingHandler.cs`
- `Services/Billing/BillingHandler.cs` – uses pricing & publishes `InvoiceIssuedEvent` (+ retry)
- `Services/Billing/BillingListener.cs` – subscribes to `SubscriptionActivatedEvent` (logs cancel)

**Reporting (CQRS/read‑model)**
- `Reports/Models/CustomerReport.cs` (includes `ActiveSubscriptionIds`)
- `Reports/Stores/IReportingStore.cs`
- `Reports/Stores/InMemoryReportingStore.cs`
- `Services/Reporting/IReportingHandler.cs`
- `Services/Reporting/ReportingHandler.cs` – updates: active/invoiced/paid + suspended/resumed/cancelled and ID set
- `Services/Reporting/ReportingListener.cs` – subscribes to events and routes to handler

**Replay**
- `Services/Replay/EventRecorder.cs` – in‑memory append‑only buffer
- `Services/Replay/EventRecorderListener.cs` – records key events for replay (incl. suspend/resume)
- `Controllers/ReplayController.cs` – snapshot (sorted) + rebuild reports (clears store and replays)

**API**
- `Controllers/SubscriptionsController.cs` – `POST /api/subscriptions`, `POST /api/subscriptions/{id}/cancel`, `.../suspend`, `.../resume`
- `Controllers/SubscriptionQueriesController.cs` – `GET /api/subscriptions/{id}`
- `Controllers/PaymentsController.cs` – `POST /api/payments`
- `Controllers/ReportsController.cs` – 
  - `GET /api/reports/customers/{customerId}`
  - `GET /api/reports/customers/{customerId}/active-subscriptions`
- `Controllers/PlansController.cs` – `GET /api/plans`

**Middleware**
- `Middleware/ProblemDetailsMiddleware.cs` – consistent 400/404/409/500 responses

**Composition**
- `Program.cs` – DI registrations for EventBus, Repository, Billing, Reporting, Replay, Pricing, Controllers + health checks

### `FlowOps.BuildingBlocks` (shared)

- `Integration/IntegrationEvent.cs` — `Id`, `OccurredOn`, `Version`
- `Integration/InvoiceIssuedEvent.cs`
- `Messaging/IEventBus.cs`, `Messaging/InMemoryEventBus.cs`

---

## 🔄 Event flow (happy path)

1. **Create Subscription** → `SubscriptionCommandService` activates aggregate → publishes **`SubscriptionActivatedEvent`**
2. **BillingListener** receives activation → **BillingHandler** computes amount via pricing → publishes **`InvoiceIssuedEvent`**
3. **ReportingListener / ReportingHandler** updates:
   - `ActiveSubscriptions += 1`
   - `ActiveSubscriptionIds.Add(subscriptionId)`
   - `TotalInvoiced += amount`
4. **PaymentsController** publishes **`InvoicePaidEvent`** → Reporting updates `TotalPaid`

Cancellation / suspend / resume:

- `POST /api/subscriptions/{id}/cancel` → `SubscriptionCancelledEvent` → Reporting: `ActiveSubscriptions--`, `ActiveSubscriptionIds.Remove(id)`
- `POST /api/subscriptions/{id}/suspend` → `SubscriptionSuspendedEvent` → Reporting: `ActiveSubscriptions--`, `ActiveSubscriptionIds.Remove(id)`
- `POST /api/subscriptions/{id}/resume` → `SubscriptionResumedEvent` → Reporting: `ActiveSubscriptions++`, `ActiveSubscriptionIds.Add(id)`

Replay:

- **EventRecorder** records key events
- `POST /api/replay/reports/rebuild` clears in‑memory store and replays events (ordered by `OccurredOn`, then `Version`)

---

## 🚀 Run

### Visual Studio
1. Set **FlowOps** as startup project
2. `F5` (IIS Express or Kestrel)

### Docker compose
`docker-compose.yml` example:
```yaml
services:
  flowops:
    build:
      context: .
      dockerfile: FlowOps/Dockerfile
    image: flowops:dev
    container_name: flowops
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ASPNETCORE_HTTP_PORTS: "8080"
    ports:
      - "5056:8080"
```
Run:
```bash
docker compose up -d --build
```

Healthcheck (if mapped):
```
GET http://localhost:5056/healthz
```

---

## 📡 Endpoints (examples)

### Create subscription
`POST /api/subscriptions`
```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "planCode": "PRO"
}
```
**200 OK**
```json
{ "message": "Subscription created and event published.", "subscriptionId": "..." }
```

### Cancel / Suspend / Resume
`POST /api/subscriptions/{subscriptionId}/cancel`  
`POST /api/subscriptions/{subscriptionId}/suspend`  
`POST /api/subscriptions/{subscriptionId}/resume`

### Pay invoice
`POST /api/payments`
```json
{
  "invoiceId": "00000000-0000-0000-0000-000000000000",
  "customerId": "11111111-1111-1111-1111-111111111111",
  "subscriptionId": "put-created-subscriptionId-here",
  "amount": 99,
  "currency": "PLN",
  "paymentMethod": "CARD",
  "transactionId": "TX-123"
}
```

### Get reports
`GET /api/reports/customers/{customerId}`  
`GET /api/reports/customers/{customerId}/active-subscriptions`

### Plans
`GET /api/plans`

### Replay
`GET /api/replay/events`  
`POST /api/replay/reports/rebuild`

---

## 📝 Notes

- `PlanCode` amounts in `InMemoryPlanPricing`:
  - `PRO` = 99, `BUSINESS` = 199, `ENTERPRISE` = 499
- Read‑model is intentionally **in‑memory** for MVP and replay demo.

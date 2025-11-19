# FlowOps – Event‑Driven Subscription & Billing (MVP)

This is a **learning-first**, event‑driven platform in ASP.NET Core that demonstrates subscriptions, billing, reporting and replay — currently running **in a single process** for simplicity. The architecture and contracts are prepared to split into microservices later.

---

## ✅ What’s implemented now

- **Create & cancel subscriptions** (domain aggregate with invariants)
- **Billing** on activation (`InvoiceIssuedEvent`) + **Payments** (`InvoicePaidEvent`)
- **Reporting (read‑model)**: per‑customer `ActiveSubscriptions`, `TotalInvoiced`, `TotalPaid`
- **Event bus** (`IEventBus`) with **in‑memory** implementation
- **Event recorder + replay** to rebuild read‑models
- **Postman‑friendly endpoints**

---

## 🧰 Requirements

- .NET SDK **9.0**
- Visual Studio 2022 (Current, 17.8+ recommended)
- Postman (or curl)
- (Optional) Docker Desktop, WSL2/Hyper‑V enabled

> You can run everything **without Docker** while learning; Docker can be enabled later.

---

## 🗂️ Solution structure

### `FlowOps` (ASP.NET Core Web API)

**Contracts**
- `Contracts/CreateSubscriptionRequest.cs`
- `Contracts/PayInvoiceRequest.cs`

**Domain / Application**
- `Domain/Subscriptions/Subscription.cs` – aggregate + invariants (`Activate`, `Cancel`)
- `Domain/Subscriptions/InMemorySubscriptionRepository.cs`
- `Application/Subscriptions/SubscriptionCommandService.cs` – orchestration (publish events)

**Events (integration)**
- `Events/SubscriptionActivatedEvent.cs`
- `Events/SubscriptionCancelledEvent.cs`
- `Events/InvoicePaidEvent.cs`

**Billing**
- `Services/Billing/IBillingHandler.cs`
- `Services/Billing/BillingHandler.cs` – generates invoice amount & publishes `InvoiceIssuedEvent`
- `Services/Billing/BillingListener.cs` – subscribes to `SubscriptionActivatedEvent` (and logs cancel)

**Reporting (CQRS/read‑model)**
- `Reports/Models/CustomerReport.cs`
- `Reports/Stores/IReportingStore.cs`
- `Reports/Stores/InMemoryReportingStore.cs`
- `Services/Reporting/IReportingHandler.cs`
- `Services/Reporting/ReportingHandler.cs` – updates: active/invoiced/paid/cancelled
- `Services/Reporting/ReportingListener.cs` – subscribes to events and routes to handler

**Replay**
- `Services/Replay/EventRecorder.cs` – in‑memory append‑only buffer
- `Services/Replay/EventRecorderListener.cs` – records key events for replay
- `Controllers/ReplayController.cs` – snapshot + rebuild reports

**API**
- `Controllers/SubscriptionsController.cs` – `POST /api/subscriptions`, `POST /api/subscriptions/{id}/cancel`
- `Controllers/PaymentsController.cs` – `POST /api/payments`
- `Controllers/ReportsController.cs` – `GET /api/reports/customers/{customerId}`

**Composition**
- `Program.cs` – DI registrations for EventBus, Repository, Billing, Reporting, Replay, Controllers

### `FlowOps.BuildingBlocks` (shared)

- `Integration/IntegrationEvent.cs` — `Id`, `OccurredOn`, `Version`
- `Integration/InvoiceIssuedEvent.cs`
- `Messaging/IEventBus.cs`, `Messaging/InMemoryEventBus.cs`

> Later, `FlowOps.BuildingBlocks` and event contracts can be shared across separate microservices.

---

## 🔄 Event flow (happy path)

1. **Create Subscription** → `SubscriptionCommandService` activates aggregate → publishes **`SubscriptionActivatedEvent`**
2. **BillingListener** receives activation → **BillingHandler** computes amount → publishes **`InvoiceIssuedEvent`**
3. **ReportingListener / ReportingHandler** updates:
   - `ActiveSubscriptions += 1`
   - `TotalInvoiced += amount`
4. **PaymentsController** publishes **`InvoicePaidEvent`** → Reporting updates `TotalPaid`

Cancellation path:

- `POST /api/subscriptions/{id}/cancel` → domain `Cancel()` → publishes **`SubscriptionCancelledEvent`**
- Reporting decrements `ActiveSubscriptions` (not below 0)
- Billing logs cancellation

Replay:

- **EventRecorder** records key events
- `POST /api/replay/reports/rebuild` clears in‑memory store and replays events (in publish order)

---

## 🚀 Run

### Visual Studio
1. Set **FlowOps** as startup project
2. `F5` (IIS Express or Kestrel)

Console shows the listening URLs, e.g. `http://localhost:5056`.

> Docker is optional right now; first learn the flow locally.

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

### Cancel subscription
`POST /api/subscriptions/{subscriptionId}/cancel`  
**200 OK**
```json
{ "message": "Subscription cancelled", "subscriptionId": "..." }
```

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
**200 OK**
```json
{ "message": "Payment received and event published.", "invoiceId": "..." }
```

### Get customer report
`GET /api/reports/customers/{customerId}`  
**200 OK**
```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "activeSubscriptions": 1,
  "totalInvoiced": 99,
  "totalPaid": 99
}
```

### Replay – snapshot
`GET /api/replay/events`  
**200 OK**
```json
[
  { "type": "SubscriptionActivatedEvent", "id": "...", "occurredOn": "...", "version": 1 },
  { "type": "InvoiceIssuedEvent", "id": "...", "occurredOn": "...", "version": 1 },
  { "type": "InvoicePaidEvent", "id": "...", "occurredOn": "...", "version": 1 },
  { "type": "SubscriptionCancelledEvent", "id": "...", "occurredOn": "...", "version": 1 }
]
```

### Replay – rebuild reports
`POST /api/replay/reports/rebuild`  
**200 OK**
```json
{ "message": "Reports rebuilt from recorded events." }
```

---

## 🧪 Plans / next steps

- Add **suspension / resume** state transitions
- Persist subscriptions/invoices with EF Core
- Replace in‑memory bus with RabbitMQ (docker‑compose)
- Extract **event contracts** to a contracts package
- Split solution into microservices (Subscriptions, Billing, Reporting)
- Deterministic replay (sorting by `OccurredOn` if needed)

---

## 📝 Notes

- `PlanCode` amounts in `BillingHandler`:
  - `PRO` = 99, `BUSINESS` = 199, `ENTERPRISE` = 499, default = 49
- Read‑model is intentionally **in‑memory** for MVP and replay demo.

---

## 🐳 Docker (optional now, ready for later)

> You can keep running locally without Docker while learning. Below is a minimal path to containerize **FlowOps** when you’re ready.

### Requirements
- Docker Desktop (Windows): **Use WSL 2 based engine** *or* Hyper‑V with **Containers** feature.
- Make sure the drive/folder with your repo (e.g. `D:\i4b\FlowOps`) is shared in Docker Desktop → **Settings → Resources → File sharing**.

### Minimal Dockerfile (solution root)
If you don’t already use the VS‑generated Dockerfile, a minimal image could look like this:

```dockerfile
# Dockerfile (at the solution root)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# copy csproj first to leverage docker layer caching
COPY ./FlowOps/FlowOps.csproj ./FlowOps/
COPY ./FlowOps.BuildingBlocks/FlowOps.BuildingBlocks.csproj ./FlowOps.BuildingBlocks/
RUN dotnet restore ./FlowOps/FlowOps.csproj

# copy the rest and publish
COPY . .
RUN dotnet publish ./FlowOps/FlowOps.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FlowOps.dll"]
```

### Build & Run
From the solution root:
```bash
docker build -t flowops:dev .
docker run --rm -p 5056:8080 --name flowops flowops:dev
```

Now point Postman to:
```
http://localhost:5056
```

### Notes for VS Docker profile
- If you prefer VS‑driven Docker debugging, choose the **Docker** profile in the run dropdown.
- If you see errors like *“WSL is too old”* or *“Hyper‑V not enabled”*:
  - Update WSL: `wsl --update` and consider `wsl --set-default-version 2`
  - Or enable Hyper‑V & Containers (admin PowerShell):  
    `Enable-WindowsOptionalFeature -Online -FeatureName $("Microsoft-Hyper-V","Containers") -All`
- If you see *“mount denied… too many colons”* or volume issues, ensure the repo drive is shared in Docker Desktop (Settings → Resources → File sharing).

### Compose / RabbitMQ (later)
Once you split services, add `docker-compose.yml` with a broker (e.g. RabbitMQ) and swap the in‑memory bus for a real transport.

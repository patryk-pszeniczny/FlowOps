# FlowOps – Event-Driven Subscription & Billing (MVP)

This project is a simple skeleton of a B2B, event-driven platform. It shows, in a single service:

- accepting an HTTP request (create subscription),
- publishing an integration/domain event,
- listening to that event in a separate component (billing listener),
- logging the handling of that event.

Later this can be split into separate microservices (Identity, Customers, Subscriptions, Billing, Reporting), but right now everything runs in one process to make learning .NET + Docker easier.

---

## 1. Requirements

- .NET SDK **9.0**
- Visual Studio 2022 **17.14.x** (current)
- Docker Desktop in **Linux containers** mode
- WSL2 / Hyper-V enabled on Windows
- Postman (or any HTTP client)

---

## 2. Solution structure

There are currently **two** projects in the solution:

### 2.1 `FlowOps` (ASP.NET Core Web API)
- `Controllers/SubscriptionsController.cs` – exposes `POST /api/Subscriptions`
- `Contracts/CreateSubscriptionRequest.cs` – input DTO
- `Events/SubscriptionActivatedEvent.cs` – integration event published by the controller
- `Services/IBillingHandler.cs` – abstraction for billing logic
- `Services/BillingHandler.cs` – current implementation (just logs the event)
- `Services/BillingListener.cs` – `IHostedService` that subscribes to the event bus and forwards events to the handler
- `Program.cs` – DI setup (event bus, handler, hosted service, controllers)

### 2.2 `FlowOps.BuildingBlocks`
- `Integration/IntegrationEvent.cs` – base class for integration events (`Id`, `OccurredOn`, `Version`)
- `Messaging/IEventBus.cs` – event bus abstraction
- `Messaging/InMemoryEventBus.cs` – simple in-memory bus (publish + subscribe)

This separation allows reusing shared code later across multiple services.

---

## 3. Running in Docker

1. Make sure Docker Desktop is **running** and is set to **Linux containers**.
2. Run the project in Visual Studio using the **Docker** launch profile.
3. In the container logs you should see:
   ```text
   Now listening on: http://[::]:8080
   Now listening on: https://[::]:8081
   ```
4. Check container ports:
   ```powershell
   docker ps
   ```
   Example output:
   ```text
   0.0.0.0:32768->8080/tcp, 0.0.0.0:32769->8081/tcp
   ```
   That means:
   - HTTP is at: `http://localhost:32768`
   - HTTPS is at: `https://localhost:32769` (self-signed cert → disable SSL verification in Postman)

5. Test with Postman:

   **POST** `http://localhost:32768/api/Subscriptions`

   Body:
   ```json
   {
     "customerId": "00000000-0000-0000-0000-000000000001",
     "planCode": "PRO"
   }
   ```

   Response:
   ```json
   {
     "message": "Subscription created and event published",
     "subscriptionId": "..."
   }
   ```

6. In Docker Desktop → Containers → Logs you should see something like:
   ```text
   info: FlowOps.Services.BillingListener[0]
         BillingListener subscribed to SubscriptionActivatedEvent
   info: FlowOps.Services.BillingHandler[0]
         BillingHandler: generating invoice for SubscriptionId=..., CustomerId=..., Plan=PRO
   ```

That confirms the full flow: HTTP → event → listener → handler.

---

## 4. Current endpoints

### `POST /api/Subscriptions`
Creates a (demo) subscription and publishes `SubscriptionActivatedEvent`.

**Request:**
```json
{
  "customerId": "00000000-0000-0000-0000-000000000001",
  "planCode": "PRO"
}
```

**Response:**
```json
{
  "message": "Subscription created and event published",
  "subscriptionId": "56b1e48b-7d42-4983-a5d2-0b6c62735d9c"
}
```

This proves the HTTP → event → listener flow.

### `GET /weatherforecast`
Default ASP.NET Core template endpoint – useful to quickly check if the container responds on the mapped port.

---

## 5. Architecture & patterns used

- **Event-driven:** the API controller does not directly call “billing”. It only publishes an event. A background service (`BillingListener`) subscribes to that event.
- **In-memory event bus:** `IEventBus` and `InMemoryEventBus` live in the shared project (`FlowOps.BuildingBlocks`). This lets all services publish/subscribe without hard-coding transport. Later this can be swapped for RabbitMQ.
- **Separation of concerns:**
  - **Controller** → accepts HTTP, maps DTO, publishes event.
  - **Event** → `SubscriptionActivatedEvent` describes what happened.
  - **Listener** → `BillingListener` subscribes on startup.
  - **Handler** → `IBillingHandler` + `BillingHandler` do the actual “billing” work (for now: logging).
- **SOLID-ish right now:**
  - SRP: listener only wires the subscription, handler only handles the event.
  - DIP: everything depends on abstractions (`IEventBus`, `IBillingHandler`).
  - KISS: DTOs and controller are simple.
- **Ready for microservices:** events have IDs/timestamps/version, there’s a shared building-blocks project, so in the future this can be split into:
  - FlowOps.Subscriptions
  - FlowOps.Billing
  - FlowOps.Reporting
  all listening to the same events.

---

## 6. Reference Program.cs

```csharp
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// shared event bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// billing logic
builder.Services.AddScoped<IBillingHandler, BillingHandler>();

// background listener that subscribes to events
builder.Services.AddHostedService<BillingListener>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 7. Next steps / TODO

- Extract event contracts to a separate project (e.g. `FlowOps.Contracts`) so multiple services can use the same event types.
- Add a second consumer (e.g. `ReportingListener`) to show that multiple services can react to the same event.
- Add persistence (EF Core) for subscriptions/invoices.
- Replace `InMemoryEventBus` with a real broker (RabbitMQ) and run it via docker-compose.
- Split the API into multiple services once the flow is stable.

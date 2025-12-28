# FlowOps - Subscriptions, Billing & Reporting (mikroserwisy .NET, EF Core)


| Serwis | Rola | Domyślny port | Bazowy URL (lokalnie) |
|---|---|---:|---|
| `flowops` | Subscriptions, Customers, Pricing, System, Diagnostics, Event Store, User State | **5056** | `http://localhost:5056` |
| `billing` | Faktury i płatności (Invoices/Payments) | **5057** | `http://localhost:5057` |
| `reporting` | Raporty / analityka / (opcjonalnie) replay read-modelu | **5058** | `http://localhost:5058` |

---

## API & endpointy


### FlowOps (`http://localhost:5056`)

**System**
- `GET /whoami`
- `GET /api/system/ping`
- `GET /api/system/uptime`
- `GET /api/system/info`
- `GET /healthz`
- `GET /healthz/details`

**Pricing**
- `GET /api/plans`
- `GET /api/plans/{planCode}`
- `GET /api/plans/recommendation?budget=...`

**Customers**
- `POST /api/customer`
- `GET /api/customer/{customerId}`
- `GET /api/customer?take=...`
- `GET /api/reporting/customers/{customerId}`
- `GET /api/reporting/customers?q=...&take=...`

**Subscriptions**
- `POST /api/subscriptions` (Idempotency-Key)
- `POST /api/subscriptions/{subscriptionId}/cancel`
- `POST /api/subscriptions/{subscriptionId}/suspend`
- `POST /api/subscriptions/{subscriptionId}/resume`
- `GET  /api/subscriptions/idempotency/{key}`
- `GET  /api/subscriptions/{subscriptionId}`
- `GET  /api/subscriptions/by-customer/{customerId}`
- `GET  /api/subscriptions/by-customer/{customerId}/plans/{planCode}`
- `GET  /api/subscriptions/by-customer/{customerId}/status-breakdown`
- SQL/read endpoints:
  - `GET /api/subscriptions/sql/{subscriptionId}`
  - `GET /api/subscriptions/sql/by-customer/{customerId}?status=Active`
  - `GET /api/subscriptions/sql/by-customer/{customerId}/paged?page=...&pageSize=...&orderBy=...&orderDirection=...`
  - `GET /api/subscriptions/sql/by-customer/{customerId}/status-summary`

**User State**
- `GET  /api/user-state/{userId}`
- `POST /api/user-state/{userId}/preferences`
- `POST /api/user-state/{userId}/drafts`
- `POST /api/user-state/{userId}/cached-lists`

**Diagnostics**
- `GET    /api/diagnostics/integration-events?take=...`
- `GET    /api/diagnostics/integration-events/summary`
- `DELETE /api/diagnostics/integration-events`
- `GET    /api/diagnostics/storage/overview`
- `GET    /api/diagnostics/storage/user-state`
- Idempotency keys:
  - `GET    /api/diagnostics/idempotency`
  - `GET    /api/diagnostics/idempotency/{key}`
  - `POST   /api/diagnostics/idempotency`
  - `DELETE /api/diagnostics/idempotency/{key}`
  - `DELETE /api/diagnostics/idempotency`
- Event store:
  - `GET /api/event-store/events`
  - `GET /api/event-store/events/{eventId}`
  - `GET /api/event-store/events/search?type=&since=&until=&take=...`
  - `GET /api/event-store/types`
  - `GET /api/event-store/stats`

---

### Billing (`http://localhost:5057`)

**Invoices**
- `POST /api/invoices/issue`
- `POST /api/invoices/{invoiceId}/pay`
- `GET  /api/invoices?status=issued`
- `GET  /api/invoices/{invoiceId}`
- `GET  /api/invoices/summary`

**Payments**
- `POST /api/payments`
- `GET  /api/payments/history?customerId=...`
- `GET  /api/payments/stats`

---

### Reporting (`http://localhost:5058`)

**Analytics**
- `GET /api/analytics/activity?take=...`
- `GET /api/analytics/billing`
- `GET /api/analytics/subscriptions/status`
- `GET /api/analytics/subscriptions/velocity?days=...`
- `GET /api/analytics/billing/trends?months=...`

**Reports**
- `GET /api/reports/customers/{customerId}`
- `GET /api/reports/customers/{customerId}/active-subscriptions`
- `GET /api/reports/customers/{customerId}/timeline`

**Replay** (jeśli wystawione w reporting)
- `GET    /api/replay/events`
- `POST   /api/replay/reports/rebuild`
- `DELETE /api/replay/events`

Projekt ma charakter edukacyjny i służy do nauki architektury event-driven, DDD i CQRS w praktyce.

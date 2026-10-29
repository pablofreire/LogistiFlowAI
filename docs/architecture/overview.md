# Architecture overview

```mermaid
flowchart LR
    user([Operator browser]) -->|HTTPS| web[web<br/>nginx + React SPA]
    client([API client / k6]) --> gw
    web -->|/api| gw[Gateway<br/>JWT issuance · YARP · dashboard]
    gw -->|REST + bearer| orders[Orders API]
    gw -->|REST + bearer| inventory[Inventory API]
    gw -->|REST + bearer| fulfillment[Fulfillment API]
    gw --- idb[(identity db)]
    orders --- odb[(orders db)]
    inventory --- ivdb[(inventory db)]
    fulfillment --- fdb[(fulfillment db)]
    orders <-->|outbox / inbox| mq{{RabbitMQ<br/>logistiflow.events}}
    inventory <-->|outbox / inbox| mq
    fulfillment <-->|outbox / inbox| mq
```

## First business flow

```mermaid
sequenceDiagram
    autonumber
    participant UI as SPA
    participant O as Orders
    participant MQ as RabbitMQ
    participant I as Inventory
    participant F as Fulfillment
    UI->>O: POST /api/orders (Idempotency-Key)
    O->>O: tx: order + history + outbox(OrderCreated)
    O-->>UI: 201 PendingInventory
    O->>MQ: relay publishes OrderCreated
    MQ->>I: OrderCreated
    I->>I: tx: inbox + conditional UPDATE per line + outbox(InventoryReserved | Failed)
    I->>MQ: InventoryReserved
    MQ->>O: InventoryReserved → Reserved
    MQ->>F: InventoryReserved
    F->>F: tx: inbox + job(Picking) + outbox(FulfillmentStarted)
    F->>MQ: FulfillmentStarted
    MQ->>O: FulfillmentStarted → Picking
    UI->>O: GET /api/orders/{id} (polls while in flight)
```

## Integration events (v1)

| Event | Producer | Consumers | Effect |
|---|---|---|---|
| `orders.order-created` | Orders | Inventory | Reserve stock |
| `orders.order-cancelled` | Orders | Inventory, Fulfillment | Release stock (or tombstone), cancel job |
| `inventory.inventory-reserved` | Inventory | Orders, Fulfillment | Order → Reserved; start job |
| `inventory.inventory-reservation-failed` | Inventory | Orders | Order → Failed with shortages |
| `inventory.inventory-released` | Inventory | — | Audit (no consumer yet) |
| `fulfillment.fulfillment-started` | Fulfillment | Orders | Order → Picking |

Every message carries `eventId`, `eventType`, `version`, `occurredAt`, `correlationId` and `causationId`, plus a
W3C `traceparent` header. Contracts: `src/Contracts/LogistiFlow.Contracts.IntegrationEvents`.

## Code layout (per service)

```
LogistiFlow.{Service}.Domain          Entities, Enums, Errors, Services (state machines), ReadModels, Interfaces/Repositories
LogistiFlow.{Service}.Application     UseCases/{Aggregate}/{Action}/{Contracts,Interfaces,Implementations,Validators}, IntegrationEventHandlers
LogistiFlow.{Service}.Infrastructure  Persistence/{Migrations,Queries,Rows,Repositories,Readers,Seed}
LogistiFlow.{Service}.Api             Controllers (+ Contracts), Program (composition root)
```

Rules are enforced by `tests/architecture` (29 rules: service isolation, layer direction, folder conventions).

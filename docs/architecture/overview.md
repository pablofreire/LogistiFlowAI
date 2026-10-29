# Architecture overview

```mermaid
flowchart LR
    user([Operator browser]) -->|HTTPS| web[web<br/>nginx + React SPA]
    client([API client / k6]) --> gw
    web -->|/api| gw[Gateway<br/>JWT issuance · YARP · dashboard]
    gw -->|REST + bearer| orders[Orders API]
    gw -->|REST + bearer| inventory[Inventory API]
    gw -->|REST + bearer| fulfillment[Fulfillment API]
    gw -->|REST + bearer| shipping[Shipping API]
    gw --- idb[(identity db)]
    orders --- odb[(orders db)]
    inventory --- ivdb[(inventory db)]
    fulfillment --- fdb[(fulfillment db)]
    shipping --- sdb[(shipping db)]
    shipping -->|book · track| carrier[Carrier simulator]
    orders <-->|outbox / inbox| mq{{RabbitMQ<br/>logistiflow.events}}
    inventory <-->|outbox / inbox| mq
    fulfillment <-->|outbox / inbox| mq
    shipping <-->|outbox / inbox| mq
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

## Second flow: pack, ship, deliver

```mermaid
sequenceDiagram
    autonumber
    participant Op as Operator
    participant F as Fulfillment
    participant MQ as RabbitMQ
    participant O as Orders
    participant I as Inventory
    participant S as Shipping
    participant C as Carrier
    Op->>F: POST /jobs/{id}/pack → Packed + outbox(FulfillmentPacked)
    MQ->>O: FulfillmentPacked → Packed
    Op->>F: POST /jobs/{id}/ship → Shipped + outbox(FulfillmentShipped)
    MQ->>O: FulfillmentShipped → Shipped
    MQ->>I: FulfillmentShipped → consume reservation (on_hand and reserved drop)
    MQ->>S: FulfillmentShipped → shipment PendingBooking (no HTTP in this transaction)
    S->>C: booking worker: POST /v1/shipments (Idempotency-Key = shipment id)
    C-->>S: tracking number + estimate → LabelCreated
    loop tracking worker
        S->>C: GET /v1/shipments/{tracking}/events
        S->>S: store new events once; delays and failed attempts open exceptions
    end
    S->>MQ: ShipmentDelivered (or ShipmentReturned)
    MQ->>O: Delivered (or Failed: returned to sender)
```

## Integration events (v1)

| Event | Producer | Consumers | Effect |
|---|---|---|---|
| `orders.order-created` | Orders | Inventory, Shipping | Reserve stock; Shipping keeps the destination |
| `orders.order-cancelled` | Orders | Inventory, Fulfillment | Release stock (or tombstone), cancel job |
| `inventory.inventory-reserved` | Inventory | Orders, Fulfillment | Order → Reserved; start job |
| `inventory.inventory-reservation-failed` | Inventory | Orders | Order → Failed with shortages |
| `inventory.inventory-released` | Inventory | — | Audit (no consumer yet) |
| `fulfillment.fulfillment-started` | Fulfillment | Orders | Order → Picking |
| `fulfillment.fulfillment-packed` | Fulfillment | Orders | Order → Packed |
| `fulfillment.fulfillment-shipped` | Fulfillment | Orders, Inventory, Shipping | Order → Shipped; consume stock; open shipment |
| `shipping.shipment-delivered` | Shipping | Orders | Order → Delivered |
| `shipping.shipment-returned` | Shipping | Orders | Order → Failed (returned to sender) |

Every message carries `eventId`, `eventType`, `version`, `occurredAt`, `correlationId` and `causationId`, plus a
W3C `traceparent` header. Contracts: `src/Contracts/LogistiFlow.Contracts.IntegrationEvents`.

## Code layout (per service)

```
LogistiFlow.{Service}.Domain          Entities, Enums, Errors, Services (state machines), ReadModels, Interfaces/Repositories
LogistiFlow.{Service}.Application     UseCases/{Aggregate}/{Action}/{Contracts,Interfaces,Implementations,Validators}, IntegrationEventHandlers
LogistiFlow.{Service}.Infrastructure  Persistence/{Migrations,Queries,Rows,Repositories,Readers,Seed}
LogistiFlow.{Service}.Api             Controllers (+ Contracts), Program (composition root)
```

Rules are enforced by `tests/architecture` (39 rules: service isolation, layer direction, folder conventions; the
carrier simulator may not reference any LogistiFlow assembly).

# Entity-Relationship Diagram

Schema produced by `InitialCreate`. Enums are stored as `varchar(20)` strings.
Money values are inlined as two columns (`_amount numeric(18,2)` + `_currency varchar(3)`).

```mermaid
erDiagram
    orders {
        uuid        Id              PK
        varchar(20) PriorityLevel
        varchar(20) Status
    }

    order_items {
        uuid            Id                  PK
        uuid            OrderId             FK
        uuid            MenuItemId
        varchar(20)     SnackType
        varchar(20)     PriorityLevel
        varchar(20)     Status
        timestamptz     EnqueuedAt
        timestamptz     StartedBakingAt     "nullable"
        timestamptz     ReadyAt             "nullable"
        numeric_18_2    unit_price_amount
        varchar(3)      unit_price_currency
    }

    menu_items {
        uuid            Id              PK
        varchar(200)    Name
        varchar(20)     SnackType
        boolean         IsRemoved
        numeric_18_2    price_amount
        varchar(3)      price_currency
    }

    payments {
        uuid            Id                  PK
        uuid            OrderId
        varchar(20)     Method
        varchar(20)     Status
        numeric_18_2    amount_due_amount
        varchar(3)      amount_due_currency
    }

    idempotency_records {
        uuid    Key             PK
        text    ResultJson
        int     HttpStatusCode
        timestamptz CreatedAt
    }

    orders ||--o{ order_items : "contains"
```

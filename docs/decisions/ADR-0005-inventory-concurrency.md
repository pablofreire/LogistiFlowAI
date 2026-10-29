# ADR-0005: Inventory concurrency through one conditional UPDATE per line

- Status: Accepted
- Date: 2026-10-29

## Context
Concurrent orders compete for the same units. Read-then-write (check availability, then reserve) oversells under
concurrency unless the check and the write are atomic.

## Decision
- Reserve each line with `UPDATE stock_positions SET reserved = reserved + @qty WHERE id = @id AND on_hand -
  reserved >= @qty`. Under READ COMMITTED a concurrent writer makes the statement wait for the row lock and
  re-evaluate the condition against committed values; 0 rows affected means the stock was taken.
- An order is reserved in **one warehouse, all lines or none**. Candidates are read first (warehouses that cover
  every line, ordered by code); lines are reserved in stock-position order to avoid deadlocks; if a line loses the
  race, the lines already reserved in this transaction are given back and the next warehouse is tried.
- `CHECK (reserved <= on_hand AND reserved >= 0)` is defence in depth. Every change is recorded in `stock_movements`.

## Evidence
`InventoryReservationTests.ConcurrentReservations_ForTheLastUnits_NeverOversell`: 50 parallel reservations for an
item with 10 units → exactly 10 reserved, 40 failed, `reserved = 10`, and the ledger holds exactly 10 units.

## Consequences
- No distributed locks, no optimistic retry loops in application code.
- Warehouse choice is deterministic (first by code), not optimal; distance- or cost-based allocation is future work.

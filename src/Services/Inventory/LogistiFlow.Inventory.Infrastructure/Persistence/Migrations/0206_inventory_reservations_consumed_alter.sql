-- Phase 2: shipping consumes a reservation. The units leave on_hand and reserved together, the reservation becomes
-- Consumed and the ledger records a Shipment movement. Constraints are widened in a new script; 0204/0205 stay as run.
ALTER TABLE inventory.reservations DROP CONSTRAINT ck_reservations_status;
ALTER TABLE inventory.reservations ADD CONSTRAINT ck_reservations_status
    CHECK (status IN ('Reserved', 'Released', 'Failed', 'Cancelled', 'Consumed'));

ALTER TABLE inventory.stock_movements DROP CONSTRAINT ck_stock_movements_type;
ALTER TABLE inventory.stock_movements ADD CONSTRAINT ck_stock_movements_type
    CHECK (movement_type IN ('Receipt', 'Reservation', 'Release', 'Shipment'));

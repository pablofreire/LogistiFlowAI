// Contract types generated from the services' OpenAPI documents (npm run generate:api). Never edit generated files.
import type { components as FulfillmentContract } from './generated/fulfillment';
import type { components as GatewayContract } from './generated/gateway';
import type { components as InventoryContract } from './generated/inventory';
import type { components as OrdersContract } from './generated/orders';
import type { components as ShippingContract } from './generated/shipping';

type Orders = OrdersContract['schemas'];
type Inventory = InventoryContract['schemas'];
type Fulfillment = FulfillmentContract['schemas'];
type Gateway = GatewayContract['schemas'];
type Shipping = ShippingContract['schemas'];

export type LoginResponse = Gateway['LoginResponse'];
export type AuthenticatedUser = Gateway['AuthenticatedUser'];

export type OrderStatus = Orders['OrderStatus'];
export type OrderDetails = Orders['OrderDetails'];
export type OrderSummary = Orders['OrderSummary'];
export type OrderStatusChangeItem = Orders['OrderStatusChangeItem'];
export type OrdersPage = Orders['PagedResultOfOrderSummary'];
export type CreateOrderBody = Orders['CreateOrderBody'];
export type OrdersMetrics = Orders['GetOrdersMetricsResponse'];

export type StockPositionItem = Inventory['StockPositionItem'];
export type StockPage = Inventory['PagedResultOfStockPositionItem'];
export type WarehouseItem = Inventory['WarehouseItem'];
export type ProductsPage = Inventory['PagedResultOfProductItem'];
export type ReservationDetails = Inventory['ReservationDetails'];
export type InventoryMetrics = Inventory['GetInventoryMetricsResponse'];

export type FulfillmentJobStatus = Fulfillment['FulfillmentJobStatus'];
export type FulfillmentJobDetails = Fulfillment['FulfillmentJobDetails'];
export type FulfillmentJobSummary = Fulfillment['FulfillmentJobSummary'];
export type FulfillmentJobsPage = Fulfillment['PagedResultOfFulfillmentJobSummary'];
export type FulfillmentActionResult = Fulfillment['PerformFulfillmentJobActionResponse'];
export type FulfillmentMetrics = Fulfillment['GetFulfillmentMetricsResponse'];

export type ShipmentStatus = Shipping['ShipmentStatus'];
export type ShipmentSummary = Shipping['ShipmentSummary'];
export type ShipmentsPage = Shipping['PagedResultOfShipmentSummary'];
export type ShipmentDetails = Shipping['ShipmentDetails'];
export type ShipmentExceptionItem = Shipping['ShipmentExceptionItem'];
export type ShipmentExceptionsPage = Shipping['PagedResultOfShipmentExceptionItem'];
export type ShipmentExceptionStatus = Shipping['ShipmentExceptionStatus'];
export type ShipmentExceptionType = Shipping['ShipmentExceptionType'];
export type ShipmentActionOutcome = Shipping['ShipmentActionOutcome'];
export type ShippingMetrics = Shipping['GetShippingMetricsResponse'];

/** The gateway forwards each service's metrics untouched; the sections keep their service contracts. */
export interface DashboardSummary {
  orders: OrdersMetrics | null;
  inventory: InventoryMetrics | null;
  fulfillment: FulfillmentMetrics | null;
  shipping: ShippingMetrics | null;
  unavailableSources: string[];
  generatedAt: string;
}

export type Role = 'Admin' | 'OpsManager' | 'WarehouseOperator' | 'Viewer';

export const fulfillmentJobStatuses: FulfillmentJobStatus[] = ['Created', 'Picking', 'Packed', 'Shipped', 'Cancelled', 'Failed'];

export const shipmentStatuses: ShipmentStatus[] = [
  'PendingBooking',
  'BookingFailed',
  'LabelCreated',
  'InTransit',
  'OutForDelivery',
  'DeliveryAttemptFailed',
  'Delivered',
  'ReturnedToSender',
];

export const shipmentExceptionTypes: ShipmentExceptionType[] = ['CarrierUnavailable', 'Delay', 'DeliveryAttemptFailed', 'ReturnedToSender'];

export const orderStatuses: OrderStatus[] = [
  'Created',
  'PendingInventory',
  'Reserved',
  'Picking',
  'Packed',
  'Shipped',
  'Delivered',
  'Cancelled',
  'Failed',
];

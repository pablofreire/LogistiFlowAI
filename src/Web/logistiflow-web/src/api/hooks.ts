import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { request, toQueryString } from './client';
import type {
  CreateOrderBody,
  DashboardSummary,
  FulfillmentActionResult,
  FulfillmentJobDetails,
  FulfillmentJobsPage,
  FulfillmentJobStatus,
  LoginResponse,
  OrderDetails,
  OrderStatus,
  OrderStatusChangeItem,
  OrdersPage,
  ReservationDetails,
  ShipmentActionOutcome,
  ShipmentDetails,
  ShipmentExceptionsPage,
  ShipmentExceptionStatus,
  ShipmentExceptionType,
  ShipmentsPage,
  ShipmentStatus,
  StockPage,
  WarehouseItem,
} from './types';

const keys = {
  dashboard: ['dashboard'] as const,
  orders: (filters: OrderFilters) => ['orders', filters] as const,
  order: (id: string) => ['order', id] as const,
  history: (id: string) => ['order', id, 'history'] as const,
  reservation: (id: string) => ['order', id, 'reservation'] as const,
  job: (id: string) => ['order', id, 'fulfillment'] as const,
  stock: (filters: StockFilters) => ['stock', filters] as const,
  warehouses: ['warehouses'] as const,
  jobs: (filters: JobFilters) => ['fulfillment-jobs', filters] as const,
  shipments: (filters: ShipmentFilters) => ['shipments', filters] as const,
  shipment: (id: string) => ['shipment', id] as const,
  shipmentByOrder: (orderId: string) => ['order', orderId, 'shipment'] as const,
  exceptions: (filters: ExceptionFilters) => ['shipment-exceptions', filters] as const,
};

export interface JobFilters {
  page: number;
  pageSize: number;
  status: FulfillmentJobStatus | null;
}

export interface ShipmentFilters {
  page: number;
  pageSize: number;
  status: ShipmentStatus | null;
  delayedOnly: boolean;
}

export interface ExceptionFilters {
  page: number;
  pageSize: number;
  status: ShipmentExceptionStatus | null;
  type: ShipmentExceptionType | null;
}

export type FloorAction = 'pack' | 'ship' | 'report-issue' | 'reprocess';

/** Shipments move on their own as the carrier reports; screens that show them refresh on this cadence. */
const trackingRefreshMs = 2_000;

export interface OrderFilters {
  page: number;
  pageSize: number;
  status: OrderStatus | null;
  search: string;
}

export interface StockFilters {
  page: number;
  pageSize: number;
  warehouseCode: string | null;
  sku: string;
  lowStockOnly: boolean;
}

// Statuses that change on their own as events arrive from other services.
const inFlightStatuses: OrderStatus[] = ['Created', 'PendingInventory', 'Reserved', 'Picking', 'Packed', 'Shipped'];

export function login(email: string, password: string) {
  return request<LoginResponse>('/api/auth/login', { method: 'POST', body: { email, password } });
}

export function useDashboard() {
  return useQuery({
    queryKey: keys.dashboard,
    queryFn: async ({ signal }) => (await request<DashboardSummary>('/api/dashboard/summary', { signal })).data,
    refetchInterval: 5_000,
  });
}

export function useOrders(filters: OrderFilters) {
  return useQuery({
    queryKey: keys.orders(filters),
    queryFn: async ({ signal }) =>
      (await request<OrdersPage>(`/api/orders${toQueryString({ ...filters, search: filters.search.trim() })}`, { signal })).data,
    placeholderData: keepPreviousData,
  });
}

export function useOrder(orderId: string) {
  return useQuery({
    queryKey: keys.order(orderId),
    queryFn: async ({ signal }) => (await request<OrderDetails>(`/api/orders/${orderId}`, { signal })).data,
    // The order moves on its own as events arrive; poll while it is still in flight.
    refetchInterval: (query) => (query.state.data && inFlightStatuses.includes(query.state.data.status) ? 1_000 : false),
  });
}

export function useOrderHistory(orderId: string, status: OrderStatus | undefined) {
  return useQuery({
    queryKey: [...keys.history(orderId), status],
    queryFn: async ({ signal }) => (await request<OrderStatusChangeItem[]>(`/api/orders/${orderId}/history`, { signal })).data,
    enabled: status !== undefined,
  });
}

export function useReservation(orderId: string, status: OrderStatus | undefined) {
  return useQuery({
    queryKey: [...keys.reservation(orderId), status],
    queryFn: async ({ signal }) => (await request<ReservationDetails>(`/api/inventory/reservations/by-order/${orderId}`, { signal })).data,
    enabled: status !== undefined && status !== 'Created' && status !== 'PendingInventory',
    retry: false,
  });
}

export function useFulfillmentJob(orderId: string, status: OrderStatus | undefined) {
  return useQuery({
    queryKey: [...keys.job(orderId), status],
    queryFn: async ({ signal }) => (await request<FulfillmentJobDetails>(`/api/fulfillment/jobs/by-order/${orderId}`, { signal })).data,
    enabled: status !== undefined && !['Created', 'PendingInventory', 'Reserved', 'Failed'].includes(status),
    retry: false,
  });
}

export function useCreateOrder() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async ({ body, idempotencyKey }: { body: CreateOrderBody; idempotencyKey: string }) =>
      (await request<OrderDetails>('/api/orders', { method: 'POST', body, headers: { 'Idempotency-Key': idempotencyKey } })).data,
    onSuccess: () => client.invalidateQueries({ queryKey: ['orders'] }),
  });
}

export function useCancelOrder(orderId: string) {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async (reason: string) => (await request<OrderDetails>(`/api/orders/${orderId}/cancel`, { method: 'POST', body: { reason } })).data,
    onSuccess: (order) => {
      client.setQueryData(keys.order(orderId), order);
      return client.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}

export function useStock(filters: StockFilters) {
  return useQuery({
    queryKey: keys.stock(filters),
    queryFn: async ({ signal }) =>
      (await request<StockPage>(`/api/inventory/stock${toQueryString({ ...filters, sku: filters.sku.trim().toUpperCase() })}`, { signal })).data,
    placeholderData: keepPreviousData,
  });
}

export function useWarehouses() {
  return useQuery({
    queryKey: keys.warehouses,
    queryFn: async ({ signal }) => (await request<WarehouseItem[]>('/api/inventory/warehouses', { signal })).data,
    staleTime: 60_000,
  });
}

export function useFulfillmentJobs(filters: JobFilters) {
  return useQuery({
    queryKey: keys.jobs(filters),
    queryFn: async ({ signal }) => (await request<FulfillmentJobsPage>(`/api/fulfillment/jobs${toQueryString({ ...filters })}`, { signal })).data,
    placeholderData: keepPreviousData,
    refetchInterval: 5_000,
  });
}

export function useFloorAction() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async ({ jobId, action, reason }: { jobId: string; action: FloorAction; reason?: string }) =>
      (
        await request<FulfillmentActionResult>(`/api/fulfillment/jobs/${jobId}/${action}`, {
          method: 'POST',
          body: action === 'report-issue' ? { reason } : undefined,
        })
      ).data,
    onSuccess: () => Promise.all([client.invalidateQueries({ queryKey: ['fulfillment-jobs'] }), client.invalidateQueries({ queryKey: ['order'] })]),
  });
}

export function useShipments(filters: ShipmentFilters) {
  return useQuery({
    queryKey: keys.shipments(filters),
    queryFn: async ({ signal }) =>
      (await request<ShipmentsPage>(`/api/shipping/shipments${toQueryString({ ...filters, delayedOnly: filters.delayedOnly || null })}`, { signal })).data,
    placeholderData: keepPreviousData,
    refetchInterval: trackingRefreshMs,
  });
}

export function useShipment(shipmentId: string) {
  return useQuery({
    queryKey: keys.shipment(shipmentId),
    queryFn: async ({ signal }) => (await request<ShipmentDetails>(`/api/shipping/shipments/${shipmentId}`, { signal })).data,
    refetchInterval: trackingRefreshMs,
  });
}

/** The order's shipment once it has shipped; the carrier keeps updating it, so it refreshes while undelivered. */
export function useShipmentByOrder(orderId: string, status: OrderStatus | undefined) {
  return useQuery({
    queryKey: keys.shipmentByOrder(orderId),
    queryFn: async ({ signal }) => (await request<ShipmentDetails>(`/api/shipping/shipments/by-order/${orderId}`, { signal })).data,
    enabled: status === 'Shipped' || status === 'Delivered' || status === 'Failed',
    retry: false,
    refetchInterval: status === 'Shipped' ? trackingRefreshMs : false,
  });
}

export function useShipmentExceptions(filters: ExceptionFilters) {
  return useQuery({
    queryKey: keys.exceptions(filters),
    queryFn: async ({ signal }) => (await request<ShipmentExceptionsPage>(`/api/shipping/exceptions${toQueryString({ ...filters })}`, { signal })).data,
    placeholderData: keepPreviousData,
    refetchInterval: 5_000,
  });
}

export function useResolveException() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async ({ exceptionId, resolution }: { exceptionId: string; resolution: string }) =>
      (await request<ShipmentActionOutcome>(`/api/shipping/exceptions/${exceptionId}/resolve`, { method: 'POST', body: { resolution } })).data,
    onSuccess: () =>
      Promise.all([client.invalidateQueries({ queryKey: ['shipment-exceptions'] }), client.invalidateQueries({ queryKey: ['shipment'] })]),
  });
}

export function useRetryBooking(shipmentId: string) {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async () => (await request<ShipmentActionOutcome>(`/api/shipping/shipments/${shipmentId}/retry-booking`, { method: 'POST' })).data,
    onSuccess: () => client.invalidateQueries({ queryKey: keys.shipment(shipmentId) }),
  });
}

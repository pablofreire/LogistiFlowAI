import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { request, toQueryString } from './client';
import type {
  CreateOrderBody,
  DashboardSummary,
  FulfillmentJobDetails,
  LoginResponse,
  OrderDetails,
  OrderStatus,
  OrderStatusChangeItem,
  OrdersPage,
  ReservationDetails,
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
};

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

const inFlightStatuses: OrderStatus[] = ['Created', 'PendingInventory', 'Reserved'];

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

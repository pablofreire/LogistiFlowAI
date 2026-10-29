import { Navigate, Route, Routes } from 'react-router-dom';
import { RequireAuth } from './auth/RequireAuth';
import { AppLayout } from './components/layout/AppLayout';
import { CreateOrderPage } from './pages/CreateOrderPage';
import { DashboardPage } from './pages/DashboardPage';
import { InventoryPage } from './pages/InventoryPage';
import { LoginPage } from './pages/LoginPage';
import { OrderDetailsPage } from './pages/OrderDetailsPage';
import { OrdersPage } from './pages/OrdersPage';

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth>
            <AppLayout />
          </RequireAuth>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="orders" element={<OrdersPage />} />
        <Route
          path="orders/new"
          element={
            <RequireAuth roles={['Admin', 'OpsManager']}>
              <CreateOrderPage />
            </RequireAuth>
          }
        />
        <Route path="orders/:orderId" element={<OrderDetailsPage />} />
        <Route path="inventory" element={<InventoryPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

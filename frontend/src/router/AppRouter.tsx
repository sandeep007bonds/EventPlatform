import { Route, Routes } from 'react-router-dom';
import { BuyerLayout } from '../layouts/BuyerLayout';
import { AdminLayout } from '../layouts/AdminLayout';
import { LoginPage } from '../pages/auth/LoginPage';
import { LogoutPage } from '../pages/auth/LogoutPage';
import { PlaceholderPage } from '../components/common/PlaceholderPage';
import { NotFoundPage } from '../components/common/errors/NotFoundPage';
import { ProtectedRoute } from './ProtectedRoute';

/**
 * The full route tree. `BuyerLayout` and `AdminLayout` each own their own themed
 * `<ConfigProvider>` — this is "one app, two themed sections," not micro-frontends.
 */
export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/logout" element={<LogoutPage />} />
      <Route path="/admin/login" element={<LoginPage />} />

      <Route element={<BuyerLayout />}>
        {/* Public: no ProtectedRoute — anonymous browsing of published events (ADR-0015). */}
        <Route index element={<PlaceholderPage title="Events" />} />
        <Route
          path="/orders"
          element={
            <ProtectedRoute>
              <PlaceholderPage title="My orders" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tickets"
          element={
            <ProtectedRoute>
              <PlaceholderPage title="My tickets" />
            </ProtectedRoute>
          }
        />
      </Route>

      <Route
        path="/admin"
        element={
          <ProtectedRoute>
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<PlaceholderPage title="Events" />} />
        <Route path="orders" element={<PlaceholderPage title="Orders" />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

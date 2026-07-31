import { Route, Routes } from 'react-router-dom';
import { BuyerLayout } from '../layouts/BuyerLayout';
import { AdminLayout } from '../layouts/AdminLayout';
import { LoginPage } from '../pages/auth/LoginPage';
import { LogoutPage } from '../pages/auth/LogoutPage';
import { NotFoundPage } from '../components/common/errors/NotFoundPage';
import { EventsListPage } from '../features/buyer/events/EventsListPage';
import { EventDetailPage } from '../features/buyer/events/EventDetailPage';
import { SeatSelectionPage } from '../features/buyer/seatmap/SeatSelectionPage';
import { CheckoutPage } from '../features/buyer/checkout/CheckoutPage';
import { OrderPage } from '../features/buyer/orders/OrderPage';
import { MyOrdersPage } from '../features/buyer/orders/MyOrdersPage';
import { AdminEventsPage } from '../features/admin/events/AdminEventsPage';
import { CreateEventPage } from '../features/admin/events/CreateEventPage';
import { AdminEventDetailPage } from '../features/admin/events/AdminEventDetailPage';
import { AdminOrdersPage } from '../features/admin/orders/AdminOrdersPage';
import { VenuesPage } from '../features/admin/venues/VenuesPage';
import { CreateVenuePage } from '../features/admin/venues/CreateVenuePage';
import { VenueDetailPage } from '../features/admin/venues/VenueDetailPage';
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
        <Route index element={<EventsListPage />} />
        <Route path="/events/:id" element={<EventDetailPage />} />
        <Route
          path="/events/:id/seats"
          element={
            <ProtectedRoute>
              <SeatSelectionPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/checkout/:holdId"
          element={
            <ProtectedRoute>
              <CheckoutPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/orders"
          element={
            <ProtectedRoute>
              <MyOrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/orders/:orderId"
          element={
            <ProtectedRoute>
              <OrderPage />
            </ProtectedRoute>
          }
        />
        {/* No standalone ticket index exists — tickets are only ever reached via their order. */}
        <Route
          path="/tickets"
          element={
            <ProtectedRoute>
              <MyOrdersPage />
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
        <Route index element={<AdminEventsPage />} />
        <Route path="events/new" element={<CreateEventPage />} />
        <Route path="events/:id" element={<AdminEventDetailPage />} />
        <Route path="venues" element={<VenuesPage />} />
        <Route path="venues/new" element={<CreateVenuePage />} />
        <Route path="venues/:id" element={<VenueDetailPage />} />
        <Route path="orders" element={<AdminOrdersPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

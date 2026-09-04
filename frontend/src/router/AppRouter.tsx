import { Route, Routes } from 'react-router-dom';
import { BuyerLayout } from '../layouts/BuyerLayout';
import { AdminLayout } from '../layouts/AdminLayout';
import { BuyerLoginPage } from '../pages/auth/BuyerLoginPage';
import { OrganizerLoginPage } from '../pages/auth/OrganizerLoginPage';
import { LogoutPage } from '../pages/auth/LogoutPage';
import { NotFoundPage } from '../components/common/errors/NotFoundPage';
import { EventsListPage } from '../features/buyer/events/EventsListPage';
import { EventDetailPage } from '../features/buyer/events/EventDetailPage';
import { SeatSelectionPage } from '../features/buyer/seatmap/SeatSelectionPage';
import { QueueWaitingRoomPage } from '../features/buyer/queue/QueueWaitingRoomPage';
import { CheckoutPage } from '../features/buyer/checkout/CheckoutPage';
import { CheckoutReturnPage } from '../features/buyer/checkout/CheckoutReturnPage';
import { OrderPage } from '../features/buyer/orders/OrderPage';
import { MyOrdersPage } from '../features/buyer/orders/MyOrdersPage';
import { AdminEventsPage } from '../features/admin/events/AdminEventsPage';
import { CreateEventPage } from '../features/admin/events/CreateEventPage';
import { AdminEventDetailPage } from '../features/admin/events/AdminEventDetailPage';
import { PageShell } from '../components/common/layout/PageShell';
import { OrganizerPoliciesPage } from '../features/admin/policies/OrganizerPoliciesPage';
import { AdminOrdersPage } from '../features/admin/orders/AdminOrdersPage';
import { EventGroupsPage } from '../features/admin/eventGroups/EventGroupsPage';
import { CreateEventGroupPage } from '../features/admin/eventGroups/CreateEventGroupPage';
import { TourDetailPage } from '../features/admin/eventGroups/TourDetailPage';
import { ScanTicketPage } from '../features/admin/tickets/ScanTicketPage';
import { ProtectedRoute } from './ProtectedRoute';

/**
 * The full route tree. `BuyerLayout` and `AdminLayout` each own their own themed
 * `<ConfigProvider>` — this is "one app, two themed sections," not micro-frontends.
 */
export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<BuyerLoginPage />} />
      <Route path="/logout" element={<LogoutPage />} />
      <Route path="/admin/login" element={<OrganizerLoginPage />} />

      <Route element={<BuyerLayout />}>
        {/* Public: no ProtectedRoute — anonymous browsing of published events (ADR-0015). */}
        <Route index element={<EventsListPage />} />
        {/* `:eventSlug` accepts a slug or a GUID — links issued before slugs existed still work. */}
        <Route path="/events/:eventSlug" element={<EventDetailPage />} />
        {/* No ProtectedRoute — a buyer picks seats freely; the identity gate is the "Hold
            selection" action itself (see SeatSelectionPage.tsx's handleHold, ADR-0016).
            The performance is in the path, not a query param: seats are inventory for one night
            (ADR-0039), so a link to "these seats" is meaningless without saying which. */}
        <Route path="/events/:eventSlug/s/:eventSessionId/seats" element={<SeatSelectionPage />} />
        {/* Public, anonymous — the virtual waiting room for events with RequiresQueue set
            (ADR-0026). Keyed on the event, not a performance: one waiting room gates the on-sale,
            and an on-sale covers the whole run. */}
        <Route path="/events/:eventSlug/queue" element={<QueueWaitingRoomPage />} />
        <Route
          path="/checkout/:holdId"
          element={
            <ProtectedRoute>
              <CheckoutPage />
            </ProtectedRoute>
          }
        />
        {/* Landing page for payment methods that redirect out to authenticate and back (a 3-D
            Secure challenge frame, a UPI app-switch) — see CheckoutPaymentForm's return_url. */}
        <Route
          path="/checkout/:holdId/return"
          element={
            <ProtectedRoute>
              <CheckoutReturnPage />
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
        <Route
          index
          element={
            <PageShell>
              <AdminEventsPage />
            </PageShell>
          }
        />
        <Route
          path="events/new"
          element={
            <PageShell>
              <CreateEventPage />
            </PageShell>
          }
        />
        <Route path="events/:eventId" element={<AdminEventDetailPage />} />
        <Route
          path="tours"
          element={
            <PageShell>
              <EventGroupsPage />
            </PageShell>
          }
        />
        <Route
          path="tours/new"
          element={
            <PageShell>
              <CreateEventGroupPage />
            </PageShell>
          }
        />
        <Route
          path="tours/:tourId"
          element={
            <PageShell>
              <TourDetailPage />
            </PageShell>
          }
        />
        <Route
          path="orders"
          element={
            <PageShell>
              <AdminOrdersPage />
            </PageShell>
          }
        />
        <Route
          path="scan"
          element={
            <PageShell>
              <ScanTicketPage />
            </PageShell>
          }
        />
        <Route
          path="policies"
          element={
            <PageShell>
              <OrganizerPoliciesPage />
            </PageShell>
          }
        />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

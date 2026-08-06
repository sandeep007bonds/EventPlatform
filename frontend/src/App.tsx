import { App as AntApp, ConfigProvider } from 'antd';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ToastHolder } from './components/common/feedback/ToastHolder';
import { TopProgressBar } from './components/common/feedback/TopProgressBar';
import { RouteErrorBoundary } from './components/common/errors/RouteErrorBoundary';
import { AppRouter } from './router/AppRouter';

// The outer `ConfigProvider` here carries no theme token — `BuyerLayout` and
// `AdminLayout` each apply their own nested `ConfigProvider` with a themed token
// set. `AntApp` is Ant's message/notification/modal context-provider component,
// required for the theme-aware toast helpers in `components/common/feedback`.
export function App() {
  return (
    <ConfigProvider>
      <AntApp>
        <ToastHolder />
        <TopProgressBar />
        <BrowserRouter>
          <AuthProvider>
            <RouteErrorBoundary>
              <AppRouter />
            </RouteErrorBoundary>
          </AuthProvider>
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  );
}

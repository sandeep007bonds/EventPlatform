import { ConfigProvider, Layout, Menu, Button } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { adminTheme } from '../theme/adminTheme';
import { useAuth } from '../contexts/useAuth';

const { Header, Sider, Content } = Layout;

/** Organizer-facing shell: denser, Ant-default theme; every route here requires login. */
export function AdminLayout() {
  const { t } = useTranslation('admin');
  const { logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    void navigate('/admin/login');
  };

  const selectedKey = location.pathname.startsWith('/admin/orders')
    ? 'orders'
    : location.pathname.startsWith('/admin/venues')
      ? 'venues'
      : 'events';

  return (
    <ConfigProvider theme={adminTheme}>
      <Layout style={{ minHeight: '100vh' }}>
        <Sider breakpoint="lg" collapsedWidth="0">
          <div
            style={{
              color: '#fff',
              fontWeight: 600,
              padding: 16,
              fontSize: 16,
            }}
          >
            {t('common:appName')} · Admin
          </div>
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[selectedKey]}
            items={[
              { key: 'events', label: <Link to="/admin">{t('nav.events')}</Link> },
              { key: 'venues', label: <Link to="/admin/venues">{t('nav.venues')}</Link> },
              { key: 'orders', label: <Link to="/admin/orders">{t('nav.orders')}</Link> },
            ]}
          />
        </Sider>
        <Layout>
          <Header style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center' }}>
            <Button onClick={handleLogout}>{t('common:actions.logOut')}</Button>
          </Header>
          <Content style={{ padding: 24 }}>
            <Outlet />
          </Content>
        </Layout>
      </Layout>
    </ConfigProvider>
  );
}

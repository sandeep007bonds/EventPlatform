import { ConfigProvider, Layout, Menu, Button, Typography } from 'antd';
import {
  CalendarOutlined,
  CompassOutlined,
  LogoutOutlined,
  QrcodeOutlined,
  ShoppingOutlined,
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { adminTheme } from '../theme/adminTheme';
import { useAuth } from '../contexts/useAuth';
import { PageContainer } from '../components/common/layout/PageContainer';

const { Header, Sider, Content } = Layout;

/** Organizer-facing shell: denser console theme; every route here requires login. */
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
    : location.pathname.startsWith('/admin/tours')
      ? 'tours'
      : location.pathname.startsWith('/admin/scan')
        ? 'scan'
        : 'events';

  return (
    <ConfigProvider theme={adminTheme}>
      <Layout style={{ minHeight: '100vh' }}>
        <Sider breakpoint="lg" collapsedWidth="0" width={220}>
          <div
            style={{
              color: '#fff',
              fontWeight: 700,
              padding: '20px 20px 16px',
              fontSize: 16,
              display: 'flex',
              alignItems: 'center',
              gap: 8,
              letterSpacing: 0.2,
            }}
          >
            <span
              aria-hidden
              style={{
                width: 10,
                height: 10,
                borderRadius: 3,
                background: '#3ea8c4',
                display: 'inline-block',
                flexShrink: 0,
              }}
            />
            <span style={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {t('common:appName')}
            </span>
          </div>
          <Typography.Text
            style={{
              display: 'block',
              padding: '0 20px 16px',
              color: 'rgba(255,255,255,0.45)',
              fontSize: 12,
              textTransform: 'uppercase',
              letterSpacing: 0.6,
            }}
          >
            Organizer console
          </Typography.Text>
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[selectedKey]}
            style={{ borderInlineEnd: 'none' }}
            items={[
              {
                key: 'events',
                icon: <CalendarOutlined />,
                label: <Link to="/admin">{t('nav.events')}</Link>,
              },
              {
                key: 'tours',
                icon: <CompassOutlined />,
                label: <Link to="/admin/tours">{t('nav.tours')}</Link>,
              },
              {
                key: 'orders',
                icon: <ShoppingOutlined />,
                label: <Link to="/admin/orders">{t('nav.orders')}</Link>,
              },
              {
                key: 'scan',
                icon: <QrcodeOutlined />,
                label: <Link to="/admin/scan">{t('nav.scan')}</Link>,
              },
            ]}
          />
        </Sider>
        <Layout>
          <Header
            style={{
              display: 'flex',
              justifyContent: 'flex-end',
              alignItems: 'center',
              borderBottom: '1px solid rgba(0,0,0,0.06)',
            }}
          >
            <Button icon={<LogoutOutlined />} onClick={handleLogout}>
              {t('common:actions.logOut')}
            </Button>
          </Header>
          <Content style={{ padding: '28px 32px 56px' }}>
            <PageContainer maxWidth={1360}>
              <Outlet />
            </PageContainer>
          </Content>
        </Layout>
      </Layout>
    </ConfigProvider>
  );
}

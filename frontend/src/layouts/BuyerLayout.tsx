import { ConfigProvider, Layout, Menu, Button, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { buyerTheme } from '../theme/buyerTheme';
import { useAuth } from '../contexts/useAuth';

const { Header, Content, Footer } = Layout;

/** Buyer-facing shell: PouchNation-referenced storefront theme, public + authenticated routes. */
export function BuyerLayout() {
  const { t } = useTranslation('buyer');
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    void navigate('/login');
  };

  return (
    <ConfigProvider theme={buyerTheme}>
      <Layout style={{ minHeight: '100vh' }}>
        <Header style={{ display: 'flex', alignItems: 'center' }}>
          <Link to="/" style={{ color: '#fff', fontWeight: 600, marginRight: 32 }}>
            {t('common:appName')}
          </Link>
          <Menu
            theme="dark"
            mode="horizontal"
            style={{ flex: 1, minWidth: 0 }}
            selectable={false}
            items={[
              { key: 'events', label: <Link to="/">{t('nav.events')}</Link> },
              ...(user
                ? [
                    { key: 'orders', label: <Link to="/orders">{t('nav.myOrders')}</Link> },
                    { key: 'tickets', label: <Link to="/tickets">{t('nav.myTickets')}</Link> },
                  ]
                : []),
            ]}
          />
          <Space>
            {user ? (
              <Button onClick={handleLogout}>{t('common:actions.logOut')}</Button>
            ) : (
              <Link to="/login">
                <Button type="primary">{t('common:actions.logIn')}</Button>
              </Link>
            )}
          </Space>
        </Header>
        <Content style={{ padding: '24px 48px' }}>
          <Outlet />
        </Content>
        <Footer style={{ textAlign: 'center' }}>
          {t('common:appName')} ©{new Date().getFullYear()}
        </Footer>
      </Layout>
    </ConfigProvider>
  );
}

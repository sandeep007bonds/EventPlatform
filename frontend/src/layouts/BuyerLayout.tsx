import { ConfigProvider, Layout, Menu, Button, Space, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { buyerTheme } from '../theme/buyerTheme';
import { useAuth } from '../contexts/useAuth';
import { PageContainer } from '../components/common/layout/PageContainer';

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
        <Header
          style={{
            display: 'flex',
            alignItems: 'center',
            position: 'sticky',
            top: 0,
            zIndex: 10,
            boxShadow: '0 1px 0 rgba(255,255,255,0.06)',
          }}
        >
          <Link
            to="/"
            style={{
              color: '#fff',
              fontWeight: 700,
              fontSize: 18,
              marginRight: 40,
              letterSpacing: 0.2,
              display: 'flex',
              alignItems: 'center',
              gap: 8,
              whiteSpace: 'nowrap',
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
              }}
            />
            {t('common:appName')}
          </Link>
          <Menu
            theme="dark"
            mode="horizontal"
            style={{ flex: 1, minWidth: 0, background: 'transparent', borderBottom: 'none' }}
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
        <Content style={{ padding: '32px 24px 56px' }}>
          <PageContainer>
            <Outlet />
          </PageContainer>
        </Content>
        <Footer
          style={{
            textAlign: 'center',
            borderTop: '1px solid rgba(0,0,0,0.06)',
            paddingTop: 20,
          }}
        >
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            {t('common:appName')} © {new Date().getFullYear()}
          </Typography.Text>
        </Footer>
      </Layout>
    </ConfigProvider>
  );
}

import { ConfigProvider, Card, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { OrganizerAuthFlow } from '../../features/admin/auth/OrganizerAuthFlow';
import { adminTheme } from '../../theme/adminTheme';
import { PRIMARY_COLOR } from '../../theme/colors';

// Same centered-card shell as BuyerLoginPage.tsx, themed for the admin console instead of the
// storefront — organizers register/log in here with real email+password (ADR-0023); dev-login has
// no UI presence anywhere in the app any more.
export function OrganizerLoginPage() {
  const { t } = useTranslation('auth');
  const navigate = useNavigate();

  return (
    <ConfigProvider theme={adminTheme}>
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 24,
          background: `radial-gradient(circle at 20% 20%, rgba(62,168,196,0.12), transparent 45%), radial-gradient(circle at 80% 80%, rgba(62,168,196,0.08), transparent 45%), #f3f5f7`,
        }}
      >
        <div style={{ width: '100%', maxWidth: 440 }}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 8,
              marginBottom: 20,
            }}
          >
            <span
              aria-hidden
              style={{
                width: 12,
                height: 12,
                borderRadius: 4,
                background: PRIMARY_COLOR,
                display: 'inline-block',
              }}
            />
            <Typography.Text strong style={{ fontSize: 16, letterSpacing: 0.2 }}>
              {t('common:appName')}
            </Typography.Text>
          </div>

          <Card
            style={{ boxShadow: '0 12px 32px rgba(15,23,32,0.1)' }}
            styles={{ body: { padding: 32 } }}
          >
            <Typography.Title level={3} style={{ marginTop: 0, marginBottom: 4 }}>
              {t('login.title')}
            </Typography.Title>
            <Typography.Text type="secondary">{t('login.subtitle')}</Typography.Text>

            <div style={{ marginTop: 24 }}>
              <OrganizerAuthFlow onAuthenticated={() => void navigate('/admin')} />
            </div>
          </Card>
        </div>
      </div>
    </ConfigProvider>
  );
}

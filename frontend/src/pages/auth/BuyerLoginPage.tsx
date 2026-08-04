import { ConfigProvider, Card, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { OtpLoginFlow } from '../../features/buyer/auth/OtpLoginFlow';
import { buyerTheme } from '../../theme/buyerTheme';

// Same PouchNation-referenced centered-card shell as LoginPage.tsx, but for buyers this now
// drives real Identity OTP login instead of the dev-login form (ADR-0016) — organizers still use
// LoginPage.tsx/dev-login via /admin/login until Entra External ID exists.
export function BuyerLoginPage() {
  const { t } = useTranslation('auth');
  const navigate = useNavigate();

  return (
    <ConfigProvider theme={buyerTheme}>
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 24,
          background:
            'radial-gradient(circle at 20% 20%, rgba(62,168,196,0.16), transparent 45%), radial-gradient(circle at 80% 80%, rgba(62,168,196,0.12), transparent 45%), #f7f5f0',
        }}
      >
        <div style={{ width: '100%', maxWidth: 400 }}>
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
                background: '#3ea8c4',
                display: 'inline-block',
              }}
            />
            <Typography.Text strong style={{ fontSize: 16, letterSpacing: 0.2 }}>
              {t('common:appName')}
            </Typography.Text>
          </div>

          <Card
            style={{ boxShadow: '0 12px 32px rgba(31,28,20,0.1)' }}
            styles={{ body: { padding: 32 } }}
          >
            <Typography.Title level={3} style={{ marginTop: 0, marginBottom: 4 }}>
              {t('login.title')}
            </Typography.Title>
            <Typography.Text type="secondary">{t('login.subtitle')}</Typography.Text>

            <div style={{ marginTop: 24 }}>
              <OtpLoginFlow onVerified={() => void navigate('/')} />
            </div>
          </Card>
        </div>
      </div>
    </ConfigProvider>
  );
}

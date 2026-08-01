import { useState } from 'react';
import { Button, Card, ConfigProvider, Form, Input, Radio, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/useAuth';
import { toast } from '../../components/common/feedback/toast';
import { buyerTheme } from '../../theme/buyerTheme';

interface LoginFormValues {
  email: string;
  role: 'buyer' | 'organizer';
}

// PouchNation-referenced: centered card on a soft ground. Real buyer OTP login and
// organizer Entra login replace this dev-login form in later phases (ADR-0015) —
// today it stands in as a safe, server-side token mint for both roles.
export function LoginPage() {
  const { t } = useTranslation('auth');
  const { loginWithDevCredentials } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [submitting, setSubmitting] = useState(false);

  const defaultRole: 'buyer' | 'organizer' = location.pathname.startsWith('/admin')
    ? 'organizer'
    : 'buyer';

  const handleSubmit = async (values: LoginFormValues) => {
    setSubmitting(true);
    try {
      await loginWithDevCredentials(values.email, values.role);
      void navigate(values.role === 'organizer' ? '/admin' : '/');
    } catch {
      toast.error('Login failed. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

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

            <Form<LoginFormValues>
              layout="vertical"
              style={{ marginTop: 24 }}
              initialValues={{ role: defaultRole }}
              onFinish={(values) => {
                void handleSubmit(values);
              }}
            >
              <Form.Item
                name="email"
                label={t('login.emailLabel')}
                rules={[{ required: true, message: t('login.emailRequired') }]}
              >
                <Input type="email" autoComplete="email" autoFocus />
              </Form.Item>

              <Form.Item name="role" label={t('login.roleLabel')}>
                <Radio.Group
                  options={[
                    { label: t('login.roleBuyer'), value: 'buyer' },
                    { label: t('login.roleOrganizer'), value: 'organizer' },
                  ]}
                  optionType="button"
                  block
                />
              </Form.Item>

              <Form.Item style={{ marginBottom: 16 }}>
                <Button type="primary" htmlType="submit" block size="large" loading={submitting}>
                  {t('login.submit')}
                </Button>
              </Form.Item>
            </Form>

            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {t('login.devNotice')}
            </Typography.Text>
          </Card>
        </div>
      </div>
    </ConfigProvider>
  );
}

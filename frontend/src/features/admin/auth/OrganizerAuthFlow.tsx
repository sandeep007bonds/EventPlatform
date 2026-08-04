import { useState } from 'react';
import { Button, Form, Input, Tabs } from 'antd';
import type { AxiosError } from 'axios';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../../contexts/useAuth';
import type { OrganizerAuthErrorBody } from '../../../services/auth/organizerApi';
import { toast } from '../../../components/common/feedback/toast';

interface LoginFormValues {
  email: string;
  password: string;
}

interface RegisterFormValues {
  organizationName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface OrganizerAuthFlowProps {
  /** Called once the organizer has successfully registered or logged in and their session is live. */
  onAuthenticated: () => void;
}

/**
 * Organizer email+password login/register, in one component switched by an internal tab — not a
 * step sequence like the buyer `OtpLoginFlow` (ADR-0023). Registering creates a brand-new
 * organization (tenant) and its first organizer account together; there is no separate
 * invite-a-teammate flow yet.
 */
export function OrganizerAuthFlow({ onAuthenticated }: OrganizerAuthFlowProps) {
  const { t } = useTranslation('auth');
  const { loginWithOrganizerCredentials, registerOrganizer } = useAuth();

  const [loggingIn, setLoggingIn] = useState(false);
  const [registering, setRegistering] = useState(false);

  const errorMessage = (error: unknown): string => {
    const body = (error as AxiosError<OrganizerAuthErrorBody>).response?.data;
    switch (body?.error) {
      case 'invalid_credentials':
        return t('organizerAuth.invalidCredentials');
      case 'locked_out':
        return t('organizerAuth.lockedOut');
      case 'email_already_registered':
        return t('organizerAuth.emailAlreadyRegistered');
      default:
        return t('organizerAuth.unexpectedError');
    }
  };

  const handleLogin = async (values: LoginFormValues) => {
    setLoggingIn(true);
    try {
      await loginWithOrganizerCredentials(values.email, values.password);
      onAuthenticated();
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setLoggingIn(false);
    }
  };

  const handleRegister = async (values: RegisterFormValues) => {
    setRegistering(true);
    try {
      await registerOrganizer(values.organizationName, values.email, values.password);
      onAuthenticated();
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setRegistering(false);
    }
  };

  return (
    <Tabs
      defaultActiveKey="login"
      items={[
        {
          key: 'login',
          label: t('organizerAuth.loginTab'),
          children: (
            <Form<LoginFormValues>
              layout="vertical"
              onFinish={(values) => void handleLogin(values)}
            >
              <Form.Item
                name="email"
                label={t('organizerAuth.emailLabel')}
                rules={[
                  { required: true, message: t('organizerAuth.emailRequired') },
                  { type: 'email', message: t('organizerAuth.emailInvalid') },
                ]}
              >
                <Input autoComplete="email" autoFocus />
              </Form.Item>
              <Form.Item
                name="password"
                label={t('organizerAuth.passwordLabel')}
                rules={[{ required: true, message: t('organizerAuth.passwordRequired') }]}
              >
                <Input.Password autoComplete="current-password" />
              </Form.Item>
              <Form.Item style={{ marginBottom: 0 }}>
                <Button type="primary" htmlType="submit" block size="large" loading={loggingIn}>
                  {t('organizerAuth.login')}
                </Button>
              </Form.Item>
            </Form>
          ),
        },
        {
          key: 'register',
          label: t('organizerAuth.registerTab'),
          children: (
            <Form<RegisterFormValues>
              layout="vertical"
              onFinish={(values) => void handleRegister(values)}
            >
              <Form.Item
                name="organizationName"
                label={t('organizerAuth.organizationNameLabel')}
                rules={[{ required: true, message: t('organizerAuth.organizationNameRequired') }]}
              >
                <Input autoFocus />
              </Form.Item>
              <Form.Item
                name="email"
                label={t('organizerAuth.emailLabel')}
                rules={[
                  { required: true, message: t('organizerAuth.emailRequired') },
                  { type: 'email', message: t('organizerAuth.emailInvalid') },
                ]}
              >
                <Input autoComplete="email" />
              </Form.Item>
              <Form.Item
                name="password"
                label={t('organizerAuth.passwordLabel')}
                rules={[
                  { required: true, message: t('organizerAuth.passwordRequired') },
                  { min: 8, message: t('organizerAuth.passwordTooShort') },
                ]}
                hasFeedback
              >
                <Input.Password autoComplete="new-password" />
              </Form.Item>
              <Form.Item
                name="confirmPassword"
                label={t('organizerAuth.confirmPasswordLabel')}
                dependencies={['password']}
                hasFeedback
                rules={[
                  { required: true, message: t('organizerAuth.passwordRequired') },
                  ({ getFieldValue }) => ({
                    validator(_, value: string) {
                      return !value || value === getFieldValue('password')
                        ? Promise.resolve()
                        : Promise.reject(new Error(t('organizerAuth.passwordsDoNotMatch')));
                    },
                  }),
                ]}
              >
                <Input.Password autoComplete="new-password" />
              </Form.Item>
              <Form.Item style={{ marginBottom: 0 }}>
                <Button type="primary" htmlType="submit" block size="large" loading={registering}>
                  {t('organizerAuth.register')}
                </Button>
              </Form.Item>
            </Form>
          ),
        },
      ]}
    />
  );
}

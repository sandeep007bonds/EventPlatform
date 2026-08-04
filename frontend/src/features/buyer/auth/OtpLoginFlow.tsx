import { useState } from 'react';
import { Button, Form, Input, Typography } from 'antd';
import type { AxiosError } from 'axios';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../../contexts/useAuth';
import { requestOtp, type OtpErrorBody } from '../../../services/auth/identityApi';
import { toast } from '../../../components/common/feedback/toast';

// E.164: leading +, first digit 1-9, 8-15 digits total — matches Identity's server-side check.
const E164_PATTERN = /^\+[1-9]\d{7,14}$/;

function maskPhoneNumber(phoneNumber: string): string {
  return phoneNumber.length > 4
    ? `${'•'.repeat(phoneNumber.length - 4)}${phoneNumber.slice(-4)}`
    : phoneNumber;
}

export interface OtpLoginFlowProps {
  /** Called once the buyer has successfully verified a code and their session is live. */
  onVerified: () => void;
}

/**
 * Two-step buyer login: request a code, then verify it. Renders bare (usable both inside a
 * `Modal` — triggered from the "Hold selection" action — and directly on `/login`, per ADR-0016's
 * "identity gate moves to the hold action, not an upfront wall" decision).
 */
export function OtpLoginFlow({ onVerified }: OtpLoginFlowProps) {
  const { t } = useTranslation('auth');
  const { loginWithOtp } = useAuth();

  const [step, setStep] = useState<'phone' | 'code'>('phone');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [code, setCode] = useState('');
  const [sendingCode, setSendingCode] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [resendDisabled, setResendDisabled] = useState(false);

  const sendCode = async (targetPhoneNumber: string) => {
    setSendingCode(true);
    try {
      const response = await requestOtp({ phoneNumber: targetPhoneNumber });
      setPhoneNumber(targetPhoneNumber);
      setCode('');
      setStep('code');
      setResendDisabled(true);
      setTimeout(() => setResendDisabled(false), response.expiresInSeconds * 1000);
    } catch (error) {
      const body = (error as AxiosError<OtpErrorBody>).response?.data;
      if (body?.error === 'rate_limited') {
        toast.error(t('otpLogin.rateLimited', { seconds: body.retryAfterSeconds ?? 60 }));
      } else if (body?.error === 'invalid_phone_number') {
        toast.error(t('otpLogin.phoneInvalid'));
      } else {
        toast.error(t('otpLogin.sendFailed'));
      }
    } finally {
      setSendingCode(false);
    }
  };

  const handleVerify = async () => {
    setVerifying(true);
    try {
      await loginWithOtp(phoneNumber, code);
      onVerified();
    } catch (error) {
      const body = (error as AxiosError<OtpErrorBody>).response?.data;
      if (body?.error === 'locked_out') {
        toast.error(t('otpLogin.lockedOut'));
      } else if (body?.error === 'expired') {
        toast.error(t('otpLogin.codeExpired'));
      } else if (body?.error === 'no_active_challenge') {
        toast.error(t('otpLogin.noActiveChallenge'));
        setStep('phone');
      } else {
        toast.error(t('otpLogin.wrongCode'));
      }
    } finally {
      setVerifying(false);
    }
  };

  if (step === 'phone') {
    return (
      <Form<{ phoneNumber: string }>
        layout="vertical"
        onFinish={(values) => void sendCode(values.phoneNumber)}
      >
        <Form.Item
          name="phoneNumber"
          label={t('otpLogin.phoneLabel')}
          rules={[
            { required: true, message: t('otpLogin.phoneRequired') },
            { pattern: E164_PATTERN, message: t('otpLogin.phoneInvalid') },
          ]}
        >
          <Input type="tel" autoComplete="tel" placeholder="+14155552671" autoFocus />
        </Form.Item>
        <Form.Item style={{ marginBottom: 0 }}>
          <Button type="primary" htmlType="submit" block size="large" loading={sendingCode}>
            {t('otpLogin.sendCode')}
          </Button>
        </Form.Item>
      </Form>
    );
  }

  return (
    <div>
      <Typography.Text type="secondary">
        {t('otpLogin.codeSentTo', { phoneNumber: maskPhoneNumber(phoneNumber) })}
      </Typography.Text>
      <Typography.Paragraph style={{ marginTop: 16, marginBottom: 8 }}>
        {t('otpLogin.codeLabel')}
      </Typography.Paragraph>
      <Input.OTP length={6} value={code} onChange={setCode} style={{ width: '100%' }} />
      <Button
        type="primary"
        block
        size="large"
        style={{ marginTop: 20 }}
        loading={verifying}
        disabled={code.length < 6}
        onClick={() => void handleVerify()}
      >
        {t('otpLogin.verify')}
      </Button>
      <div style={{ marginTop: 12, display: 'flex', justifyContent: 'space-between' }}>
        <Button type="link" style={{ padding: 0 }} onClick={() => setStep('phone')}>
          {t('otpLogin.changeNumber')}
        </Button>
        <Button
          type="link"
          style={{ padding: 0 }}
          disabled={resendDisabled || sendingCode}
          onClick={() => void sendCode(phoneNumber)}
        >
          {t('otpLogin.resend')}
        </Button>
      </div>
    </div>
  );
}

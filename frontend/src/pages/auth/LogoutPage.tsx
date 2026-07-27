import { useEffect } from 'react';
import { Button, Result } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/useAuth';

/** Logs the user out on mount, then shows a confirmation with a way back in. */
export function LogoutPage() {
  const { t } = useTranslation('auth');
  const { logout } = useAuth();

  useEffect(() => {
    logout();
    // Runs once on mount only — logging out again on every re-render would be wrong.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <Result
      status="success"
      title={t('logout.title')}
      subTitle={t('logout.subtitle')}
      extra={
        <Link to="/login">
          <Button type="primary">{t('common:actions.logIn')}</Button>
        </Link>
      }
    />
  );
}

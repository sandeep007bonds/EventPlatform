import { Button, Result } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, useLocation } from 'react-router-dom';

// Ant's `Result` ships 403/404/500 illustrations natively but has no literal 401
// variant — a 401 (not logged in) is mapped onto the 403 visual, since both mean
// "you can't see this," just for a different reason.
export function UnauthorizedPage() {
  const { t } = useTranslation('errors');
  const location = useLocation();

  // /admin/* and everything else are two genuinely different login flows now (organizer
  // email+password vs buyer OTP, ADR-0023) — route the "Log in" link to whichever one applies.
  const loginPath = location.pathname.startsWith('/admin') ? '/admin/login' : '/login';

  return (
    <Result
      status="403"
      title={t('unauthorized.title')}
      subTitle={t('unauthorized.subtitle')}
      extra={
        <Link to={loginPath}>
          <Button type="primary">{t('common:actions.logIn')}</Button>
        </Link>
      }
    />
  );
}

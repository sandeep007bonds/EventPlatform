import { Button, Result } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

// Ant's `Result` ships 403/404/500 illustrations natively but has no literal 401
// variant — a 401 (not logged in) is mapped onto the 403 visual, since both mean
// "you can't see this," just for a different reason.
export function UnauthorizedPage() {
  const { t } = useTranslation('errors');

  return (
    <Result
      status="403"
      title={t('unauthorized.title')}
      subTitle={t('unauthorized.subtitle')}
      extra={
        <Link to="/login">
          <Button type="primary">{t('common:actions.logIn')}</Button>
        </Link>
      }
    />
  );
}

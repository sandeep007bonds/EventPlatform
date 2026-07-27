import { Button, Result } from 'antd';
import { useTranslation } from 'react-i18next';

/** Full-page 500 — reserved for route-level failures, not every failed API call (those toast). */
export function ServerErrorPage() {
  const { t } = useTranslation('errors');

  return (
    <Result
      status="500"
      title={t('serverError.title')}
      subTitle={t('serverError.subtitle')}
      extra={
        <Button type="primary" onClick={() => window.location.reload()}>
          {t('common:actions.retry')}
        </Button>
      }
    />
  );
}

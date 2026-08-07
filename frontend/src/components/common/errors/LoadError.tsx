import { Button, Result } from 'antd';
import { useTranslation } from 'react-i18next';

interface LoadErrorProps {
  /** Overrides the default subtitle — e.g. "Could not load your orders." */
  description?: string;
  /** Re-runs the failed fetch. Omit if the caller has no cheap way to retry in place. */
  onRetry?: () => void;
}

/**
 * Inline, non-navigating "couldn't load this" state for a list/panel's own data fetch — the
 * graceful alternative to a toast for a failed GET, and to silently rendering an empty state
 * that's indistinguishable from a genuine "there's nothing here." Unlike `ServerErrorPage`, the
 * rest of the page (header, toolbar, sibling panels) stays mounted and usable.
 */
export function LoadError({ description, onRetry }: LoadErrorProps) {
  const { t } = useTranslation('errors');

  return (
    <Result
      status="warning"
      title={t('loadError.title')}
      subTitle={description ?? t('loadError.subtitle')}
      extra={
        onRetry && (
          <Button type="primary" onClick={onRetry}>
            {t('common:actions.retry')}
          </Button>
        )
      }
    />
  );
}

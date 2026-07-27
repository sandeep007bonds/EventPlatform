import { Result } from 'antd';

/**
 * Stands in for a route until its real feature lands (buyer/admin features are Phase 4
 * of the frontend build — see the approved plan). Lets routing, layouts and auth be
 * reviewed end-to-end before any feature is built.
 */
export function PlaceholderPage({ title }: { title: string }) {
  return <Result icon={<span />} title={title} subTitle="Coming soon." />;
}
